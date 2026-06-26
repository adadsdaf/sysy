using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class CategoryService
    {
        public static List<Category> GetAll()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Categories ORDER BY SortOrder, Name");
            return dt.Rows.Cast<DataRow>().Select(r => new Category
            {
                Id = (int)r["Id"], Name = (string)r["Name"],
                Color = r["Color"] == DBNull.Value ? "#4CAF50" : (string)r["Color"],
                SortOrder = r["SortOrder"] == DBNull.Value ? 0 : (int)r["SortOrder"]
            }).ToList();
        }

        public static int Create(Category cat)
        {
            var id = DatabaseHelper.ExecuteScalar("INSERT INTO Categories(Name,Color,SortOrder) VALUES(@N,@C,@S);SELECT SCOPE_IDENTITY();",
                new() { ["@N"] = cat.Name, ["@C"] = cat.Color, ["@S"] = cat.SortOrder });
            return Convert.ToInt32(id);
        }

        public static void Update(Category cat)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE Categories SET Name=@N,Color=@C,SortOrder=@S WHERE Id=@Id",
                new() { ["@N"] = cat.Name, ["@C"] = cat.Color, ["@S"] = cat.SortOrder, ["@Id"] = cat.Id });
        }

        public static void Delete(int id) => DatabaseHelper.ExecuteNonQuery("DELETE FROM Categories WHERE Id=@Id", new() { ["@Id"] = id });
    }
}
