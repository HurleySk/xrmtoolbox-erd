using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using SolutionErd.Layout;
using SolutionErd.Render;

namespace SolutionErdXrmToolBoxPlugin
{
    /// <summary>Scrollable, zoomable preview of one ERD page. Ctrl+wheel zooms, dragging pans.</summary>
    public sealed class ErdCanvas : ScrollableControl
    {
        const int PageMargin = 24;
        const float MinZoom = 0.1f;
        const float MaxZoom = 4f;

        PageLayout page;
        float zoom = 1f;
        bool fit = true;
        Point? dragStart;
        Point dragScroll;

        public ErdCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            AutoScroll = true;
            BackColor = Color.FromArgb(0xEE, 0xF0, 0xF3);
        }

        public event EventHandler ZoomChanged;

        /// <summary>Shown when there is no page.</summary>
        public string EmptyText { get; set; } = "";

        public PageLayout Page
        {
            get => page;
            set
            {
                page = value;
                if (fit) ApplyFit();
                else UpdateScrollSize();
                AutoScrollPosition = Point.Empty;
                Invalidate();
            }
        }

        public float Zoom => zoom;

        /// <summary>Zoom so the whole page fits, and keep fitting as the page or window changes.</summary>
        public bool FitToWindow
        {
            get => fit;
            set
            {
                fit = value;
                if (fit) ApplyFit();
            }
        }

        public void SetZoom(float value) => SetZoom(value, new Point(ClientSize.Width / 2, ClientSize.Height / 2));

        public void ZoomIn() => SetZoom(zoom * 1.25f);

        public void ZoomOut() => SetZoom(zoom / 1.25f);

        void SetZoom(float value, Point anchor)
        {
            fit = false;
            if (page == null) return;
            value = Math.Max(MinZoom, Math.Min(MaxZoom, value));

            // Keep the page point under the anchor where it is.
            var origin = Origin();
            var pageX = (anchor.X - origin.X) / zoom;
            var pageY = (anchor.Y - origin.Y) / zoom;
            zoom = value;
            UpdateScrollSize();
            var centred = Origin();
            AutoScrollPosition = new Point(
                (int)(pageX * zoom + centred.X - AutoScrollPosition.X - anchor.X),
                (int)(pageY * zoom + centred.Y - AutoScrollPosition.Y - anchor.Y));
            Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        void ApplyFit()
        {
            if (page != null && page.Width > 0 && page.Height > 0)
            {
                var w = Math.Max(1, ClientSize.Width - 2 * PageMargin);
                var h = Math.Max(1, ClientSize.Height - 2 * PageMargin);
                zoom = Math.Max(MinZoom, Math.Min(1f, Math.Min(w / (float)page.Width, h / (float)page.Height)));
            }

            UpdateScrollSize();
            Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        void UpdateScrollSize()
        {
            AutoScrollMinSize = page == null
                ? Size.Empty
                : new Size((int)Math.Ceiling(page.Width * zoom) + 2 * PageMargin, (int)Math.Ceiling(page.Height * zoom) + 2 * PageMargin);
        }

        /// <summary>Where the page's top-left corner is drawn, in client coordinates. Small pages are centred.</summary>
        PointF Origin()
        {
            var width = (float)(page.Width * zoom);
            var height = (float)(page.Height * zoom);
            return new PointF(
                AutoScrollPosition.X + Math.Max(PageMargin, (ClientSize.Width - width) / 2),
                AutoScrollPosition.Y + Math.Max(PageMargin, (ClientSize.Height - height) / 2));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (fit) ApplyFit();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (page == null)
            {
                using (var brush = new SolidBrush(Color.FromArgb(0x6B, 0x77, 0x85)))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(EmptyText, Font, brush, ClientRectangle, format);
                }

                return;
            }

            var origin = Origin();
            var rect = new RectangleF(origin.X, origin.Y, (float)(page.Width * zoom), (float)(page.Height * zoom));
            using (var shadow = new SolidBrush(Color.FromArgb(0xD5, 0xD9, 0xDF)))
            {
                g.FillRectangle(shadow, rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
            }

            g.FillRectangle(Brushes.White, rect);

            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.TranslateTransform(origin.X, origin.Y);
            g.ScaleTransform(zoom, zoom);
            PreviewRenderer.Draw(g, page);
            g.Restore(state);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                SetZoom(e.Delta > 0 ? zoom * 1.15f : zoom / 1.15f, e.Location);
                return;
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left || page == null) return;
            dragStart = e.Location;
            dragScroll = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y);
            Cursor = Cursors.SizeAll;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragStart == null) return;
            AutoScrollPosition = new Point(dragScroll.X - (e.X - dragStart.Value.X), dragScroll.Y - (e.Y - dragStart.Value.Y));
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragStart = null;
            Cursor = Cursors.Default;
        }
    }
}
