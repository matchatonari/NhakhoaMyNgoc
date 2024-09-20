using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;

namespace NhakhoaMyNgoc_Db
{
    internal class PDFTemplates
    {
        static PdfDocument document;
        static PdfPage page;
        static XGraphics gfx;

        static void init()
        {
            document = new PdfDocument();
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A5;
            gfx = XGraphics.FromPdfPage(page);
        }

        static void export()
        {
            document.Save("temp.pdf");
        }

        public static void HoaDonDichVu(string name, bool female, string birthdate, List<string[]> orders)
        {
            init();

            pdfutil.image(gfx, @"tmp\logo.jpg", 25, 25, 100, 100);
            pdfutil.text(gfx, "NHA KHOA MỸ NGỌC", 16, XFontStyleEx.Bold, 125, 50);
            pdfutil.text(gfx, "268 Lý Thường Kiệt, P. 14, Q. 10, TP. HCM", 10, XFontStyleEx.Italic, 125, 70);
            pdfutil.text(gfx, "HOÁ ĐƠN DỊCH VỤ NHA KHOA", 18, XFontStyleEx.Bold, 75, 150);
            pdfutil.text(gfx, "Số hoá đơn:", 14, XFontStyleEx.Italic, 250, 170);
            pdfutil.text(gfx, "Họ và tên:", 14, XFontStyleEx.Regular, 25, 200);
            pdfutil.text(gfx, name, 14, XFontStyleEx.Bold, 85, 200);
            pdfutil.text(gfx, "Giới tính: ", 14, XFontStyleEx.Regular, 300, 200);
            pdfutil.text(gfx, (female ? "Nữ" : "Nam"), 14, XFontStyleEx.Bold, 360, 200);
            pdfutil.text(gfx, "Ngày sinh:", 14, XFontStyleEx.Regular, 25, 220);
            pdfutil.text(gfx, birthdate, 14, XFontStyleEx.Bold, 90, 220);

            int[] w = new int[] { 15, 200, 25, 60, 50, 60 };
            int[] h = new int[] { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 };
            List<string[]> stringTable = new List<string[]>();
            stringTable.Add(new string[] { "#", "Dịch vụ", "SL", "Đơn giá", "Giảm giá", "Thành tiền" });

            int total = 0;

            for (int i = 0; i < orders.Count; i++)
            {
                string[] order = new string[6];
                order[0] = (i + 1).ToString();
                for (int j = 0; j < 5; j++)
                    order[j + 1] = orders[i][j];
                stringTable.Add(order);

                total += Convert.ToInt32(order[2].Replace(",", "")) * Convert.ToInt32(order[3].Replace(",", "")) - Convert.ToInt32(order[4].Replace(",", ""));
            }

            pdfutil.table(gfx, w, h, 5, 240, stringTable);

            pdfutil.text(gfx, "Tổng cộng:", 14, XFontStyleEx.Regular, 25, 480);
            pdfutil.text(gfx, total.ToString("N0"), 14, XFontStyleEx.Bold, 100, 480);
            pdfutil.text(gfx, "Người lập hoá đơn", 14, XFontStyleEx.Regular, 250, 500);
            pdfutil.text(gfx, "(Ký tên hoặc đóng dấu)", 11, XFontStyleEx.Italic, 250, 520);

            export();
        }
    }
}
