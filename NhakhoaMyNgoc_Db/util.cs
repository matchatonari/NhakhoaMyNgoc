using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public class Customer
    {
        public string Customer_FullName { get; set; }
        public bool Customer_Sex { get; set; }
        public DateTime Customer_Birthdate { get; set; }
        public string Customer_CitizenId { get; set; }
        public string Customer_Address { get; set; }
        public string Customer_Phone { get; set; }
    };

    public class Receipt
    {
        public int Receipt_Id { get; set; }
        public int Receipt_CustomerId { get; set; }
        public DateTime Receipt_Date { get; set; }
        public int Receipt_Total { get; set; } = 0;
        public int Receipt_Remaining { get; set; } = 0;
        public DateTime Receipt_RevisitDate { get; set; }
        public string Receipt_Notes { get; set; }
    };

    public class ReceiptDetail
    {
        public int ReceiptDetail_Id { get; set; }
        public int ReceiptDetail_ReceiptId { get; set; }
        public string ReceiptDetail_Content { get; set; }
        public int ReceiptDetail_Quantity { get; set; }
        public int ReceiptDetail_Price { get; set; }
        public int ReceiptDetail_Discount { get; set; }
    };

    public class StockReceipt
    {
        public int StockReceipt_Id { get; set; }
        public DateTime StockReceipt_Date { get; set; }
        public int StockReceipt_Total { get; set; } = 0;
    };

    public class StockReceiptDetail
    {
        public int StockReceiptDetail_Id { get; set; }
        public int StockReceiptDetail_ReceiptId { get; set; }
        public int StockReceiptDetail_ItemId { get; set; }
        public int StockReceiptDetail_Quantity { get; set; }
        public int StockReceiptDetail_Price { get; set; }
    };

    public class Item
    {
        public int Stock_Id { get; set; }
        public string Stock_Name { get; set; }
        public int Stock_Quantity { get; set; } = 0;
        public int Stock_Total { get; set; } = 0;
    };

    public static class Util
    {
        public static void AttachReformatHook(DataGridView dgv)
        {
            dgv.CellFormatting += (sender, e) =>
            {
                string columnName = dgv.Columns[e.ColumnIndex].Name;
                if (e.Value != null && decimal.TryParse(e.Value.ToString(), out decimal value))
                {
                    e.Value = value.ToString("N0");
                    e.FormattingApplied = true;
                }
            };
        }

        public static void AttachUpdateHook(DataGridView dgv, string tableName)
        {
            dgv.CellValueChanged += (sender, e) => {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                var primaryKeyColumn = tableName + "_Id";

                var columnName = dgv.Columns[e.ColumnIndex].Name;
                var id = dgv.Rows[e.RowIndex].Cells[primaryKeyColumn].Value;
                var newValue = dgv.CurrentCell.Value;

                if (columnName.Contains("Total")) return;

                if (dgv.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
                    newValue = ((newValue != DBNull.Value && Convert.ToBoolean(newValue)) ? 1 : 0);

                // ghi đè dữ liệu vào db
                DataTable result = Database.UpdateRecord(tableName, Convert.ToInt32(id), new Dictionary<string, object>
                {
                    { columnName, newValue }
                });
                DataRow updatedRow = result.Rows[0];
                // vẽ lại UI
                foreach (DataColumn column in updatedRow.Table.Columns)
                    if (dgv.Columns.Contains(column.ColumnName))
                        dgv.Rows[e.RowIndex].Cells[column.ColumnName].Value = updatedRow[column];
            };
        }

        public static void DismissDirtyState(DataGridView dgv) {
            dgv.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (dgv.CurrentCell is DataGridViewComboBoxCell)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
        }

        public static T MapRowTo<T>(DataGridViewRow row) where T : new()
        {
            T obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                if (row.DataGridView.Columns.Contains(prop.Name) &&
                    row.Cells[prop.Name] != null)
                {
                    object value = row.Cells[prop.Name].Value;
                    prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            return obj;
        }
    }
}
