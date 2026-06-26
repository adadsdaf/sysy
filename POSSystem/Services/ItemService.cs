using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class ItemService
    {
        public static List<Item> GetItems(int? categoryId = null, string? search = null, bool? favorites = null)
        {
            var sql = @"SELECT i.*, c.Name AS CategoryName, c.Color AS CategoryColor 
                        FROM Items i LEFT JOIN Categories c ON i.CategoryId = c.Id
                        WHERE i.IsAvailable = 1";
            var p = new Dictionary<string, object?>();
            if (categoryId.HasValue) { sql += " AND i.CategoryId = @CatId"; p["@CatId"] = categoryId; }
            if (!string.IsNullOrEmpty(search)) { sql += " AND (i.Name LIKE @S OR i.Barcode LIKE @S OR i.ItemNumber LIKE @S)"; p["@S"] = $"%{search}%"; }
            if (favorites == true) { sql += " AND i.IsFavorite = 1"; }
            sql += " ORDER BY i.Name";
            var dt = DatabaseHelper.ExecuteQuery(sql, p);
            return dt.Rows.Cast<DataRow>().Select(MapItem).ToList();
        }

        public static Item? GetById(int id)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT i.*, c.Name AS CategoryName, c.Color AS CategoryColor FROM Items i LEFT JOIN Categories c ON i.CategoryId=c.Id WHERE i.Id=@Id", new() { ["@Id"] = id });
            return dt.Rows.Count > 0 ? MapItem(dt.Rows[0]) : null;
        }

        public static Item? GetByBarcode(string barcode)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT i.*, c.Name AS CategoryName, c.Color AS CategoryColor FROM Items i LEFT JOIN Categories c ON i.CategoryId=c.Id WHERE i.Barcode=@B", new() { ["@B"] = barcode });
            return dt.Rows.Count > 0 ? MapItem(dt.Rows[0]) : null;
        }

        public static int Create(Item item)
        {
            var id = DatabaseHelper.ExecuteScalar(@"INSERT INTO Items (Name,NameAr,Barcode,ItemNumber,Price,TaxRate,DiscountRate,CategoryId,Stock,IsFavorite,IsAvailable,Notes)
                VALUES (@Name,@NameAr,@Barcode,@ItemNum,@Price,@Tax,@Disc,@CatId,@Stock,@Fav,@Avail,@Notes);SELECT SCOPE_IDENTITY();",
                new() { ["@Name"]=item.Name, ["@NameAr"]=item.NameAr, ["@Barcode"]=item.Barcode, ["@ItemNum"]=item.ItemNumber,
                    ["@Price"]=item.Price, ["@Tax"]=item.TaxRate, ["@Disc"]=item.DiscountRate, ["@CatId"]=item.CategoryId,
                    ["@Stock"]=item.Stock, ["@Fav"]=item.IsFavorite, ["@Avail"]=item.IsAvailable, ["@Notes"]=item.Notes });
            return Convert.ToInt32(id);
        }

        public static void Update(Item item)
        {
            DatabaseHelper.ExecuteNonQuery(@"UPDATE Items SET Name=@Name,NameAr=@NameAr,Barcode=@Barcode,ItemNumber=@ItemNum,Price=@Price,
                TaxRate=@Tax,DiscountRate=@Disc,CategoryId=@CatId,Stock=@Stock,IsFavorite=@Fav,IsAvailable=@Avail,Notes=@Notes WHERE Id=@Id",
                new() { ["@Name"]=item.Name, ["@NameAr"]=item.NameAr, ["@Barcode"]=item.Barcode, ["@ItemNum"]=item.ItemNumber,
                    ["@Price"]=item.Price, ["@Tax"]=item.TaxRate, ["@Disc"]=item.DiscountRate, ["@CatId"]=item.CategoryId,
                    ["@Stock"]=item.Stock, ["@Fav"]=item.IsFavorite, ["@Avail"]=item.IsAvailable, ["@Notes"]=item.Notes, ["@Id"]=item.Id });
        }

        public static void Delete(int id) => DatabaseHelper.ExecuteNonQuery("DELETE FROM Items WHERE Id=@Id", new() { ["@Id"] = id });

        public static void ToggleFavorite(int id) => DatabaseHelper.ExecuteNonQuery("UPDATE Items SET IsFavorite = ~IsFavorite WHERE Id=@Id", new() { ["@Id"] = id });

        private static Item MapItem(DataRow r) => new Item
        {
            Id = (int)r["Id"], Name = (string)r["Name"],
            NameAr = r["NameAr"] == DBNull.Value ? null : (string)r["NameAr"],
            Barcode = r["Barcode"] == DBNull.Value ? null : (string)r["Barcode"],
            ItemNumber = r["ItemNumber"] == DBNull.Value ? null : (string)r["ItemNumber"],
            Price = (decimal)r["Price"], TaxRate = (decimal)r["TaxRate"], DiscountRate = (decimal)r["DiscountRate"],
            CategoryId = r["CategoryId"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["CategoryId"]),
            CategoryName = r["CategoryName"] == DBNull.Value ? null : (string)r["CategoryName"],
            CategoryColor = r["CategoryColor"] == DBNull.Value ? null : (string)r["CategoryColor"],
            Stock = r["Stock"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["Stock"]),
            IsFavorite = (bool)r["IsFavorite"], IsAvailable = (bool)r["IsAvailable"],
            Notes = r["Notes"] == DBNull.Value ? null : (string)r["Notes"],
        };
    }
}
