namespace SolutionErdXrmToolBoxPlugin
{
    partial class SolutionErdControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.tssClose = new System.Windows.Forms.ToolStripSeparator();
            this.tsbLoadSolutions = new System.Windows.Forms.ToolStripButton();
            this.tsbOpenSnapshot = new System.Windows.Forms.ToolStripButton();
            this.tssFiles = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExportDrawio = new System.Windows.Forms.ToolStripButton();
            this.tsbSaveSnapshot = new System.Windows.Forms.ToolStripButton();
            this.tssZoom = new System.Windows.Forms.ToolStripSeparator();
            this.tslZoom = new System.Windows.Forms.ToolStripLabel();
            this.tsbZoomIn = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomActual = new System.Windows.Forms.ToolStripButton();
            this.tsbZoomFit = new System.Windows.Forms.ToolStripButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.grpSolutions = new System.Windows.Forms.GroupBox();
            this.lvSolutions = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colVersion = new System.Windows.Forms.ColumnHeader();
            this.colManaged = new System.Windows.Forms.ColumnHeader();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.btnDraw = new System.Windows.Forms.Button();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.tlpOptions = new System.Windows.Forms.TableLayoutPanel();
            this.lblColumns = new System.Windows.Forms.Label();
            this.cboColumns = new System.Windows.Forms.ComboBox();
            this.lblRowLabels = new System.Windows.Forms.Label();
            this.cboRowLabels = new System.Windows.Forms.ComboBox();
            this.lblDirection = new System.Windows.Forms.Label();
            this.cboDirection = new System.Windows.Forms.ComboBox();
            this.chkSplit = new System.Windows.Forms.CheckBox();
            this.lblMaxTables = new System.Windows.Forms.Label();
            this.numMaxTables = new System.Windows.Forms.NumericUpDown();
            this.lblHub = new System.Windows.Forms.Label();
            this.numHub = new System.Windows.Forms.NumericUpDown();
            this.chkOutside = new System.Windows.Forms.CheckBox();
            this.chkStubs = new System.Windows.Forms.CheckBox();
            this.chkShortcuts = new System.Windows.Forms.CheckBox();
            this.chkIncoming = new System.Windows.Forms.CheckBox();
            this.chkSystem = new System.Windows.Forms.CheckBox();
            this.chkExternal = new System.Windows.Forms.CheckBox();
            this.tsPages = new System.Windows.Forms.ToolStrip();
            this.canvas = new SolutionErdXrmToolBoxPlugin.ErdCanvas();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblPageInfo = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpSolutions.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.tlpOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxTables)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHub)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(16, 16);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbClose,
            this.tssClose,
            this.tsbLoadSolutions,
            this.tsbOpenSnapshot,
            this.tssFiles,
            this.tsbExportDrawio,
            this.tsbSaveSnapshot,
            this.tsbZoomActual,
            this.tsbZoomFit,
            this.tssZoom,
            this.tsbZoomIn,
            this.tslZoom,
            this.tsbZoomOut});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.toolStrip.Size = new System.Drawing.Size(1100, 29);
            this.toolStrip.TabIndex = 0;
            //
            // tsbClose
            //
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(40, 22);
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            //
            // tssClose
            //
            this.tssClose.Name = "tssClose";
            this.tssClose.Size = new System.Drawing.Size(6, 25);
            //
            // tsbLoadSolutions
            //
            this.tsbLoadSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadSolutions.Name = "tsbLoadSolutions";
            this.tsbLoadSolutions.Size = new System.Drawing.Size(92, 22);
            this.tsbLoadSolutions.Text = "Load solutions";
            this.tsbLoadSolutions.ToolTipText = "List the visible solutions in the connected environment";
            this.tsbLoadSolutions.Click += new System.EventHandler(this.tsbLoadSolutions_Click);
            //
            // tsbOpenSnapshot
            //
            this.tsbOpenSnapshot.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbOpenSnapshot.Name = "tsbOpenSnapshot";
            this.tsbOpenSnapshot.Size = new System.Drawing.Size(98, 22);
            this.tsbOpenSnapshot.Text = "Open metadata...";
            this.tsbOpenSnapshot.ToolTipText = "Draw from a metadata file saved earlier, without a connection";
            this.tsbOpenSnapshot.Click += new System.EventHandler(this.tsbOpenSnapshot_Click);
            //
            // tssFiles
            //
            this.tssFiles.Name = "tssFiles";
            this.tssFiles.Size = new System.Drawing.Size(6, 25);
            //
            // tsbExportDrawio
            //
            this.tsbExportDrawio.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExportDrawio.Enabled = false;
            this.tsbExportDrawio.Name = "tsbExportDrawio";
            this.tsbExportDrawio.Size = new System.Drawing.Size(101, 22);
            this.tsbExportDrawio.Text = "Export draw.io...";
            this.tsbExportDrawio.ToolTipText = "Save every page as a draw.io diagram";
            this.tsbExportDrawio.Click += new System.EventHandler(this.tsbExportDrawio_Click);
            //
            // tsbSaveSnapshot
            //
            this.tsbSaveSnapshot.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSaveSnapshot.Enabled = false;
            this.tsbSaveSnapshot.Name = "tsbSaveSnapshot";
            this.tsbSaveSnapshot.Size = new System.Drawing.Size(96, 22);
            this.tsbSaveSnapshot.Text = "Save metadata...";
            this.tsbSaveSnapshot.ToolTipText = "Save the solution's tables and relationships as JSON, to draw again later without a connection";
            this.tsbSaveSnapshot.Click += new System.EventHandler(this.tsbSaveSnapshot_Click);
            //
            // tssZoom
            //
            this.tssZoom.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tssZoom.Name = "tssZoom";
            this.tssZoom.Size = new System.Drawing.Size(6, 25);
            //
            // tslZoom
            //
            this.tslZoom.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tslZoom.AutoSize = false;
            this.tslZoom.Name = "tslZoom";
            this.tslZoom.Size = new System.Drawing.Size(44, 22);
            this.tslZoom.Text = "100%";
            this.tslZoom.ToolTipText = "Zoom. Ctrl+wheel zooms, dragging pans.";
            //
            // tsbZoomIn
            //
            this.tsbZoomIn.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbZoomIn.Enabled = false;
            this.tsbZoomIn.Name = "tsbZoomIn";
            this.tsbZoomIn.Size = new System.Drawing.Size(23, 22);
            this.tsbZoomIn.Text = "+";
            this.tsbZoomIn.ToolTipText = "Zoom in (Ctrl+wheel)";
            this.tsbZoomIn.Click += new System.EventHandler(this.tsbZoomIn_Click);
            //
            // tsbZoomOut
            //
            this.tsbZoomOut.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbZoomOut.Enabled = false;
            this.tsbZoomOut.Name = "tsbZoomOut";
            this.tsbZoomOut.Size = new System.Drawing.Size(23, 22);
            this.tsbZoomOut.Text = "−";
            this.tsbZoomOut.ToolTipText = "Zoom out (Ctrl+wheel)";
            this.tsbZoomOut.Click += new System.EventHandler(this.tsbZoomOut_Click);
            //
            // tsbZoomActual
            //
            this.tsbZoomActual.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbZoomActual.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbZoomActual.Enabled = false;
            this.tsbZoomActual.Name = "tsbZoomActual";
            this.tsbZoomActual.Size = new System.Drawing.Size(39, 22);
            this.tsbZoomActual.Text = "1:1";
            this.tsbZoomActual.ToolTipText = "Actual size";
            this.tsbZoomActual.Click += new System.EventHandler(this.tsbZoomActual_Click);
            //
            // tsbZoomFit
            //
            this.tsbZoomFit.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbZoomFit.CheckOnClick = false;
            this.tsbZoomFit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbZoomFit.Enabled = false;
            this.tsbZoomFit.Name = "tsbZoomFit";
            this.tsbZoomFit.Size = new System.Drawing.Size(26, 22);
            this.tsbZoomFit.Text = "Fit";
            this.tsbZoomFit.ToolTipText = "Fit the page to the window";
            this.tsbZoomFit.Click += new System.EventHandler(this.tsbZoomFit_Click);
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 29);
            this.splitMain.Name = "splitMain";
            //
            // splitMain.Panel1
            //
            this.splitMain.Panel1.Controls.Add(this.grpSolutions);
            this.splitMain.Panel1.Controls.Add(this.grpOptions);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(6, 4, 0, 6);
            //
            // splitMain.Panel2
            //
            this.splitMain.Panel2.Controls.Add(this.canvas);
            this.splitMain.Panel2.Controls.Add(this.tsPages);
            this.splitMain.Size = new System.Drawing.Size(1100, 649);
            this.splitMain.SplitterDistance = 330;
            this.splitMain.TabIndex = 1;
            //
            // grpSolutions
            //
            this.grpSolutions.Controls.Add(this.lvSolutions);
            this.grpSolutions.Controls.Add(this.txtFilter);
            this.grpSolutions.Controls.Add(this.btnDraw);
            this.grpSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSolutions.Name = "grpSolutions";
            this.grpSolutions.Padding = new System.Windows.Forms.Padding(6);
            this.grpSolutions.TabIndex = 0;
            this.grpSolutions.TabStop = false;
            this.grpSolutions.Text = "Solution";
            //
            // lvSolutions
            //
            this.lvSolutions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colVersion,
            this.colManaged});
            this.lvSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvSolutions.FullRowSelect = true;
            this.lvSolutions.HideSelection = false;
            this.lvSolutions.MultiSelect = false;
            this.lvSolutions.Name = "lvSolutions";
            this.lvSolutions.ShowItemToolTips = true;
            this.lvSolutions.TabIndex = 1;
            this.lvSolutions.UseCompatibleStateImageBehavior = false;
            this.lvSolutions.View = System.Windows.Forms.View.Details;
            this.lvSolutions.SelectedIndexChanged += new System.EventHandler(this.lvSolutions_SelectedIndexChanged);
            this.lvSolutions.DoubleClick += new System.EventHandler(this.lvSolutions_DoubleClick);
            //
            // colName
            //
            this.colName.Text = "Name";
            this.colName.Width = 165;
            //
            // colVersion
            //
            this.colVersion.Text = "Version";
            this.colVersion.Width = 70;
            //
            // colManaged
            //
            this.colManaged.Text = "Managed";
            this.colManaged.Width = 66;
            //
            // txtFilter
            //
            this.txtFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.TabIndex = 0;
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);
            //
            // btnDraw
            //
            this.btnDraw.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnDraw.Enabled = false;
            this.btnDraw.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnDraw.Name = "btnDraw";
            this.btnDraw.Size = new System.Drawing.Size(300, 34);
            this.btnDraw.TabIndex = 2;
            this.btnDraw.Text = "Draw ERD";
            this.btnDraw.UseVisualStyleBackColor = true;
            this.btnDraw.Click += new System.EventHandler(this.btnDraw_Click);
            //
            // grpOptions
            //
            this.grpOptions.Controls.Add(this.tlpOptions);
            this.grpOptions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Padding = new System.Windows.Forms.Padding(6);
            this.grpOptions.Size = new System.Drawing.Size(324, 330);
            this.grpOptions.TabIndex = 1;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "Diagram options";
            //
            // tlpOptions
            //
            this.tlpOptions.ColumnCount = 2;
            this.tlpOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOptions.Controls.Add(this.lblColumns, 0, 0);
            this.tlpOptions.Controls.Add(this.cboColumns, 1, 0);
            this.tlpOptions.Controls.Add(this.lblRowLabels, 0, 1);
            this.tlpOptions.Controls.Add(this.cboRowLabels, 1, 1);
            this.tlpOptions.Controls.Add(this.lblDirection, 0, 2);
            this.tlpOptions.Controls.Add(this.cboDirection, 1, 2);
            this.tlpOptions.Controls.Add(this.chkSplit, 0, 3);
            this.tlpOptions.Controls.Add(this.lblMaxTables, 0, 4);
            this.tlpOptions.Controls.Add(this.numMaxTables, 1, 4);
            this.tlpOptions.Controls.Add(this.lblHub, 0, 5);
            this.tlpOptions.Controls.Add(this.numHub, 1, 5);
            this.tlpOptions.Controls.Add(this.chkOutside, 0, 6);
            this.tlpOptions.Controls.Add(this.chkStubs, 0, 7);
            this.tlpOptions.Controls.Add(this.chkShortcuts, 0, 8);
            this.tlpOptions.Controls.Add(this.chkIncoming, 0, 9);
            this.tlpOptions.Controls.Add(this.chkSystem, 0, 10);
            this.tlpOptions.Controls.Add(this.chkExternal, 0, 11);
            this.tlpOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpOptions.Name = "tlpOptions";
            this.tlpOptions.RowCount = 12;
            for (var i = 0; i < 12; i++)
            {
                this.tlpOptions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, i < 6 ? 27F : 23F));
            }
            this.tlpOptions.TabIndex = 0;
            //
            // lblColumns
            //
            this.lblColumns.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblColumns.AutoSize = true;
            this.lblColumns.Name = "lblColumns";
            this.lblColumns.Text = "Columns";
            //
            // cboColumns
            //
            this.cboColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboColumns.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboColumns.Name = "cboColumns";
            this.cboColumns.TabIndex = 0;
            this.cboColumns.SelectedIndexChanged += new System.EventHandler(this.Option_Changed);
            //
            // lblRowLabels
            //
            this.lblRowLabels.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRowLabels.AutoSize = true;
            this.lblRowLabels.Name = "lblRowLabels";
            this.lblRowLabels.Text = "Names";
            //
            // cboRowLabels
            //
            this.cboRowLabels.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboRowLabels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRowLabels.Name = "cboRowLabels";
            this.cboRowLabels.TabIndex = 1;
            this.cboRowLabels.SelectedIndexChanged += new System.EventHandler(this.Option_Changed);
            //
            // lblDirection
            //
            this.lblDirection.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDirection.AutoSize = true;
            this.lblDirection.Name = "lblDirection";
            this.lblDirection.Text = "Parents on the";
            //
            // cboDirection
            //
            this.cboDirection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDirection.Name = "cboDirection";
            this.cboDirection.TabIndex = 2;
            this.cboDirection.SelectedIndexChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkSplit
            //
            this.chkSplit.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkSplit.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkSplit, 2);
            this.chkSplit.Name = "chkSplit";
            this.chkSplit.TabIndex = 3;
            this.chkSplit.Text = "Split into subject-area pages";
            this.chkSplit.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // lblMaxTables
            //
            this.lblMaxTables.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMaxTables.AutoSize = true;
            this.lblMaxTables.Name = "lblMaxTables";
            this.lblMaxTables.Text = "    Tables per page";
            //
            // numMaxTables
            //
            this.numMaxTables.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.numMaxTables.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.numMaxTables.Minimum = new decimal(new int[] { 4, 0, 0, 0 });
            this.numMaxTables.Name = "numMaxTables";
            this.numMaxTables.Size = new System.Drawing.Size(60, 23);
            this.numMaxTables.TabIndex = 4;
            this.numMaxTables.Value = new decimal(new int[] { 15, 0, 0, 0 });
            this.numMaxTables.ValueChanged += new System.EventHandler(this.Option_Changed);
            //
            // lblHub
            //
            this.lblHub.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHub.AutoSize = true;
            this.lblHub.Name = "lblHub";
            this.lblHub.Text = "    Hub at (links)";
            //
            // numHub
            //
            this.numHub.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.numHub.Maximum = new decimal(new int[] { 99, 0, 0, 0 });
            this.numHub.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.numHub.Name = "numHub";
            this.numHub.Size = new System.Drawing.Size(60, 23);
            this.numHub.TabIndex = 5;
            this.numHub.Value = new decimal(new int[] { 6, 0, 0, 0 });
            this.numHub.ValueChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkOutside
            //
            this.chkOutside.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkOutside.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkOutside, 2);
            this.chkOutside.Name = "chkOutside";
            this.chkOutside.TabIndex = 6;
            this.chkOutside.Text = "Show lookups to tables outside the solution";
            this.chkOutside.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkStubs
            //
            this.chkStubs.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkStubs.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkStubs, 2);
            this.chkStubs.Name = "chkStubs";
            this.chkStubs.TabIndex = 7;
            this.chkStubs.Text = "Draw off-page tables as boxes, not row text";
            this.chkStubs.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkShortcuts
            //
            this.chkShortcuts.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkShortcuts.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkShortcuts, 2);
            this.chkShortcuts.Name = "chkShortcuts";
            this.chkShortcuts.TabIndex = 8;
            this.chkShortcuts.Text = "Draw lines for shortcut lookups";
            this.chkShortcuts.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkIncoming
            //
            this.chkIncoming.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkIncoming.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkIncoming, 2);
            this.chkIncoming.Name = "chkIncoming";
            this.chkIncoming.TabIndex = 9;
            this.chkIncoming.Text = "Draw lookups from tables on other pages";
            this.chkIncoming.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkSystem
            //
            this.chkSystem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkSystem.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkSystem, 2);
            this.chkSystem.Name = "chkSystem";
            this.chkSystem.TabIndex = 10;
            this.chkSystem.Text = "Show system relationships (owner, created by...)";
            this.chkSystem.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // chkExternal
            //
            this.chkExternal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkExternal.AutoSize = true;
            this.tlpOptions.SetColumnSpan(this.chkExternal, 2);
            this.chkExternal.Name = "chkExternal";
            this.chkExternal.TabIndex = 11;
            this.chkExternal.Text = "Show outside tables that look up these tables";
            this.chkExternal.CheckedChanged += new System.EventHandler(this.Option_Changed);
            //
            // tsPages
            //
            this.tsPages.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tsPages.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsPages.Name = "tsPages";
            this.tsPages.Padding = new System.Windows.Forms.Padding(4, 1, 4, 1);
            this.tsPages.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.tsPages.TabIndex = 1;
            //
            // canvas
            //
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.EmptyText = "Pick a solution and click Draw ERD, or open a saved metadata file.";
            this.canvas.Name = "canvas";
            this.canvas.TabIndex = 0;
            this.canvas.ZoomChanged += new System.EventHandler(this.canvas_ZoomChanged);
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblPageInfo});
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Spring = true;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblPageInfo
            //
            this.lblPageInfo.Name = "lblPageInfo";
            //
            // SolutionErdControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "SolutionErdControl";
            this.Size = new System.Drawing.Size(1100, 700);
            this.Load += new System.EventHandler(this.SolutionErdControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.grpSolutions.ResumeLayout(false);
            this.grpSolutions.PerformLayout();
            this.grpOptions.ResumeLayout(false);
            this.tlpOptions.ResumeLayout(false);
            this.tlpOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxTables)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHub)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripSeparator tssClose;
        private System.Windows.Forms.ToolStripButton tsbLoadSolutions;
        private System.Windows.Forms.ToolStripButton tsbOpenSnapshot;
        private System.Windows.Forms.ToolStripSeparator tssFiles;
        private System.Windows.Forms.ToolStripButton tsbExportDrawio;
        private System.Windows.Forms.ToolStripButton tsbSaveSnapshot;
        private System.Windows.Forms.ToolStripSeparator tssZoom;
        private System.Windows.Forms.ToolStripLabel tslZoom;
        private System.Windows.Forms.ToolStripButton tsbZoomIn;
        private System.Windows.Forms.ToolStripButton tsbZoomOut;
        private System.Windows.Forms.ToolStripButton tsbZoomActual;
        private System.Windows.Forms.ToolStripButton tsbZoomFit;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpSolutions;
        private System.Windows.Forms.ListView lvSolutions;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colVersion;
        private System.Windows.Forms.ColumnHeader colManaged;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Button btnDraw;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.TableLayoutPanel tlpOptions;
        private System.Windows.Forms.Label lblColumns;
        private System.Windows.Forms.ComboBox cboColumns;
        private System.Windows.Forms.Label lblRowLabels;
        private System.Windows.Forms.ComboBox cboRowLabels;
        private System.Windows.Forms.Label lblDirection;
        private System.Windows.Forms.ComboBox cboDirection;
        private System.Windows.Forms.CheckBox chkSplit;
        private System.Windows.Forms.Label lblMaxTables;
        private System.Windows.Forms.NumericUpDown numMaxTables;
        private System.Windows.Forms.Label lblHub;
        private System.Windows.Forms.NumericUpDown numHub;
        private System.Windows.Forms.CheckBox chkOutside;
        private System.Windows.Forms.CheckBox chkStubs;
        private System.Windows.Forms.CheckBox chkShortcuts;
        private System.Windows.Forms.CheckBox chkIncoming;
        private System.Windows.Forms.CheckBox chkSystem;
        private System.Windows.Forms.CheckBox chkExternal;
        private System.Windows.Forms.ToolStrip tsPages;
        private ErdCanvas canvas;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblPageInfo;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
