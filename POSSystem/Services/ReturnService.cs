using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class ReturnService
    {
        public static List<ReturnInvoice> GetAll(DateTime? from = null, DateTime? to = null)
        {
            var sql = @"SELECT r.*, c.Name AS CustomerName, u.Name AS CashierName
                        FROM ReturnInvoices r
                        LEFT JOIN Customers c ON r.CustomerId=c.Id
                        LEFT JOIN Users u ON r.CashierId=u.Id
                        WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (from.HasValue) { sql += " AND r.CreatedAt>=@Fr"; p["@Fr"]=from.Value.Date; }
            if (to.HasValue) { sql += " AND r.CreatedAt<=@To"; p["@To"]=to.Value.Date.AddDays(1); }
            sql += " ORDER BY r.CreatedAt DESC";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(MapReturn).ToList();
        }

        public static int Create(ReturnInvoice ret, int? cashierId)
        {
            var num = $"RET-{DateTime.Now:yyyyMMdd}-{Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*)+1 FROM ReturnInvoices")):D4}";
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO ReturnInvoices(ReturnNumber,OriginalInvoiceId,OriginalInvoiceNumber,CustomerId,CashierId,Total,Reason,RefundMethod,Notes)
VALUES(@Num,@OId,@ONum,@CustId,@Cash,@Tot,@Re,@Ref,@No);SELECT SCOPE_IDENTITY();",
                new() { ["@Num"]=num,
                    ["@OId"]=(object?)ret.OriginalInvoiceId??DBNull.Value,
                    ["@ONum"]=(object?)ret.OriginalInvoiceNumber??DBNull.Value,
                    ["@CustId"]=(object?)ret.CustomerId??DBNull.Value,
                    ["@Cash"]=(object?)cashierId??DBNull.Value,
                    ["@Tot"]=ret.Total, ["@Re"]=ret.Reason,
                    ["@Ref"]=ret.RefundMethod,
                    ["@No"]=(object?)ret.Notes??DBNull.Value });
            var retId = Convert.ToInt32(id);
            foreach (var item in ret.Items)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO ReturnInvoiceItems(ReturnInvoiceId,ItemId,ItemName,Quantity,UnitPrice,Reason)
VALUES(@RId,@IId,@IN,@Q,@UP,@Re)",
                    new() { ["@RId"]=retId, ["@IId"]=item.ItemId, ["@IN"]=item.ItemName,
                        ["@Q"]=item.Quantity, ["@UP"]=item.UnitPrice,
                        ["@Re"]=(object?)item.Reason??DBNull.Value });
                // Return stock
                DatabaseHelper.ExecuteNonQuery("UPDATE Items SET Stock=ISNULL(Stock,0)+@Q WHERE Id=@Id",
                    new() { ["@Q"]=item.Quantity, ["@Id"]=item.ItemId });
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO StockMovements(ItemId,ItemName,MovementType,Quantity,UnitCost,ReferenceId,ReferenceType,Notes)
SELECT @IId,(SELECT Name FROM Items WHERE Id=@IId),'return',@Q,@UP,@RefId,'return',N'مرتجع'",
                    new() { ["@IId"]=item.ItemId, ["@Q"]=item.Quantity, ["@UP"]=item.UnitPrice, ["@RefId"]=retId });
            }
            return retId;
        }

        private static ReturnInvoice MapReturn(DataRow r) => new ReturnInvoice
        {
            Id=(int)r["Id"], ReturnNumber=(string)r["ReturnNumber"],
            OriginalInvoiceId=r["OriginalInvoiceId"]==DBNull.Value?null:(int?)Convert.ToInt32(r["OriginalInvoiceId"]),
            OriginalInvoiceNumber=r["OriginalInvoiceNumber"]==DBNull.Value?null:(string)r["OriginalInvoiceNumber"],
            CustomerId=r["CustomerId"]==DBNull.Value?null:(int?)Convert.ToInt32(r["CustomerId"]),
            CustomerName=r["CustomerName"]==DBNull.Value?null:(string)r["CustomerName"],
            CashierId=r["CashierId"]==DBNull.Value?null:(int?)Convert.ToInt32(r["CashierId"]),
            CashierName=r["CashierName"]==DBNull.Value?null:(string)r["CashierName"],
            Total=(decimal)r["Total"], Reason=(string)r["Reason"],
            RefundMethod=(string)r["RefundMethod"],
            Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
            CreatedAt=(DateTime)r["CreatedAt"]
        };
    }
}
