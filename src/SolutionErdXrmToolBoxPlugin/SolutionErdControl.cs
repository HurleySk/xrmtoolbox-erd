using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using SolutionErd;
using SolutionErd.Layout;
using SolutionErd.Model;
using SolutionErd.Writers;
using XrmToolBox.Extensibility;

namespace SolutionErdXrmToolBoxPlugin
{
    public partial class SolutionErdControl : PluginControlBase
    {
        static readonly (ColumnMode Value, string Text)[] ColumnChoices =
        {
            (ColumnMode.KeysAndLookups, "Keys and lookups"),
            (ColumnMode.KeysLookupsAndCustom, "Keys, lookups and custom columns"),
            (ColumnMode.All, "All columns"),
        };

        static readonly (RowLabel Value, string Text)[] RowLabelChoices =
        {
            (RowLabel.DisplayAndLogical, "Display and logical name"),
            (RowLabel.Display, "Display name"),
            (RowLabel.Logical, "Logical name"),
        };

        static readonly (LayoutDirection Value, string Text)[] DirectionChoices =
        {
            (LayoutDirection.LeftToRight, "Left"),
            (LayoutDirection.TopToBottom, "Top"),
        };

        readonly Timer rebuildTimer = new Timer { Interval = 250 };
        PluginSettings settings = new PluginSettings();
        List<SolutionSummary> solutions = new List<SolutionSummary>();
        ErdSnapshot snapshot;
        string snapshotOrigin;
        ErdDocument document;
        bool loaded;
        bool showingOptions;
        bool building;
        bool rebuildPending;

        public SolutionErdControl()
        {
            InitializeComponent();
            components.Add(rebuildTimer);
            rebuildTimer.Tick += (s, e) =>
            {
                rebuildTimer.Stop();
                Rebuild();
            };

            cboColumns.Items.AddRange(ColumnChoices.Select(c => (object)c.Text).ToArray());
            cboRowLabels.Items.AddRange(RowLabelChoices.Select(c => (object)c.Text).ToArray());
            cboDirection.Items.AddRange(DirectionChoices.Select(c => (object)c.Text).ToArray());

            toolTip.SetToolTip(cboColumns, "Which columns each table box lists. Lines always leave from the lookup's row.");
            toolTip.SetToolTip(cboDirection, "Where parent tables go relative to the tables that look them up.");
            toolTip.SetToolTip(chkSplit, "Split a big solution into pages of closely related tables, with an overview page first.");
            toolTip.SetToolTip(numMaxTables, "Most tables drawn in full on one page.");
            toolTip.SetToolTip(numHub, "A table with at least this many relationships is a hub: it lives on one page, and other pages show its name on the lookup row.");
            toolTip.SetToolTip(chkOutside, "Show lookups to tables that aren't in the solution, such as Account or Contact.");
            toolTip.SetToolTip(chkStubs, "Draw tables that live on another page or outside the solution as small boxes with a line, instead of \"→ Table\" on the lookup row.");
            toolTip.SetToolTip(chkShortcuts, "A shortcut lookup points where another lookup already leads (Task → Project when Task → Milestone → Project). By default it is row text, \"→ Project (via Milestone)\".");
            toolTip.SetToolTip(chkIncoming, "On split pages, also draw lookups from tables on other pages into this page's tables.");
            toolTip.SetToolTip(chkSystem, "Owner, created by, modified by, business unit, currency and similar relationships every table has.");
            toolTip.SetToolTip(chkExternal, "Show tables outside the solution whose lookups point at the solution's tables.");
        }

        void SolutionErdControl_Load(object sender, EventArgs e)
        {
            if (SettingsManager.Instance.TryLoad(GetType(), out PluginSettings saved) && saved?.Options != null)
            {
                settings = saved;
            }

            SetCueBanner(txtFilter, "Filter solutions");
            ShowOptions(settings.Options);
            loaded = true;
            UpdateState();
            if (Service != null) LoadSolutions();
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            solutions.Clear();
            FillSolutions();
            if (loaded && newService != null) LoadSolutions();
            UpdateState();
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSettings();
            base.ClosingPlugin(info);
        }

        // Solutions

        void tsbLoadSolutions_Click(object sender, EventArgs e) => ExecuteMethod(LoadSolutions);

        void LoadSolutions()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) => args.Result = new MetadataReader(Service).ListSolutions(),
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowErrorDialog(args.Error, "Couldn't load solutions");
                        return;
                    }

                    solutions = (List<SolutionSummary>)args.Result;
                    FillSolutions();
                    SetStatus($"{solutions.Count} solutions in {EnvironmentName()}.");
                },
            });
        }

        void FillSolutions()
        {
            var selected = SelectedSolution()?.Id;
            var filter = txtFilter.Text.Trim();
            lvSolutions.BeginUpdate();
            lvSolutions.Items.Clear();
            foreach (var solution in solutions.Where(s => filter.Length == 0
                         || Contains(s.FriendlyName, filter) || Contains(s.UniqueName, filter) || Contains(s.Publisher, filter)))
            {
                var item = new ListViewItem(solution.FriendlyName ?? solution.UniqueName)
                {
                    Tag = solution,
                    ToolTipText = solution.UniqueName + (solution.Publisher != null ? "\npublisher: " + solution.Publisher : ""),
                };
                item.SubItems.Add(solution.Version);
                item.SubItems.Add(solution.IsManaged ? "Yes" : "");
                item.Selected = solution.Id == selected;
                lvSolutions.Items.Add(item);
            }

            lvSolutions.EndUpdate();
            UpdateState();
        }

        static bool Contains(string text, string part) => text != null && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        SolutionSummary SelectedSolution() => lvSolutions.SelectedItems.Count == 1 ? lvSolutions.SelectedItems[0].Tag as SolutionSummary : null;

        void txtFilter_TextChanged(object sender, EventArgs e) => FillSolutions();

        void lvSolutions_SelectedIndexChanged(object sender, EventArgs e) => UpdateState();

        void lvSolutions_DoubleClick(object sender, EventArgs e) => btnDraw.PerformClick();

        void btnDraw_Click(object sender, EventArgs e)
        {
            var solution = SelectedSolution();
            if (solution == null) return;
            ExecuteMethod(() => ReadSolution(solution));
        }

        void ReadSolution(SolutionSummary solution)
        {
            var environment = EnvironmentName();
            var options = settings.Options.Clone();
            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Reading {solution}...",
                Work = (worker, args) =>
                {
                    var read = new MetadataReader(Service).Read(solution, environment, message => worker.ReportProgress(0, message));
                    worker.ReportProgress(0, "Laying out the diagram...");
                    args.Result = Tuple.Create(read, ErdBuilder.Build(read, options));
                },
                ProgressChanged = args => SetWorkingMessage(args.UserState as string),
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowErrorDialog(args.Error, $"Couldn't read {solution}");
                        return;
                    }

                    var result = (Tuple<ErdSnapshot, ErdDocument>)args.Result;
                    snapshot = result.Item1;
                    snapshotOrigin = environment;
                    ShowDocument(result.Item2, keepPage: false);
                },
            });
        }

        // Metadata files

        void tsbOpenSnapshot_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Open solution metadata",
                Filter = "Solution metadata (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = settings.LastFolder ?? "",
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                ErdSnapshot opened;
                try
                {
                    opened = ErdSnapshot.Load(dialog.FileName);
                    if (opened?.Tables == null || opened.Tables.Count == 0) throw new InvalidDataException("The file has no tables in it.");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog(ex, $"Couldn't open {Path.GetFileName(dialog.FileName)}", allownewissue: false);
                    return;
                }

                settings.LastFolder = Path.GetDirectoryName(dialog.FileName);
                snapshot = opened;
                snapshotOrigin = Path.GetFileName(dialog.FileName);
                document = null;
                Rebuild();
            }
        }

        void tsbSaveSnapshot_Click(object sender, EventArgs e)
        {
            if (snapshot == null) return;
            using (var dialog = new SaveFileDialog
            {
                Title = "Save solution metadata",
                Filter = "Solution metadata (*.json)|*.json",
                FileName = FileStem() + "-metadata.json",
                InitialDirectory = settings.LastFolder ?? "",
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    snapshot.Save(dialog.FileName);
                }
                catch (Exception ex)
                {
                    ShowErrorDialog(ex, "Couldn't save the metadata", allownewissue: false);
                    return;
                }

                settings.LastFolder = Path.GetDirectoryName(dialog.FileName);
                SetStatus($"Saved {Path.GetFileName(dialog.FileName)}.");
            }
        }

        void tsbExportDrawio_Click(object sender, EventArgs e)
        {
            if (document == null) return;
            using (var dialog = new SaveFileDialog
            {
                Title = "Export to draw.io",
                Filter = "draw.io diagram (*.drawio)|*.drawio",
                FileName = FileStem() + "-erd.drawio",
                InitialDirectory = settings.LastFolder ?? "",
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    DrawioWriter.Write(document, dialog.FileName);
                }
                catch (Exception ex)
                {
                    ShowErrorDialog(ex, "Couldn't export the diagram", allownewissue: false);
                    return;
                }

                settings.LastFolder = Path.GetDirectoryName(dialog.FileName);
                var name = Path.GetFileName(dialog.FileName);
                SetStatus($"Exported {document.Pages.Count} pages to {name}.");
                var answer = MessageBox.Show(this, $"Saved {name} with {document.Pages.Count} pages.\n\nOpen it now?", "Export to draw.io",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer == DialogResult.Yes) OpenFile(dialog.FileName);
            }
        }

        void OpenFile(string path)
        {
            try
            {
                Process.Start(path);
            }
            catch (Win32Exception)
            {
                MessageBox.Show(this, "Nothing on this PC opens .drawio files yet. Open it in draw.io desktop or at app.diagrams.net.",
                    "Export to draw.io", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        string FileStem()
        {
            var name = snapshot?.SolutionName ?? "solution";
            var safe = new string(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            return $"{safe}-{DateTime.Now:yyyyMMdd-HHmm}";
        }

        // Options

        void ShowOptions(ErdOptions options)
        {
            showingOptions = true;
            cboColumns.SelectedIndex = Math.Max(0, Array.FindIndex(ColumnChoices, c => c.Value == options.Columns));
            cboRowLabels.SelectedIndex = Math.Max(0, Array.FindIndex(RowLabelChoices, c => c.Value == options.RowLabel));
            cboDirection.SelectedIndex = Math.Max(0, Array.FindIndex(DirectionChoices, c => c.Value == options.Direction));
            chkSplit.Checked = options.SplitIntoPages;
            numMaxTables.Value = Math.Max(numMaxTables.Minimum, Math.Min(numMaxTables.Maximum, options.MaxTablesPerPage));
            numHub.Value = Math.Max(numHub.Minimum, Math.Min(numHub.Maximum, options.HubDegreeThreshold));
            chkOutside.Checked = options.ShowOutsideTables;
            chkStubs.Checked = options.OffPageLinks == OffPageLinks.Stubs;
            chkShortcuts.Checked = options.DrawShortcutLookups;
            chkIncoming.Checked = options.ShowIncomingFromOtherPages;
            chkSystem.Checked = options.ShowSystemRelationships;
            chkExternal.Checked = options.ShowExternalChildren;
            showingOptions = false;
            UpdateOptionStates();
        }

        void Option_Changed(object sender, EventArgs e)
        {
            if (showingOptions) return;
            var options = settings.Options;
            options.Columns = ColumnChoices[Math.Max(0, cboColumns.SelectedIndex)].Value;
            options.RowLabel = RowLabelChoices[Math.Max(0, cboRowLabels.SelectedIndex)].Value;
            options.Direction = DirectionChoices[Math.Max(0, cboDirection.SelectedIndex)].Value;
            options.SplitIntoPages = chkSplit.Checked;
            options.MaxTablesPerPage = (int)numMaxTables.Value;
            options.HubDegreeThreshold = (int)numHub.Value;
            options.ShowOutsideTables = chkOutside.Checked;
            options.OffPageLinks = chkStubs.Checked ? OffPageLinks.Stubs : OffPageLinks.Labels;
            options.DrawShortcutLookups = chkShortcuts.Checked;
            options.ShowIncomingFromOtherPages = chkIncoming.Checked;
            options.ShowSystemRelationships = chkSystem.Checked;
            options.ShowExternalChildren = chkExternal.Checked;
            UpdateOptionStates();
            if (snapshot == null) return;
            rebuildTimer.Stop();
            rebuildTimer.Start();
        }

        void UpdateOptionStates()
        {
            numMaxTables.Enabled = lblMaxTables.Enabled = chkSplit.Checked;
            numHub.Enabled = lblHub.Enabled = chkSplit.Checked;
            chkIncoming.Enabled = chkSplit.Checked;
        }

        void SaveSettings()
        {
            try
            {
                SettingsManager.Instance.Save(GetType(), settings);
            }
            catch (Exception ex)
            {
                LogWarning("Couldn't save settings: {0}", ex.Message);
            }
        }

        // Drawing

        /// <summary>Lays the cached metadata out again with the current options, off the UI thread.</summary>
        void Rebuild()
        {
            if (snapshot == null) return;
            if (building)
            {
                rebuildPending = true;
                return;
            }

            building = true;
            var source = snapshot;
            var options = settings.Options.Clone();
            var keepPage = document != null;
            SetStatus("Laying out the diagram...");
            Task.Run(() => ErdBuilder.Build(source, options)).ContinueWith(task =>
            {
                building = false;
                if (task.Exception != null)
                {
                    ShowErrorDialog(task.Exception.GetBaseException(), "Couldn't lay out the diagram");
                }
                else if (source == snapshot)
                {
                    ShowDocument(task.Result, keepPage);
                }

                if (rebuildPending)
                {
                    rebuildPending = false;
                    Rebuild();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void ShowDocument(ErdDocument shown, bool keepPage)
        {
            var previous = canvas.Page?.Name;
            document = shown;
            if (!keepPage) canvas.FitToWindow = true;

            tsPages.SuspendLayout();
            tsPages.Items.Clear();
            foreach (var page in document.Pages)
            {
                var tab = new ToolStripButton(page.Name)
                {
                    Tag = page,
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    ToolTipText = DescribePage(page),
                    Margin = new Padding(0, 1, 2, 1),
                };
                tab.Click += (s, e) => SelectPage(document.Pages.IndexOf((PageLayout)((ToolStripItem)s).Tag));
                tsPages.Items.Add(tab);
            }

            tsPages.ResumeLayout();
            var index = keepPage ? document.Pages.FindIndex(p => p.Name == previous) : -1;
            SelectPage(Math.Max(0, index));

            var tables = snapshot.Tables.Count(t => t.InSolution && !t.IsIntersect);
            var captured = snapshot.CapturedUtc == default ? "" : $", read {snapshot.CapturedUtc.ToLocalTime():d MMM yyyy HH:mm}";
            SetStatus($"{snapshot.SolutionDisplayName ?? snapshot.SolutionName}: {tables} tables on {Pages(document.Pages.Count)}, from {snapshotOrigin}{captured}.");
            UpdateState();
        }

        static string Pages(int count) => count == 1 ? "1 page" : $"{count} pages";

        void SelectPage(int index)
        {
            var page = document != null && index >= 0 && index < document.Pages.Count ? document.Pages[index] : null;
            foreach (ToolStripButton tab in tsPages.Items) tab.Checked = tab.Tag == page;
            canvas.Page = page;
            lblPageInfo.Text = page == null ? "" : DescribePage(page);
        }

        static string DescribePage(PageLayout page)
        {
            if (page.IsOverview) return $"{page.Boxes.Count(b => b.IsArea)} subject areas";
            var tables = page.Boxes.Count(b => !b.IsStub && !b.IsArea);
            return $"{tables} tables, {page.Edges.Count} lines";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+PageDown / Ctrl+PageUp move between pages, as in Visio and Excel.
            if (document != null && (keyData == (Keys.Control | Keys.PageDown) || keyData == (Keys.Control | Keys.PageUp)))
            {
                var index = document.Pages.IndexOf(canvas.Page) + ((keyData & Keys.KeyCode) == Keys.PageDown ? 1 : -1);
                if (index >= 0 && index < document.Pages.Count) SelectPage(index);
                return true;
            }

            if (document != null && (keyData & Keys.Modifiers) == Keys.Control)
            {
                switch (keyData & Keys.KeyCode)
                {
                    case Keys.D0:
                    case Keys.NumPad0:
                        canvas.FitToWindow = true;
                        return true;
                    case Keys.D1:
                    case Keys.NumPad1:
                        canvas.SetZoom(1f);
                        return true;
                    case Keys.Oemplus:
                    case Keys.Add:
                        canvas.ZoomIn();
                        return true;
                    case Keys.OemMinus:
                    case Keys.Subtract:
                        canvas.ZoomOut();
                        return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void canvas_ZoomChanged(object sender, EventArgs e) => tslZoom.Text = $"{canvas.Zoom * 100:0}%";

        void tsbZoomIn_Click(object sender, EventArgs e) => canvas.ZoomIn();

        void tsbZoomOut_Click(object sender, EventArgs e) => canvas.ZoomOut();

        void tsbZoomActual_Click(object sender, EventArgs e) => canvas.SetZoom(1f);

        void tsbZoomFit_Click(object sender, EventArgs e) => canvas.FitToWindow = true;

        void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        // State

        void UpdateState()
        {
            var hasDocument = document != null;
            btnDraw.Enabled = SelectedSolution() != null;
            tsbExportDrawio.Enabled = hasDocument;
            tsbSaveSnapshot.Enabled = snapshot != null;
            tsbZoomIn.Enabled = tsbZoomOut.Enabled = tsbZoomActual.Enabled = tsbZoomFit.Enabled = hasDocument;
            grpSolutions.Text = Service == null ? "Solution (not connected)" : $"Solution in {EnvironmentName()}";
        }

        string EnvironmentName() => ConnectionDetail?.OrganizationFriendlyName ?? ConnectionDetail?.ConnectionName ?? "the environment";

        void SetStatus(string text) => lblStatus.Text = text;

        static void SetCueBanner(TextBox box, string text) => SendMessage(box.Handle, 0x1501, (IntPtr)1, text);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
    }
}
