using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class UserService
    {
        public static User? Login(string pin)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Users WHERE Pin=@Pin AND IsActive=1", new() { ["@Pin"] = pin });
            return dt.Rows.Count > 0 ? MapUser(dt.Rows[0]) : null;
        }

        public static List<User> GetAll()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Users ORDER BY Name");
            return dt.Rows.Cast<DataRow>().Select(MapUser).ToList();
        }

        public static int Create(User u)
        {
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO Users(Name,Role,Pin,IsActive,CanAccessPOS,CanManageItems,CanViewReports,CanApplyDiscount,CanCancelInvoice,CanAccessManagement,CanManageCustomers)
VALUES(@N,@R,@P,@A,@POS,@MI,@VR,@AD,@CI,@AM,@MC);SELECT SCOPE_IDENTITY();",
                new()
                {
                    ["@N"] = u.Name, ["@R"] = u.Role, ["@P"] = u.Pin, ["@A"] = u.IsActive,
                    ["@POS"] = u.Permissions.CanAccessPOS,
                    ["@MI"] = u.Permissions.CanManageItems,
                    ["@VR"] = u.Permissions.CanViewReports,
                    ["@AD"] = u.Permissions.CanApplyDiscount,
                    ["@CI"] = u.Permissions.CanCancelInvoice,
                    ["@AM"] = u.Permissions.CanAccessManagement,
                    ["@MC"] = u.Permissions.CanManageCustomers
                });
            return Convert.ToInt32(id);
        }

        public static void Update(User u)
        {
            DatabaseHelper.ExecuteNonQuery(@"
UPDATE Users SET Name=@N,Role=@R,Pin=@P,IsActive=@A,
CanAccessPOS=@POS,CanManageItems=@MI,CanViewReports=@VR,
CanApplyDiscount=@AD,CanCancelInvoice=@CI,CanAccessManagement=@AM,CanManageCustomers=@MC
WHERE Id=@Id",
                new()
                {
                    ["@N"] = u.Name, ["@R"] = u.Role, ["@P"] = u.Pin, ["@A"] = u.IsActive,
                    ["@POS"] = u.Permissions.CanAccessPOS,
                    ["@MI"] = u.Permissions.CanManageItems,
                    ["@VR"] = u.Permissions.CanViewReports,
                    ["@AD"] = u.Permissions.CanApplyDiscount,
                    ["@CI"] = u.Permissions.CanCancelInvoice,
                    ["@AM"] = u.Permissions.CanAccessManagement,
                    ["@MC"] = u.Permissions.CanManageCustomers,
                    ["@Id"] = u.Id
                });
        }

        public static bool ChangePin(int userId, string oldPin, string newPin)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT Id FROM Users WHERE Id=@Id AND Pin=@Pin", new() { ["@Id"] = userId, ["@Pin"] = oldPin });
            if (dt.Rows.Count == 0) return false;
            DatabaseHelper.ExecuteNonQuery("UPDATE Users SET Pin=@NewPin WHERE Id=@Id", new() { ["@NewPin"] = newPin, ["@Id"] = userId });
            return true;
        }

        public static void Delete(int id) => DatabaseHelper.ExecuteNonQuery("DELETE FROM Users WHERE Id=@Id", new() { ["@Id"] = id });

        private static User MapUser(DataRow r)
        {
            bool SafeBool(string col) {
                try { return r.Table.Columns.Contains(col) && r[col] != DBNull.Value && (bool)r[col]; }
                catch { return false; }
            }
            return new User
            {
                Id = (int)r["Id"],
                Name = (string)r["Name"],
                Role = (string)r["Role"],
                Pin = (string)r["Pin"],
                IsActive = (bool)r["IsActive"],
                CreatedAt = (DateTime)r["CreatedAt"],
                Permissions = new UserPermissions
                {
                    CanAccessPOS = r.Table.Columns.Contains("CanAccessPOS") && r["CanAccessPOS"] != DBNull.Value ? (bool)r["CanAccessPOS"] : true,
                    CanManageItems = SafeBool("CanManageItems"),
                    CanViewReports = SafeBool("CanViewReports"),
                    CanApplyDiscount = SafeBool("CanApplyDiscount"),
                    CanCancelInvoice = SafeBool("CanCancelInvoice"),
                    CanAccessManagement = SafeBool("CanAccessManagement"),
                    CanManageCustomers = SafeBool("CanManageCustomers")
                }
            };
        }
    }
}
