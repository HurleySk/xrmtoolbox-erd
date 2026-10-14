using System;
using System.Drawing;

namespace SolutionErd.Layout
{
    /// <summary>Sizes and fonts shared by layout, preview and writers.</summary>
    public static class ErdStyle
    {
        public const string FontFamily = "Arial";
        public const float TitleSize = 12f;
        public const float SubTitleSize = 10f;
        public const float RowSize = 11f;
        public const float RowSubSize = 10f;

        public const double HeaderHeight = 38;
        public const double AreaHeaderHeight = 28;
        public const double RowHeight = 20;
        public const double KeyColumnWidth = 26;
        public const double TextPadding = 8;
        public const double MinBoxWidth = 140;
        public const double MaxBoxWidth = 340;

        public const double PageMargin = 30;
        public const double NodeSeparation = 40;
        public const double LayerSeparation = 110;
        public const double RoutingPadding = 10;
        public const double SelfLoopReach = 18;

        static readonly object Gate = new object();
        static Graphics graphics;

        /// <summary>Width in pixels of text drawn at the given pixel size.</summary>
        public static double MeasureText(string text, float pixelSize, bool bold = false)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            lock (Gate)
            {
                graphics = graphics ?? Graphics.FromImage(new Bitmap(1, 1));
                using (var font = new Font(FontFamily, pixelSize, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var size = graphics.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic);
                    return Math.Ceiling(size.Width) + 1;
                }
            }
        }
    }
}
