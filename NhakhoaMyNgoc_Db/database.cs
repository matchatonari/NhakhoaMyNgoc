﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public enum QueryOperator
    {
        EQUALS,
        LESS_THAN,
        LESS_THAN_OR_EQUAL,
        LIKE,
        BETWEEN,
        COLLATE
    }

    public class Database
    {
        private static readonly string databaseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NhakhoaMyNgoc", "database.db");
        static SQLiteConnection connection;

        public static void Initialize()
        {
            string folderPath = Path.GetDirectoryName(databaseFile);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string schema = string.Empty;
            if (!File.Exists(databaseFile))
            {
                SQLiteConnection.CreateFile(databaseFile);

                connection = new SQLiteConnection($"Data Source={databaseFile};Version=3;");
                connection.Open();

                schema = File.ReadAllText(Path.Combine(Application.StartupPath, $"{version.Major}.{version.Minor}.{version.Build}.sql"));
            }
            else
            {
                connection = new SQLiteConnection($"Data Source={databaseFile};Version=3;");
                connection.Open();

                using (var command = new SQLiteCommand("PRAGMA user_version;", connection))
                {
                    int dataVersion = Convert.ToInt32(command.ExecuteScalar());
                    schema = File.ReadAllText(Path.Combine(Application.StartupPath, $"DB{dataVersion}.sql"));
                }
            }

            using (var cmd = new SQLiteCommand(schema, connection))
                cmd.ExecuteNonQuery();
        }

        public static void Close()
        {
            if (connection != null && connection.State == ConnectionState.Open)
                connection.Close();
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

            string sql = $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}) RETURNING {tableName}_Id;";
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

            string sql = $"UPDATE {tableName} SET {editExpr} WHERE {tableName}_Id = @PrimaryKey RETURNING *";
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
        ///     QueryOperator là toán tử sử dụng theo enum.</param>
        /// <returns>DataTable chứa kết quả truy vấn.</returns>
        public static DataTable Query(string tableName, List<string> queryProperties = null, List<string> sumColumns = null, Dictionary<string, (QueryOperator, object)> conditions = null)
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

                    string opResult = string.Empty;
                    string extraCommand = string.Empty;

                    switch (op)
                    {
                        case QueryOperator.EQUALS:
                            opResult = "="; break;
                        case QueryOperator.LESS_THAN:
                            opResult = "<"; break;
                        case QueryOperator.LESS_THAN_OR_EQUAL:
                            opResult = "<="; break;
                        case QueryOperator.COLLATE:
                            opResult = "=";
                            extraCommand = "COLLATE NOCASE";
                            break;
                        case QueryOperator.LIKE:
                            opResult = "LIKE"; break;
                        case QueryOperator.BETWEEN:
                            opResult = "BETWEEN"; break;
                    }

                    if (value is IEnumerable enumerable && !(value is string))
                    {
                        // OR condition (dùng nhiều tham số)
                        var orTokens = new List<string>();

                        foreach (object item in enumerable)
                        {
                            string paramName = $"@param{paramIndex++}";
                            orTokens.Add($"{key} {extraCommand} {opResult} {paramName}");
                            parameters.Add(new SQLiteParameter(paramName, (op == QueryOperator.LIKE) ? $"%{item}%" : item));
                        }

                        tokens.Add($"({string.Join(" OR ", orTokens)})");
                    }
                    else if (value is ValueTuple<DateTime, DateTime> tuple)
                    {
                        tokens.Add($"{key} BETWEEN @Item1 AND @Item2");
                        parameters.Add(new SQLiteParameter("@Item1", tuple.Item1));
                        parameters.Add(new SQLiteParameter("@Item2", tuple.Item2));
                    }
                    else
                    {
                        // Single value condition
                        string paramName = $"@param{paramIndex++}";
                        tokens.Add($"{key} {extraCommand} {opResult} {paramName}");
                        parameters.Add(new SQLiteParameter(paramName, (op == QueryOperator.LIKE) ? $"%{value}%" : value));
                    }
                }
            }

            string expression = string.Join(" AND ", tokens);
            string sql = "SELECT ";
            if (queryProperties != null)
            {
                foreach (string property in queryProperties)
                {
                    if (sql != "SELECT ")
                        sql += ", ";

                    if (sumColumns != null)
                    {
                        if (sumColumns.Contains(property))
                            sql += $"SUM({property})";
                    }
                    else
                        sql += property;
                }
            }
            else
                sql += '*';

            sql += $" FROM {tableName}";

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
            DataTable customers = Query("Customer", conditions: new Dictionary<string, (QueryOperator, object)>
            {
                { "Customer_FullName", (QueryOperator.LIKE, customer.Customer_FullName) },
                { "Customer_CitizenId", (QueryOperator.EQUALS, customer.Customer_CitizenId) },
                { "Customer_Address", (QueryOperator.LIKE, customer.Customer_Address) },
                { "Customer_Phone", (QueryOperator.EQUALS, customer.Customer_Phone) }
            });

            if (customers.Rows.Count == 0)
                return new DataTable();

            DataRow bestResult = customers.Rows[0];
            return Query("Receipt", conditions: new Dictionary<string, (QueryOperator, object)> { { "Receipt_CustomerId", (QueryOperator.EQUALS, bestResult["Customer_Id"]) } });
        }

        public static DataTable GetCustomerHistory(Customer customer)
        {
            string sql = "SELECT r.Receipt_Date, d.ReceiptDetail_Content, r.Receipt_Total, r.Receipt_Remaining " +
                "FROM Receipt r " +
                "JOIN ReceiptDetail d ON d.ReceiptDetail_ReceiptId = r.Receipt_Id " +
                "WHERE Receipt_CustomerId = @Customer_Id;";
            DataTable result = new DataTable();
            using (var cmd = new SQLiteCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@Customer_Id", customer.Customer_Id);
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    adapter.Fill(result);
            }
            return result;
        }
    }
}
