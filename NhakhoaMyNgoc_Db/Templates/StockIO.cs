using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public class StockIO : PrintablePaper
    {
        StockReceipt receipt;
        DataTable details;
        bool isInput;
        public StockIO(StockReceipt receipt, DataTable details)
        {
            this.receipt = receipt;
            this.details = details;
        }

        public override string GetResultPath()
        {
            return Path.Combine(Path.GetTempPath(), receipt.StockReceipt_Id + ".html");
        }
        public override void Render()
        {
            string serviceList = string.Empty;
            foreach (DataRow row in details.Rows)
            {
                serviceList += "<tr>\n<td></td>\n";
                foreach (DataColumn column in row.Table.Columns)
                {
                    if (column.ColumnName == "StockReceiptDetail_Id" ||
                        column.ColumnName == "StockReceiptDetail_ReceiptID" ||
                        column.ColumnName == "StockReceiptDetail_ItemName")
                        continue;

                    if (column.ColumnName == "StockReceiptDetail_ItemId")
                        serviceList += $"<td>{row["StockReceiptDetail_ItemName"].ToString()}</td>\n<td>{row[column].ToString()}</td>\n";
                    else
                        serviceList += "<td>" + row[column].ToString() + "</td>\n";
                }
                serviceList += "</tr>";
            }

            string receiptPath = GetResultPath();

            File.Copy(Path.Combine(PrintablePaper.RESOURCE_PATH, (receipt.StockReceipt_IsInput ? "stock_input.html" : "stock_output.html")), receiptPath, true);
            File.Copy(Path.Combine(PrintablePaper.RESOURCE_PATH, "logo.png"), Path.Combine(Path.GetTempPath(), "logo.png"), true);
            File.Copy(Path.Combine(PrintablePaper.RESOURCE_PATH, "docTien.js"), Path.Combine(Path.GetTempPath(), "docTien.js"), true);

            DataTable stockList = Database.Query("StockList", conditions: new Dictionary<string, (QueryOperator, object)>
                { { "StockList_Id", (QueryOperator.EQUALS, receipt.StockReceipt_StockId) } });
            DataRow stock = stockList.Rows[0];
            string stockAlias = stock["StockList_Alias"].ToString();
            string stockAddress = stock["StockList_Address"].ToString();

            string receiptContent = File.ReadAllText(receiptPath);
            var receiptProperties = typeof(StockReceipt).GetProperties();
            foreach (var prop in receiptProperties)
                receiptContent = receiptContent.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(receipt).ToString());

            receiptContent = receiptContent
                .Replace("{{StockList_Alias}}", stockAlias)
                .Replace("{{StockList_Address}}", stockAddress)
                .Replace("{{StockReceiptDetail}}", serviceList);

            File.WriteAllText(receiptPath, receiptContent);
        }
    }
}
