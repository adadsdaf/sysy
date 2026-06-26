using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class SupplierService
    {
        public static List<Supplier> GetAll()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Suppliers ORDER BY Name");
            return dt.Rows.Cast<DataRow>().Select(MapSupplier).ToList();
        }

        public static int Create(Supplier s)
        {
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO Suppliers(Name,Phone,Email,Address,TaxNumber,ContactPerson,Balance,Notes)
VALUES(@N,@Ph,@Em,@Ad,@TN,@CP,@Bal,@No);SELECT SCOPE_IDENTITY();",
                new() { ["@N"]=s.Name, ["@Ph"]=(object?)s.Phone??DBNull.Value,
                    ["@Em"]=(object?)s.Email??DBNull.Value, ["@Ad"]=(object?)s.Address??DBNull.Value,
                    ["@TN"]=(object?)s.TaxNumber??DBNull.Value, ["@CP"]=(object?)s.ContactPerson??DBNull.Value,
                    ["@Bal"]=s.Balance, ["@No"]=(object?)s.Notes??DBNull.Value });
            return Convert.ToInt32(id);
        }

        public static void Update(Supplier s)
        {
            DatabaseHelper.ExecuteNonQuery(@"
UPDATE Suppliers SET Name=@N,Phone=@Ph,Email=@Em,Address=@Ad,TaxNumber=@TN,
ContactPerson=@CP,Notes=@No WHERE Id=@Id",
                new() { ["@N"]=s.Name, ["@Ph"]=(object?)s.Phone??DBNull.Value,
                    ["@Em"]=(object?)s.Email??DBNull.Value, ["@Ad"]=(object?)s.Address??DBNull.Value,
                    ["@TN"]=(object?)s.TaxNumber??DBNull.Value, ["@CP"]=(object?)s.ContactPerson??DBNull.Value,
                    ["@No"]=(object?)s.Notes??DBNull.Value, ["@Id"]=s.Id });
        }

        public static void UpdateBalance(int supplierId, decimal delta)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE Suppliers SET Balance=Balance+@D WHERE Id=@Id",
                new() { ["@D"]=delta, ["@Id"]=supplierId });
        }

        public static void Delete(int id) =>
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Suppliers WHERE Id=@Id", new() { ["@Id"]=id });

        // ── Purchase Orders ───────────────────────────────────────────────
        public static List<PurchaseOrder> GetOrders(int? supplierId = null, string? status = null)
        {
            var sql = @"SELECT po.*, s.Name AS SupplierName FROM PurchaseOrders po
                        JOIN Suppliers s ON po.SupplierId=s.Id WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (supplierId.HasValue) { sql += " AND po.SupplierId=@SId"; p["@SId"]=supplierId; }
            if (!string.IsNullOrEmpty(status)) { sql += " AND po.Status=@St"; p["@St"]=status; }
            sql += " ORDER BY po.CreatedAt DESC";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(MapOrder).ToList();
        }

        public static int CreateOrder(PurchaseOrder po)
        {
            var num = $"PO-{DateTime.Now:yyyyMMdd}-{DatabaseHelper.ExecuteScalar("SELECT COUNT(*)+1 FROM PurchaseOrders"):D4}";
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO PurchaseOrders(OrderNumber,SupplierId,OrderDate,Status,Total,AmountPaid,Notes)
VALUES(@Num,@SId,@OD,@St,@Tot,@Paid,@No);SELECT SCOPE_IDENTITY();",
                new() { ["@Num"]=num, ["@SId"]=po.SupplierId, ["@OD"]=po.OrderDate.Date,
                    ["@St"]=po.Status, ["@Tot"]=po.Total, ["@Paid"]=po.AmountPaid,
                    ["@No"]=(object?)po.Notes??DBNull.Value });
            var poId = Convert.ToInt32(id);
            foreach (var item in po.Items)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO PurchaseOrderItems(PurchaseOrderId,ItemId,ItemName,Unit,Quantity,UnitPrice)
VALUES(@PId,@IId,@IN,@U,@Q,@UP)",
                    new() { ["@PId"]=poId, ["@IId"]=item.ItemId, ["@IN"]=item.ItemName,
                        ["@U"]=(object?)item.Unit??DBNull.Value, ["@Q"]=item.Quantity, ["@UP"]=item.UnitPrice });
            }
            // Update supplier balance
            UpdateBalance(po.SupplierId, po.Total - po.AmountPaid);
            return poId;
        }

        public static void ReceiveOrder(int orderId)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM PurchaseOrderItems WHERE PurchaseOrderId=@Id",
                new() { ["@Id"]=orderId });
            foreach (DataRow r in dt.Rows)
            {
                var itemId = (int)r["ItemId"];
                var qty = (decimal)r["Quantity"];
                var cost = (decimal)r["UnitPrice"];
                // Update stock
                DatabaseHelper.ExecuteNonQuery("UPDATE Items SET Stock=ISNULL(Stock,0)+@Q WHERE Id=@Id",
                    new() { ["@Q"]=qty, ["@Id"]=itemId });
                // Log movement
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO StockMovements(ItemId,ItemName,MovementType,Quantity,UnitCost,ReferenceId,ReferenceType,Notes)
SELECT @IId,(SELECT Name FROM Items WHERE Id=@IId),'in',@Q,@C,@RefId,'purchase',N'استلام طلب شراء'",
                    new() { ["@IId"]=itemId, ["@Q"]=qty, ["@C"]=cost, ["@RefId"]=orderId });
            }
            DatabaseHelper.ExecuteNonQuery("UPDATE PurchaseOrders SET Status='received',ReceiveDate=GETDATE() WHERE Id=@Id",
                new() { ["@Id"]=orderId });
        }

        public static void PayOrderPartial(int orderId, decimal amount)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT SupplierId,Total,AmountPaid FROM PurchaseOrders WHERE Id=@Id",
                new() { ["@Id"]=orderId });
            if (dt.Rows.Count == 0) return;
            var suppId = (int)dt.Rows[0]["SupplierId"];
            DatabaseHelper.ExecuteNonQuery("UPDATE PurchaseOrders SET AmountPaid=AmountPaid+@Am WHERE Id=@Id",
                new() { ["@Am"]=amount, ["@Id"]=orderId });
            UpdateBalance(suppId, -amount);
        }

        // ── Supplier Account Statement ─────────────────────────────────────
        public static List<AccountStatement> GetStatement(int supplierId, DateTime from, DateTime to)
        {
            var rows = new List<AccountStatement>();
            var orders = GetOrders(supplierId).Where(o => o.CreatedAt >= from && o.CreatedAt <= to.AddDays(1)).ToList();
            decimal balance = 0;
            foreach (var o in orders.OrderBy(x => x.OrderDate))
            {
                balance += o.Total;
                rows.Add(new AccountStatement {
                    Date=o.OrderDate, Description=$"طلب شراء {o.OrderNumber}",
                    Type="debit", Debit=o.Total, Balance=balance, Reference=o.OrderNumber });
                if (o.AmountPaid > 0)
                {
                    balance -= o.AmountPaid;
                    rows.Add(new AccountStatement {
                        Date=o.OrderDate, Description=$"دفع جزئي لـ {o.OrderNumber}",
                        Type="credit", Credit=o.AmountPaid, Balance=balance, Reference=o.OrderNumber });
                }
            }
            return rows;
        }

        private static Supplier MapSupplier(DataRow r) => new Supplier
        {
            Id=(int)r["Id"], Name=(string)r["Name"],
            Phone=r["Phone"]==DBNull.Value?null:(string)r["Phone"],
            Email=r["Email"]==DBNull.Value?null:(string)r["Email"],
            Address=r["Address"]==DBNull.Value?null:(string)r["Address"],
            TaxNumber=r["TaxNumber"]==DBNull.Value?null:(string)r["TaxNumber"],
            ContactPerson=r["ContactPerson"]==DBNull.Value?null:(string)r["ContactPerson"],
            Balance=(decimal)r["Balance"],
            Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
            CreatedAt=(DateTime)r["CreatedAt"]
        };

        private static PurchaseOrder MapOrder(DataRow r) => new PurchaseOrder
        {
            Id=(int)r["Id"], OrderNumber=(string)r["OrderNumber"],
            SupplierId=(int)r["SupplierId"],
            SupplierName=r["SupplierName"]==DBNull.Value?null:(string)r["SupplierName"],
            OrderDate=(DateTime)r["OrderDate"],
            ReceiveDate=r["ReceiveDate"]==DBNull.Value?null:(DateTime?)r["ReceiveDate"],
            Status=(string)r["Status"], Total=(decimal)r["Total"],
            AmountPaid=(decimal)r["AmountPaid"],
            Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
            CreatedAt=(DateTime)r["CreatedAt"]
        };
    }
}
