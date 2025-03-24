using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NhakhoaMyNgoc_Db.Templates
{
    public class StockReport : PrintablePaper
    {
        DateTime from, to;
        DataTable stats;
        int sum;
        public StockReport(DateTime from, DateTime to, DataTable stats, int sum)
        {
            this.from = from;
            this.to = to;
            this.stats = stats;
            this.sum = sum;
        }
        public override object GetFileName()
        {
            return from.ToString("yyyy-MM-dd") + "_" + to.ToString("yyyy-MM-dd");
        }
        public override string GetTemplateName()
        {
            return "StockReport";
        }
        public override void Edit(ref string templateSrc)
        {
            string stockList = string.Empty;
            foreach (DataRow row in stats.Rows) {
                stockList += "<tr>";
                foreach (DataColumn col in stats.Columns)
                    stockList += "<td>" + row[col] + "</td>";
                stockList += "</tr>";
            }

            templateSrc = templateSrc.Replace("{{fromDate}}", from.ToString("dd/MM/yyyy"))
                                     .Replace("{{toDate}}", to.ToString("dd/MM/yyyy"))
                                     .Replace("{{StockReceiptDetail}}", stockList);
            templateSrc = templateSrc.Replace("{{sum}}", sum.ToString());
        }
    }
}
