using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace tlg2023
{
    public class TransparentLabel : Label
    {
        public Color OutlineColor { get; set; } = Color.White;
        public float OutlineWidth { get; set; } = 2;

        protected override void OnPaint(PaintEventArgs e)
        {
            // e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            // e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), 0, 0);
            e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
            //e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            //e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath gp = new GraphicsPath())
            using (Pen outline = new Pen(OutlineColor, OutlineWidth) { LineJoin = LineJoin.Round })
            using (StringFormat sf = new StringFormat())
            using (Brush foreBrush = new SolidBrush(ForeColor))
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                gp.AddString(Text, Font.FontFamily, (int)Font.Style, Font.Size, ClientRectangle, sf);

                e.Graphics.DrawPath(outline, gp);
                e.Graphics.FillPath(foreBrush, gp);
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            if (this.Parent != null)
            {
                Parent.Invalidate(this.Bounds, true);
            }
            base.OnBackColorChanged(e);
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            this.Invalidate();
            base.OnParentBackColorChanged(e);
        }
    }
}
