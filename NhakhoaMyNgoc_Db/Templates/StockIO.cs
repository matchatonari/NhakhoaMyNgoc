using System.Collections.Generic;
using System.Data;

namespace NhakhoaMyNgoc_Db
{
    public class StockIO : PrintablePaper
    {
        StockReceipt receipt;
        DataTable details;
        public StockIO(StockReceipt receipt, DataTable details)
        {
            this.receipt = receipt;
            this.details = details;
        }

        public override string GetTemplateName()
        {
            return receipt.StockReceipt_IsInput ? "StockI" : "StockO";
        }

        public override object GetFileName()
        {
            return receipt.StockReceipt_Id;
        }
        public override void Edit(ref string templateSrc)
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

            DataTable stockList = Database.Query("StockList", conditions: new Dictionary<string, (QueryOperator, object)>
                { { "StockList_Id", (QueryOperator.EQUALS, receipt.StockReceipt_StockId) } });
            DataRow stock = stockList.Rows[0];
            string stockAlias = stock["StockList_Alias"].ToString();
            string stockAddress = stock["StockList_Address"].ToString();

            var receiptProperties = typeof(StockReceipt).GetProperties();
            foreach (var prop in receiptProperties)
                templateSrc = templateSrc.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(receipt).ToString());

            templateSrc = templateSrc
                .Replace("{{StockList_Alias}}", stockAlias)
                .Replace("{{StockList_Address}}", stockAddress)
                .Replace("{{StockReceiptDetail}}", serviceList);
        }
    }
}
