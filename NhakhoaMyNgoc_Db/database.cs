using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public class Database
    {
        static string databaseFile = Path.Combine(Application.StartupPath, "res", "database.db");
        static SQLiteConnection connection;

        public static void Initialize() {
            connection = new SQLiteConnection($"Data Source={databaseFile};Version=3;");
            connection.Open();
        }

        /// <summary>
        /// Lấy số hàng của bảng.
        /// </summary>
        /// <param name="tableName">Tên bảng</param>
        /// <returns></returns>
        public static int GetNextId(string tableName)
        {
            DataTable result = Query("sqlite_sequence", new List<string> { "seq" },
                                     new Dictionary<string, (bool, object)> { { "name", (false, tableName) } });

            return result.Rows.Count > 0 ? Convert.ToInt32(result.Rows[0]["seq"]) + 1 : 1;
        }

        /// <summary>
        /// Thêm hàng vào bảng.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu khách hàng, đơn hàng...</typeparam>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="record">Bản ghi</param>
        public static int AddRecord<T>(string tableName, T record)
        {
            string columnList = string.Join(", ",
                typeof(T).GetProperties()
                .Where(p => !p.Name.Contains("_Id"))  // Lọc bỏ cột chứa "_Id"
                .Select(p => p.Name));

            string parameterList = string.Join(", ",
                typeof(T).GetProperties().
                Where(p => !p.Name.Contains("_Id")).
                Select(p => $"@{p.Name}"));

            string sql = $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}) RETURNING {tableName + "_Id"};";

            using (var command = new SQLiteCommand(sql, connection))
            {
                foreach (var property in typeof(T).GetProperties())
                {
                    string parameterName = $"@{property.Name}";

                    var value = property.GetValue(record) ?? DBNull.Value;

                    if (value is DateTime dateTimeValue)
                    {
                        if ((typeof(T) == typeof(Receipt) && parameterName != "RevisitDate") || (typeof(T) == typeof(StockReceipt)))
                            value = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                        else if (typeof(T) == typeof(Customer) || (typeof(T) == typeof(Receipt) && parameterName == "RevisitDate"))
                            value = dateTimeValue.ToString("yyyy-MM-dd");
                    }

                    if (value == null)
                        value = DBNull.Value;

                    command.Parameters.AddWithValue(parameterName, value);
                }
                
                object lastId = command.ExecuteScalar();
                return Convert.ToInt32(lastId);
            }
        }

        /// <summary>
        /// Cập nhật giá trị trong bảng.
        /// </summary>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="primaryValue">Giá trị chốt</param>
        /// <param name="edits">Map chứa key là cột cần sửa, value là giá trị muốn sửa</param>
        /// <returns>DataTable chứa các hàng đã sửa.</returns>
        public static DataTable UpdateRecord(string tableName, int primaryValue, Dictionary<string, object> edits)
        {
            DataTable rowsEdited = new DataTable();
            
            string editExpr = string.Join(", ", edits.Keys.Select(k => $"{k} = @{k}"));

            string sql = $"UPDATE {tableName} SET {editExpr} WHERE {tableName + "_Id"} = @PrimaryKey RETURNING *";
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@PrimaryKey", primaryValue);

                foreach (var kv in edits)
                    command.Parameters.AddWithValue($"@{kv.Key}", kv.Value ?? DBNull.Value);

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    rowsEdited.Merge(dt);
                }
            }
            return rowsEdited;
        }

        /// <summary>
        /// Xoá bản ghi
        /// </summary>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="primaryKeyColumn">Cột chốt</param>
        /// <param name="primaryKeyValue">Giá trị chốt</param>
        /// <returns></returns>
        public static void DeleteRecord(string tableName, List<int> primaryKeys)
        {
            if (primaryKeys == null || primaryKeys.Count == 0) return; // Không có gì để xóa

            using (var transaction = connection.BeginTransaction())
            {
                // Tạo danh sách tham số (@p0, @p1, ...)
                string paramList = string.Join(", ", primaryKeys.Select((_, i) => $"@p{i}"));
                string sql = $"DELETE FROM [{tableName}] WHERE [{tableName}_Id] IN ({paramList})";

                using (var cmd = new SQLiteCommand(sql, connection, transaction))
                {
                    for (int i = 0; i < primaryKeys.Count; i++)
                        cmd.Parameters.AddWithValue($"@p{i}", primaryKeys[i]);

                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Truy vấn bảng
        /// </summary>
        /// <param name="tableName">Khách hàng</param>
        /// <param name="conditions">Map chứa key là tên cột, value là (bool, object) với object là giá trị tìm,
        ///     bool = false nếu dùng toán tử '=' để so sánh, = true nếu dùng toán tử LIKE.</param>
        /// <returns>DataTable chứa kết quả truy vấn.</returns>
        public static DataTable Query(string tableName, List<string> queryProperties = null, Dictionary<string, (bool, object)> conditions = null)
        {
            DataTable result = new DataTable();
            var tokens = new List<string>();
            var parameters = new List<SQLiteParameter>();

            int paramIndex = 0;

            if (conditions != null)
            {
                foreach (var kvp in conditions)
                {
                    var key = kvp.Key;
                    var op = kvp.Value.Item1;
                    var value = kvp.Value.Item2;

                    if (value == null || value.ToString() == string.Empty) continue;

                    if (value is IEnumerable enumerable && !(value is string))
                    {
                        // OR condition (dùng nhiều tham số)
                        var orTokens = new List<string>();

                        foreach (object item in enumerable)
                        {
                            string paramName = $"@param{paramIndex++}";
                            orTokens.Add($"{key} {(op ? "LIKE" : "=")} {paramName}");
                            parameters.Add(new SQLiteParameter(paramName, op ? $"%{item}%" : item));
                        }

                        tokens.Add($"({string.Join(" OR ", orTokens)})");
                    }
                    else
                    {
                        // Single value condition
                        string paramName = $"@param{paramIndex++}";
                        tokens.Add($"{key} {(op ? "LIKE" : "=")} {paramName}");
                        parameters.Add(new SQLiteParameter(paramName, op ? $"%{value}%" : value));
                    }
                }
            }

            string expression = string.Join(" AND ", tokens);
            string sql = $"SELECT * FROM {tableName}";

            if (expression != string.Empty)
                sql += " WHERE " + expression;

            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters.ToArray());

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    adapter.Fill(result);
            }

            return result;
        }

        /// <summary>
        /// Lấy lịch sử của khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng (theo database)</param>
        /// <returns>Bảng các đơn hàng của khách hàng.</returns>
        public static DataTable GetReceipts(Customer customer)
        {
            DataTable customers = Query("Customer", null, new Dictionary<string, (bool, object)>
            {
                { "Customer_FullName", (true, customer.Customer_FullName) },
                { "Customer_CitizenId", (false, customer.Customer_CitizenId) },
                { "Customer_Address", (true, customer.Customer_Address) },
                { "Customer_Phone", (false, customer.Customer_Phone) }
            });

            DataRow bestResult = customers.Rows[0];
            return Query("Receipt", null, new Dictionary<string, (bool, object)> { { "Receipt_CustomerId", (false, bestResult["Customer_Id"]) } });
        }
        /// <summary>
        /// Lấy dữ liệu kho hàng.
        /// </summary>
        /// <param name="showHidden">Có hiện những vật phẩm đã xoá không?</param>
        /// <returns>Bảng kho.</returns>
        public static DataTable GetStock(bool showHidden = false)
        {
            return Database.Query("Stock", null, 
                new Dictionary<string, (bool, object)> { { "Stock_IsActive", (true, 1) } });
        }

        /// <summary>
        /// Tìm đơn nhập theo thời gian
        /// </summary>
        /// <param name="from">Mốc thời gian đầu tiên</param>
        /// <param name="to">Mốc thời gian thứ hai</param>
        /// <returns>Bảng đơn nhập theo thời gian.</returns>
        public static DataTable GetInputReceiptsBetween(DateTime from, DateTime to)
        {
            string sql = @"
                SELECT StockReceiptDetail.* 
                FROM StockReceiptDetail 
                JOIN StockReceipt 
                ON StockReceiptDetail.StockReceiptDetail_ReceiptID = StockReceipt.StockReceipt_Id 
                WHERE DATE(StockReceipt.StockReceipt_Date) BETWEEN @StartDate AND @EndDate";

            DataTable dataTable = new DataTable();

            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StartDate", from.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", to.ToString("yyyy-MM-dd"));

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                adapter.Fill(dataTable);
            }

            // Chế biến datatable
            dataTable.Columns.Add("_StockReceiptDetail_Input", typeof(bool)).SetOrdinal(1);
            dataTable.Columns.Add("_StockReceiptDetail_Date", typeof(DateTime)).SetOrdinal(1);
            foreach (DataRow row in dataTable.Rows)
            {
                var quantity = Convert.ToInt32(row["StockReceiptDetail_Quantity"]);
                var total = Convert.ToInt32(row["StockReceiptDetail_Total"]);

                row["_StockReceiptDetail_Date"] = Query(
                    "StockReceipt",
                    new List<string> { "StockReceipt_Date" },
                    new Dictionary<string, (bool, object)> {
                        { "StockReceipt_Id", (false, row["StockReceiptDetail_ReceiptID"]) }
                    }
                ).Rows[0]["StockReceipt_Date"];

                row["_StockReceiptDetail_Input"] = (quantity > 0);
                row["StockReceiptDetail_Quantity"] = Math.Abs(quantity);
                row["StockReceiptDetail_Total"] = Math.Abs(total);
            }
            return dataTable;
        }
    }
}
