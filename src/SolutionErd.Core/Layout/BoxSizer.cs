using System;
using System.Linq;
using SolutionErd.Model;

namespace SolutionErd.Layout
{
    /// <summary>Builds a box's title, rows and size from a page table.</summary>
    public static class BoxSizer
    {
        public static BoxLayout Size(PageTable pageTable, bool isArea, ErdOptions options, string id)
        {
            var table = pageTable.Table;
            var box = new BoxLayout
            {
                Id = id,
                Name = table.Name,
                Title = table.DisplayName,
                IsStub = pageTable.AsStub,
                IsArea = isArea,
            };

            if (isArea)
            {
                box.SubTitle = null;
                box.HeaderHeight = ErdStyle.AreaHeaderHeight;
            }
            else
            {
                var sub = SameName(table.DisplayName, table.Name) ? null : table.Name;
                if (!string.IsNullOrEmpty(pageTable.Note)) sub = sub == null ? pageTable.Note : sub + " · " + pageTable.Note;
                box.SubTitle = sub;
                box.HeaderHeight = ErdStyle.HeaderHeight;
            }

            var width = Math.Max(
                ErdStyle.MeasureText(box.Title, ErdStyle.TitleSize, bold: true),
                ErdStyle.MeasureText(box.SubTitle, ErdStyle.SubTitleSize)) + 2 * ErdStyle.TextPadding;

            if (!pageTable.AsStub)
            {
                var top = box.HeaderHeight;
                foreach (var row in table.Rows)
                {
                    var layout = new RowLayout
                    {
                        Name = row.Name,
                        Key = isArea ? null : KeyLabel(row.Kind),
                        Bold = row.Kind == RowKind.PrimaryKey,
                        Top = top,
                        Height = ErdStyle.RowHeight,
                    };
                    switch (isArea ? RowLabel.Display : options.RowLabel)
                    {
                        case RowLabel.Logical:
                            layout.Text = row.Name;
                            break;
                        case RowLabel.Display:
                            layout.Text = row.DisplayName;
                            break;
                        default:
                            layout.Text = row.DisplayName;
                            layout.SubText = SameName(row.DisplayName, row.Name) ? null : row.Name;
                            break;
                    }

                    if (pageTable.RowRefs.TryGetValue(row.Name, out var targets) && targets.Count > 0)
                    {
                        layout.Ref = "→ " + string.Join(", ", targets);
                    }

                    var rowWidth = (isArea ? 0 : ErdStyle.KeyColumnWidth)
                        + ErdStyle.MeasureText(layout.Text, ErdStyle.RowSize, layout.Bold)
                        + (layout.SubText == null ? 0 : 6 + ErdStyle.MeasureText(layout.SubText, ErdStyle.RowSubSize))
                        + (layout.Ref == null ? 0 : 10 + ErdStyle.MeasureText(layout.Ref, ErdStyle.RowSubSize))
                        + 2 * ErdStyle.TextPadding;
                    width = Math.Max(width, rowWidth);
                    box.Rows.Add(layout);
                    top += ErdStyle.RowHeight;
                }

                box.Height = top + (box.Rows.Count > 0 ? 4 : 0);
            }
            else
            {
                box.Height = ErdStyle.HeaderHeight;
            }

            box.Width = Math.Round(Math.Min(ErdStyle.MaxBoxWidth, Math.Max(ErdStyle.MinBoxWidth, width)));
            return box;
        }

        static string KeyLabel(RowKind kind)
        {
            switch (kind)
            {
                case RowKind.PrimaryKey: return "PK";
                case RowKind.Lookup: return "FK";
                default: return null;
            }
        }

        static bool SameName(string display, string logical) =>
            string.IsNullOrEmpty(display) || string.Equals(Normalize(display), Normalize(logical), StringComparison.OrdinalIgnoreCase);

        static string Normalize(string s) => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray());
    }
}
