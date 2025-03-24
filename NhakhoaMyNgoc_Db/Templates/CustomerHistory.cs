using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NhakhoaMyNgoc_Db.Templates
{
    class CustomerHistory : PrintablePaper
    {
        Customer customer;
        DataTable queryResult;
        public CustomerHistory(Customer customer, DataTable queryResult)
        {
            this.customer = customer;
            this.queryResult = queryResult;
        }
        public override object GetFileName() {
            return customer.Customer_Id;
        }
        public override string GetTemplateName()
        {
            return "CustomerHistory";
        }
        public override void Edit(ref string templateSrc)
        {
            string serviceList = string.Empty;
            foreach (DataRow row in queryResult.Rows)
            {
                serviceList += "<tr><td></td>";
                serviceList += "<td>" + row["Receipt_Date"].ToString() + "</td>\n";
                serviceList += "<td>" + row["ReceiptDetail_Content"].ToString() + "</td>\n";
                serviceList += "<td>" + row["Receipt_Total"].ToString() + "</td>\n";
                serviceList += "<td>" + row["Receipt_Remaining"].ToString() + "</td>\n";
                serviceList += "</tr>";
            }

            var customerProperties = typeof(Customer).GetProperties();
            foreach (var prop in customerProperties)
            {
                if (prop.Name == "Customer_Sex") continue;
                if (prop.Name == "Customer_Birthdate")
                    templateSrc = templateSrc.Replace($"{{{{{prop.Name}}}}}", customer.Customer_Birthdate.ToString("dd/MM/yyyy"));

                templateSrc = templateSrc.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(customer).ToString());
            }

            templateSrc = templateSrc
                .Replace("{{!Customer_Sex}}", (customer.Customer_Sex ? "☐" : "☑"))
                .Replace("{{Customer_Sex}}", (customer.Customer_Sex ? "☑" : "☐"))
                .Replace("{{QueryResult}}", serviceList);
        }
    }
}
