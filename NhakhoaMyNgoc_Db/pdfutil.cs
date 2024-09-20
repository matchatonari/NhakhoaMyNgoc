using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.Generic;
using System.Linq;

namespace NhakhoaMyNgoc_Db
{
    internal class pdfutil
    {
        public static void image(XGraphics gfx, string path, int x, int y, int w, int h)
        {
            gfx.DrawImage(XImage.FromFile(path), x, y, w, h);
        }

        public static void text(XGraphics gfx, string text, int size, XFontStyleEx style, int x, int y)
        {
            gfx.DrawString(text, new XFont("Times New Roman", size, style), XBrushes.Black, new XPoint(x, y));
        }

        public static void table(XGraphics gfx, int[] w, int[] h, int x, int y, List<string[]> tableText)
        {
            gfx.DrawRectangle(new XPen(XColors.Black, 1), null, new XRect(x, y, w.Sum(), h.Sum()));
            for (int i = 0; i < w.Length; i++)
                gfx.DrawLine(new XPen(XColors.Black, 1), x + w.Take(i).Sum(), y, x + w.Take(i).Sum(), y + h.Sum());
            for (int i = 0; i < h.Length; i++)
                gfx.DrawLine(new XPen(XColors.Black, 1), x, y + h.Take(i).Sum(), x + w.Sum(), y + h.Take(i).Sum());
            for (int i = 0; i < tableText.Count; i++)
                for (int j = 0; j < tableText[i].Length; j++)
                    text(gfx, tableText[i][j], 12, XFontStyleEx.Regular, x + w.Take(j).Sum(), y + h.Take(i).Sum() + 15);
        }
    }
}
