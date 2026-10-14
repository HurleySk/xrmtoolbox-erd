using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using SolutionErd.Layout;

namespace SolutionErd.Render
{
    /// <summary>Draws a page layout with GDI+, for the plugin preview and for PNG checks.</summary>
    public static class PreviewRenderer
    {
        static readonly Color TableFill = ColorTranslator.FromHtml("#DAE8FC");
        static readonly Color TableStroke = ColorTranslator.FromHtml("#6C8EBF");
        static readonly Color AreaFill = ColorTranslator.FromHtml("#FFF2CC");
        static readonly Color AreaStroke = ColorTranslator.FromHtml("#D6B656");
        static readonly Color StubFill = ColorTranslator.FromHtml("#F5F5F5");
        static readonly Color StubStroke = ColorTranslator.FromHtml("#999999");
        static readonly Color TitleText = ColorTranslator.FromHtml("#1F2D3D");
        static readonly Color MutedText = ColorTranslator.FromHtml("#6B7785");
        static readonly Color LineColor = ColorTranslator.FromHtml("#4D4D4D");
        static readonly Color RefText = ColorTranslator.FromHtml("#3D6DB5");

        public static Bitmap Render(PageLayout page, float scale = 1f)
        {
            var width = Math.Max(1, (int)Math.Ceiling(page.Width * scale));
            var height = Math.Max(1, (int)Math.Ceiling(page.Height * scale));
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.ScaleTransform(scale, scale);
                Draw(g, page);
            }

            return bitmap;
        }

        public static void SavePng(PageLayout page, string path, float scale = 1f)
        {
            using (var bitmap = Render(page, scale))
            {
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        public static void Draw(Graphics g, PageLayout page)
        {
            foreach (var box in page.Boxes) DrawBox(g, box);
            foreach (var edge in page.Edges) DrawEdge(g, edge);
        }

        static void DrawBox(Graphics g, BoxLayout box)
        {
            var rect = new RectangleF((float)box.X, (float)box.Y, (float)box.Width, (float)box.Height);
            var fill = box.IsStub ? StubFill : box.IsArea ? AreaFill : TableFill;
            var stroke = box.IsStub ? StubStroke : box.IsArea ? AreaStroke : TableStroke;

            using (var white = new SolidBrush(Color.White))
            using (var headerBrush = new SolidBrush(fill))
            using (var pen = new Pen(stroke, 1.2f) { DashStyle = box.IsStub ? DashStyle.Dash : DashStyle.Solid })
            using (var titleFont = new Font(ErdStyle.FontFamily, ErdStyle.TitleSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var subFont = new Font(ErdStyle.FontFamily, ErdStyle.SubTitleSize, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var rowFont = new Font(ErdStyle.FontFamily, ErdStyle.RowSize, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var rowBold = new Font(ErdStyle.FontFamily, ErdStyle.RowSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var rowSub = new Font(ErdStyle.FontFamily, ErdStyle.RowSubSize, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var keyFont = new Font(ErdStyle.FontFamily, 9f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var titleBrush = new SolidBrush(TitleText))
            using (var mutedBrush = new SolidBrush(MutedText))
            using (var refBrush = new SolidBrush(RefText))
            using (var keyBrush = new SolidBrush(stroke))
            using (var format = new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
            {
                g.FillRectangle(white, rect);
                var headerHeight = (float)Math.Min(box.HeaderHeight, box.Height);
                g.FillRectangle(headerBrush, rect.X, rect.Y, rect.Width, headerHeight);

                var pad = (float)ErdStyle.TextPadding;
                if (box.SubTitle == null)
                {
                    g.DrawString(box.Title, titleFont, titleBrush, new RectangleF(rect.X + pad, rect.Y, rect.Width - 2 * pad, headerHeight), format);
                }
                else
                {
                    g.DrawString(box.Title, titleFont, titleBrush, new RectangleF(rect.X + pad, rect.Y + 3, rect.Width - 2 * pad, 18), format);
                    g.DrawString(box.SubTitle, subFont, mutedBrush, new RectangleF(rect.X + pad, rect.Y + 20, rect.Width - 2 * pad, 14), format);
                }

                if (box.Rows.Count > 0)
                {
                    g.DrawLine(pen, rect.X, rect.Y + headerHeight, rect.Right, rect.Y + headerHeight);
                }

                foreach (var row in box.Rows)
                {
                    var top = rect.Y + (float)row.Top;
                    var h = (float)row.Height;
                    var x = rect.X + pad;
                    if (!box.IsArea)
                    {
                        if (row.Key != null)
                        {
                            g.DrawString(row.Key, keyFont, keyBrush, new RectangleF(x - 2, top, (float)ErdStyle.KeyColumnWidth, h), format);
                        }

                        x = rect.X + (float)ErdStyle.KeyColumnWidth;
                    }

                    var font = row.Bold ? rowBold : rowFont;
                    var right = rect.Right - pad;
                    if (row.Ref != null)
                    {
                        var refWidth = Math.Min(right - x, (float)ErdStyle.MeasureText(row.Ref, ErdStyle.RowSubSize));
                        g.DrawString(row.Ref, rowSub, refBrush, new RectangleF(right - refWidth, top, refWidth + 2, h), format);
                        right -= refWidth + 10;
                    }

                    var available = right - x;
                    var textWidth = Math.Min(available, (float)ErdStyle.MeasureText(row.Text, ErdStyle.RowSize, row.Bold));
                    g.DrawString(row.Text, font, titleBrush, new RectangleF(x, top, textWidth + 2, h), format);
                    if (row.SubText != null && available - textWidth > 20)
                    {
                        g.DrawString(row.SubText, rowSub, mutedBrush, new RectangleF(x + textWidth + 6, top, available - textWidth - 6, h), format);
                    }
                }

                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        static void DrawEdge(Graphics g, EdgeLayout edge)
        {
            if (edge.Points.Count < 2) return;
            var points = edge.Points.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();
            using (var pen = new Pen(LineColor, 1.2f))
            {
                g.DrawLines(pen, points);
                DrawMark(g, pen, points[0], points[1], edge.StartMark);
                DrawMark(g, pen, points[points.Length - 1], points[points.Length - 2], edge.EndMark);
            }

            if (!string.IsNullOrEmpty(edge.Label))
            {
                var (a, b) = edge.Straight
                    ? (points[(points.Length - 1) / 2], points[(points.Length - 1) / 2 + 1])
                    : Enumerable.Range(0, points.Length - 1)
                        .Select(i => (points[i], points[i + 1]))
                        .OrderByDescending(s => Math.Abs(s.Item1.X - s.Item2.X) + Math.Abs(s.Item1.Y - s.Item2.Y))
                        .First();
                var mid = new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                using (var font = new Font(ErdStyle.FontFamily, 10f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(TitleText))
                using (var back = new SolidBrush(Color.White))
                {
                    var size = g.MeasureString(edge.Label, font);
                    var r = new RectangleF(mid.X - size.Width / 2 - 2, mid.Y - size.Height / 2, size.Width + 4, size.Height);
                    g.FillRectangle(back, r);
                    g.DrawString(edge.Label, font, brush, r.X + 2, r.Y);
                }
            }
        }

        /// <summary>Draws a crow's-foot mark at <paramref name="end"/>, where the line meets the box.</summary>
        static void DrawMark(Graphics g, Pen pen, PointF end, PointF previous, EndMark mark)
        {
            if (mark == EndMark.None) return;
            var dx = end.X - previous.X;
            var dy = end.Y - previous.Y;
            var len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            dx /= len;
            dy /= len;
            var nx = -dy;
            var ny = dx;

            PointF At(float back, float side) => new PointF(end.X - dx * back + nx * side, end.Y - dy * back + ny * side);

            void Bar(float back) => g.DrawLine(pen, At(back, -6), At(back, 6));

            void Circle(float back)
            {
                var c = At(back, 0);
                using (var white = new SolidBrush(Color.White))
                {
                    g.FillEllipse(white, c.X - 4, c.Y - 4, 8, 8);
                }

                g.DrawEllipse(pen, c.X - 4, c.Y - 4, 8, 8);
            }

            switch (mark)
            {
                case EndMark.One:
                    Bar(6);
                    Bar(10);
                    break;
                case EndMark.ZeroOrOne:
                    Bar(6);
                    Circle(14);
                    break;
                case EndMark.ZeroOrMany:
                    var foot = At(12, 0);
                    g.DrawLine(pen, foot, At(0, -6));
                    g.DrawLine(pen, foot, At(0, 6));
                    Circle(17);
                    break;
            }
        }
    }
}
