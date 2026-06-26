using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class FinanceService
    {
        // ── Cash Boxes ────────────────────────────────────────────────────
        public static List<CashBox> GetCashBoxes()
        {
            return DatabaseHelper.ExecuteQuery("SELECT * FROM CashBoxes ORDER BY IsDefault DESC, Name")
                .Rows.Cast<DataRow>().Select(r => new CashBox
                {
                    Id=(int)r["Id"], Name=(string)r["Name"],
                    Description=r["Description"]==DBNull.Value?null:(string)r["Description"],
                    Balance=(decimal)r["Balance"], IsDefault=(bool)r["IsDefault"],
                    CreatedAt=(DateTime)r["CreatedAt"]
                }).ToList();
        }

        public static int CreateCashBox(CashBox cb)
        {
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO CashBoxes(Name,Description,Balance,IsDefault)
VALUES(@N,@D,@B,@Def);SELECT SCOPE_IDENTITY();",
                new() { ["@N"]=cb.Name, ["@D"]=(object?)cb.Description??DBNull.Value,
                    ["@B"]=cb.Balance, ["@Def"]=cb.IsDefault });
            return Convert.ToInt32(id);
        }

        // ── Cash Vouchers ─────────────────────────────────────────────────
        public static List<CashVoucher> GetVouchers(string? type = null, DateTime? from = null, DateTime? to = null, int? cashBoxId = null)
        {
            var sql = @"SELECT v.*, cb.Name AS CashBoxName, u.Name AS CreatedByName
                        FROM CashVouchers v
                        JOIN CashBoxes cb ON v.CashBoxId=cb.Id
                        LEFT JOIN Users u ON v.CreatedBy=u.Id
                        WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(type)) { sql += " AND v.VoucherType=@Tp"; p["@Tp"]=type; }
            if (from.HasValue) { sql += " AND v.VoucherDate>=@Fr"; p["@Fr"]=from.Value.Date; }
            if (to.HasValue) { sql += " AND v.VoucherDate<=@To"; p["@To"]=to.Value.Date; }
            if (cashBoxId.HasValue) { sql += " AND v.CashBoxId=@CB"; p["@CB"]=cashBoxId; }
            sql += " ORDER BY v.CreatedAt DESC";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(MapVoucher).ToList();
        }

        public static int CreateVoucher(CashVoucher v, int? createdBy = null)
        {
            var num = GenerateVoucherNumber(v.VoucherType);
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO CashVouchers(VoucherNumber,VoucherType,CashBoxId,Amount,PayeeName,PayeeType,PayeeId,Description,ReferenceNumber,VoucherDate,CreatedBy,Notes)
VALUES(@Num,@VT,@CB,@Am,@PN,@PT,@PId,@Desc,@Ref,@VD,@Cr,@No);SELECT SCOPE_IDENTITY();",
                new() { ["@Num"]=num, ["@VT"]=v.VoucherType, ["@CB"]=v.CashBoxId,
                    ["@Am"]=v.Amount, ["@PN"]=v.PayeeName,
                    ["@PT"]=(object?)v.PayeeType??DBNull.Value, ["@PId"]=(object?)v.PayeeId??DBNull.Value,
                    ["@Desc"]=v.Description, ["@Ref"]=(object?)v.ReferenceNumber??DBNull.Value,
                    ["@VD"]=v.VoucherDate.Date, ["@Cr"]=(object?)createdBy??DBNull.Value,
                    ["@No"]=(object?)v.Notes??DBNull.Value });

            // Update cash box balance
            var delta = v.VoucherType == "receipt" ? v.Amount : -v.Amount;
            DatabaseHelper.ExecuteNonQuery("UPDATE CashBoxes SET Balance=Balance+@D WHERE Id=@Id",
                new() { ["@D"]=delta, ["@Id"]=v.CashBoxId });

            return Convert.ToInt32(id);
        }

        public static void DeleteVoucher(int id)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM CashVouchers WHERE Id=@Id", new() { ["@Id"]=id });
            if (dt.Rows.Count == 0) return;
            var r = dt.Rows[0];
            var amount = (decimal)r["Amount"];
            var vtype = (string)r["VoucherType"];
            var cbId = (int)r["CashBoxId"];
            // Reverse the balance change
            var delta = vtype == "receipt" ? -amount : amount;
            DatabaseHelper.ExecuteNonQuery("UPDATE CashBoxes SET Balance=Balance+@D WHERE Id=@Id",
                new() { ["@D"]=delta, ["@Id"]=cbId });
            DatabaseHelper.ExecuteNonQuery("DELETE FROM CashVouchers WHERE Id=@Id", new() { ["@Id"]=id });
        }

        // ── Cash Box Statement ─────────────────────────────────────────────
        public static List<AccountStatement> GetCashBoxStatement(int cashBoxId, DateTime from, DateTime to)
        {
            var vouchers = GetVouchers(cashBoxId: cashBoxId, from: from, to: to.AddDays(1));
            var rows = new List<AccountStatement>();
            decimal balance = 0;
            foreach (var v in vouchers.OrderBy(x => x.VoucherDate))
            {
                if (v.VoucherType == "receipt")
                {
                    balance += v.Amount;
                    rows.Add(new AccountStatement { Date=v.VoucherDate, Description=v.Description,
                        Type="credit", Credit=v.Amount, Balance=balance, Reference=v.VoucherNumber });
                }
                else
                {
                    balance -= v.Amount;
                    rows.Add(new AccountStatement { Date=v.VoucherDate, Description=v.Description,
                        Type="debit", Debit=v.Amount, Balance=balance, Reference=v.VoucherNumber });
                }
            }
            return rows;
        }

        // ── Dashboard Summary ─────────────────────────────────────────────
        public static (decimal TotalIn, decimal TotalOut, decimal NetBalance) GetTodaySummary()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT
    ISNULL(SUM(CASE WHEN VoucherType='receipt' THEN Amount ELSE 0 END),0) AS TotalIn,
    ISNULL(SUM(CASE WHEN VoucherType='payment' THEN Amount ELSE 0 END),0) AS TotalOut
FROM CashVouchers WHERE CAST(VoucherDate AS DATE)=CAST(GETDATE() AS DATE)");
            var r = dt.Rows[0];
            var totalIn = (decimal)r["TotalIn"];
            var totalOut = (decimal)r["TotalOut"];
            return (totalIn, totalOut, totalIn - totalOut);
        }

        private static string GenerateVoucherNumber(string type)
        {
            var prefix = type == "receipt" ? "RCV" : "PAY";
            var count = DatabaseHelper.ExecuteScalar($"SELECT COUNT(*)+1 FROM CashVouchers WHERE VoucherType='{type}'");
            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{Convert.ToInt32(count):D4}";
        }

        private static CashVoucher MapVoucher(DataRow r) => new CashVoucher
        {
            Id=(int)r["Id"], VoucherNumber=(string)r["VoucherNumber"],
            VoucherType=(string)r["VoucherType"], CashBoxId=(int)r["CashBoxId"],
            CashBoxName=r["CashBoxName"]==DBNull.Value?null:(string)r["CashBoxName"],
            Amount=(decimal)r["Amount"], PayeeName=(string)r["PayeeName"],
            PayeeType=r["PayeeType"]==DBNull.Value?null:(string)r["PayeeType"],
            PayeeId=r["PayeeId"]==DBNull.Value?null:(int?)Convert.ToInt32(r["PayeeId"]),
            Description=(string)r["Description"],
            ReferenceNumber=r["ReferenceNumber"]==DBNull.Value?null:(string)r["ReferenceNumber"],
            VoucherDate=(DateTime)r["VoucherDate"],
            CreatedBy=r["CreatedBy"]==DBNull.Value?null:(int?)Convert.ToInt32(r["CreatedBy"]),
            CreatedByName=r["CreatedByName"]==DBNull.Value?null:(string)r["CreatedByName"],
            Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
            CreatedAt=(DateTime)r["CreatedAt"]
        };
    }
}
