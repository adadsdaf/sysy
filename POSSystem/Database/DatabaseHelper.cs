using Microsoft.Data.SqlClient;
using System.Data;

namespace POSSystem.Database
{
    public static class DatabaseHelper
    {
        private static string _connectionString = "";
        private const string DatabaseName = "POSSystem";

        public static void Initialize(string serverConnectionString)
        {
            _connectionString = BuildDbConnectionString(serverConnectionString);
        }

        /// <summary>
        /// ينشئ قاعدة البيانات إذا لم تكن موجودة — يتصل بـ master أولاً
        /// </summary>
        public static void EnsureDatabaseExists(string serverConnectionString)
        {
            var masterConn = BuildMasterConnectionString(serverConnectionString);
            using var conn = new SqlConnection(masterConn);
            conn.Open();
            using var cmd = new SqlCommand($@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{DatabaseName}')
                BEGIN
                    CREATE DATABASE [{DatabaseName}]
                END", conn);
            cmd.ExecuteNonQuery();
        }

        public static void TestConnection()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
        }

        public static SqlConnection GetConnection() => new SqlConnection(_connectionString);

        public static void ExecuteNonQuery(string sql, Dictionary<string, object?>? parameters = null)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            AddParams(cmd, parameters);
            cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, Dictionary<string, object?>? parameters = null)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            AddParams(cmd, parameters);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        public static DataTable ExecuteQuery(string sql, Dictionary<string, object?>? parameters = null)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            AddParams(cmd, parameters);
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public static T? GetValue<T>(DataRow row, string column)
        {
            if (row[column] == DBNull.Value) return default;
            return (T)Convert.ChangeType(row[column], typeof(T));
        }

        public static T GetValueOrDefault<T>(DataRow row, string column, T defaultValue)
        {
            if (row[column] == DBNull.Value) return defaultValue;
            try { return (T)Convert.ChangeType(row[column], typeof(T)); }
            catch { return defaultValue; }
        }

        // ─── Private Helpers ──────────────────────────────────────────────
        private static void AddParams(SqlCommand cmd, Dictionary<string, object?>? parameters)
        {
            if (parameters == null) return;
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        }

        private static string BuildMasterConnectionString(string serverConn)
        {
            var b = new SqlConnectionStringBuilder(Normalize(serverConn));
            b.InitialCatalog = "master";
            return b.ConnectionString;
        }

        private static string BuildDbConnectionString(string serverConn)
        {
            var b = new SqlConnectionStringBuilder(Normalize(serverConn));
            b.InitialCatalog = DatabaseName;
            return b.ConnectionString;
        }

        private static string Normalize(string input)
        {
            input = input.Trim();
            if (!input.Contains('='))
                return $"Server={input};Trusted_Connection=True;TrustServerCertificate=True;";
            return input;
        }
    }
}
