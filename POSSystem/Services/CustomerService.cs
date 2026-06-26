using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class CustomerService
    {
        public static List<Customer> GetAll(string? search = null)
        {
            var sql = "SELECT * FROM Customers WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(search)) { sql += " AND (Name LIKE @S OR Phone LIKE @S)"; p["@S"] = $"%{search}%"; }
            sql += " ORDER BY Name";
            var dt = DatabaseHelper.ExecuteQuery(sql, p);
            return dt.Rows.Cast<DataRow>().Select(MapCustomer).ToList();
        }

        public static int Create(Customer c)
        {
            var id = DatabaseHelper.ExecuteScalar("INSERT INTO Customers(Name,Phone,Email,Notes) VALUES(@N,@Ph,@Em,@No);SELECT SCOPE_IDENTITY();",
                new() { ["@N"] = c.Name, ["@Ph"] = c.Phone, ["@Em"] = c.Email, ["@No"] = c.Notes });
            return Convert.ToInt32(id);
        }

        public static void Update(Customer c)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE Customers SET Name=@N,Phone=@Ph,Email=@Em,Notes=@No WHERE Id=@Id",
                new() { ["@N"] = c.Name, ["@Ph"] = c.Phone, ["@Em"] = c.Email, ["@No"] = c.Notes, ["@Id"] = c.Id });
        }

        public static void Delete(int id) => DatabaseHelper.ExecuteNonQuery("DELETE FROM Customers WHERE Id=@Id", new() { ["@Id"] = id });

        private static Customer MapCustomer(DataRow r) => new Customer
        {
            Id = (int)r["Id"], Name = (string)r["Name"],
            Phone = r["Phone"] == DBNull.Value ? null : (string)r["Phone"],
            Email = r["Email"] == DBNull.Value ? null : (string)r["Email"],
            Notes = r["Notes"] == DBNull.Value ? null : (string)r["Notes"],
            TotalPurchases = r["TotalPurchases"] == DBNull.Value ? 0 : (decimal)r["TotalPurchases"],
            CreatedAt = (DateTime)r["CreatedAt"]
        };
    }
}
