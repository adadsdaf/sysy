using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class StockService
    {
        public static List<StockMovement> GetMovements(int? itemId = null, string? type = null, DateTime? from = null, DateTime? to = null)
        {
            var sql = @"SELECT sm.*, u.Name AS CreatedByName FROM StockMovements sm
                        LEFT JOIN Users u ON sm.CreatedBy=u.Id WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (itemId.HasValue) { sql += " AND sm.ItemId=@IId"; p["@IId"]=itemId; }
            if (!string.IsNullOrEmpty(type)) { sql += " AND sm.MovementType=@Tp"; p["@Tp"]=type; }
            if (from.HasValue) { sql += " AND sm.CreatedAt>=@Fr"; p["@Fr"]=from.Value.Date; }
            if (to.HasValue) { sql += " AND sm.CreatedAt<=@To"; p["@To"]=to.Value.Date.AddDays(1); }
            sql += " ORDER BY sm.CreatedAt DESC";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(r => new StockMovement
            {
                Id=(int)r["Id"], ItemId=(int)r["ItemId"],
                ItemName=r["ItemName"]==DBNull.Value?null:(string)r["ItemName"],
                MovementType=(string)r["MovementType"], Quantity=(decimal)r["Quantity"],
                UnitCost=(decimal)r["UnitCost"],
                ReferenceId=r["ReferenceId"]==DBNull.Value?null:(int?)Convert.ToInt32(r["ReferenceId"]),
                ReferenceType=r["ReferenceType"]==DBNull.Value?null:(string)r["ReferenceType"],
                Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
                CreatedBy=r["CreatedBy"]==DBNull.Value?null:(int?)Convert.ToInt32(r["CreatedBy"]),
                CreatedByName=r["CreatedByName"]==DBNull.Value?null:(string)r["CreatedByName"],
                CreatedAt=(DateTime)r["CreatedAt"]
            }).ToList();
        }

        public static void AddMovement(int itemId, string movementType, decimal quantity, decimal unitCost,
            string? notes = null, int? createdBy = null, int? refId = null, string? refType = null)
        {
            DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO StockMovements(ItemId,ItemName,MovementType,Quantity,UnitCost,ReferenceId,ReferenceType,Notes,CreatedBy)
SELECT @IId,(SELECT Name FROM Items WHERE Id=@IId),@MT,@Q,@UC,@RefId,@RefT,@No,@Cr",
                new() { ["@IId"]=itemId, ["@MT"]=movementType, ["@Q"]=quantity,
                    ["@UC"]=unitCost, ["@RefId"]=(object?)refId??DBNull.Value,
                    ["@RefT"]=(object?)refType??DBNull.Value, ["@No"]=(object?)notes??DBNull.Value,
                    ["@Cr"]=(object?)createdBy??DBNull.Value });

            var delta = movementType == "in" ? quantity : -quantity;
            DatabaseHelper.ExecuteNonQuery("UPDATE Items SET Stock=ISNULL(Stock,0)+@D WHERE Id=@Id",
                new() { ["@D"]=delta, ["@Id"]=itemId });
        }

        public static List<Item> GetLowStockItems()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT i.*, c.Name AS CategoryName, c.Color AS CategoryColor
FROM Items i LEFT JOIN Categories c ON i.CategoryId=c.Id
WHERE i.IsAvailable=1 AND i.Stock IS NOT NULL AND i.MinStock IS NOT NULL AND i.Stock <= i.MinStock
ORDER BY i.Stock ASC");
            return dt.Rows.Cast<DataRow>().Select(r => new Item
            {
                Id=(int)r["Id"], Name=(string)r["Name"],
                ItemNumber=r["ItemNumber"]==DBNull.Value?null:(string)r["ItemNumber"],
                Price=(decimal)r["Price"], Stock=r["Stock"]==DBNull.Value?(int?)null:(int?)Convert.ToInt32(r["Stock"]),
                MinStock=r["MinStock"]==DBNull.Value?null:(decimal?)r["MinStock"],
                Unit=r["Unit"]==DBNull.Value?null:(string)r["Unit"],
                CategoryName=r["CategoryName"]==DBNull.Value?null:(string)r["CategoryName"],
                CategoryColor=r["CategoryColor"]==DBNull.Value?null:(string)r["CategoryColor"],
                IsAvailable=(bool)r["IsAvailable"]
            }).ToList();
        }

        public static (decimal TotalValue, int ItemCount, int LowStockCount) GetInventorySummary()
        {
            var dt = DatabaseHelper.ExecuteQuery(@"
SELECT
    ISNULL(SUM(ISNULL(Stock,0)*Price),0) AS TotalValue,
    COUNT(*) AS ItemCount,
    COUNT(CASE WHEN ISNULL(Stock,0) <= ISNULL(MinStock,0) THEN 1 END) AS LowStockCount
FROM Items WHERE IsAvailable=1");
            var r = dt.Rows[0];
            return ((decimal)r["TotalValue"], Convert.ToInt32(r["ItemCount"]), Convert.ToInt32(r["LowStockCount"]));
        }
    }
}
