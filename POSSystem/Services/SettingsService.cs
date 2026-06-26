using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class SettingsService
    {
        private static AppSettings? _cached;

        public static AppSettings Get()
        {
            if (_cached != null) return _cached;
            var dt = DatabaseHelper.ExecuteQuery("SELECT TOP 1 * FROM AppSettings");
            if (dt.Rows.Count == 0) return new AppSettings();
            var r = dt.Rows[0];
            string? SafeStr(string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? (string)r[col] : null;
            bool SafeBool(string col, bool def = false) {
                if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return def;
                return (bool)r[col];
            }
            _cached = new AppSettings
            {
                Id = (int)r["Id"],
                StoreName = (string)r["StoreName"],
                StoreNameAr = SafeStr("StoreNameAr"),
                Address = SafeStr("Address"),
                Phone = SafeStr("Phone"),
                TaxRate = (decimal)r["TaxRate"],
                ServiceRate = (decimal)r["ServiceRate"],
                Currency = (string)r["Currency"],
                CurrencySymbol = (string)r["CurrencySymbol"],
                ReceiptFooter = SafeStr("ReceiptFooter"),
                ReceiptNotes = SafeStr("ReceiptNotes"),
                LogoPath = SafeStr("LogoPath"),
                PrintCustomerCopy = SafeBool("PrintCustomerCopy", true),
                PrintKitchenSlips = SafeBool("PrintKitchenSlips", true),
                LicenseStatus = (string)r["LicenseStatus"]
            };
            return _cached;
        }

        public static void Save(AppSettings s)
        {
            _cached = null;
            DatabaseHelper.ExecuteNonQuery(@"UPDATE AppSettings SET
                StoreName=@SN, StoreNameAr=@SA, Address=@Ad, Phone=@Ph,
                TaxRate=@Tax, ServiceRate=@Svc, Currency=@Cur, CurrencySymbol=@Sym,
                ReceiptFooter=@RF, ReceiptNotes=@RN, LogoPath=@LP,
                PrintCustomerCopy=@PC, PrintKitchenSlips=@PK
                WHERE Id=@Id",
                new()
                {
                    ["@SN"] = s.StoreName, ["@SA"] = (object?)s.StoreNameAr ?? DBNull.Value,
                    ["@Ad"] = (object?)s.Address ?? DBNull.Value, ["@Ph"] = (object?)s.Phone ?? DBNull.Value,
                    ["@Tax"] = s.TaxRate, ["@Svc"] = s.ServiceRate,
                    ["@Cur"] = s.Currency, ["@Sym"] = s.CurrencySymbol,
                    ["@RF"] = (object?)s.ReceiptFooter ?? DBNull.Value,
                    ["@RN"] = (object?)s.ReceiptNotes ?? DBNull.Value,
                    ["@LP"] = (object?)s.LogoPath ?? DBNull.Value,
                    ["@PC"] = s.PrintCustomerCopy, ["@PK"] = s.PrintKitchenSlips,
                    ["@Id"] = s.Id
                });
        }

        public static void ClearCache() => _cached = null;
    }
}
