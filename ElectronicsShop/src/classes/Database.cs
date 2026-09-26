using System.IO;
using System.Text.Json;
using ElectronicsShop.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsShop.classes
{
    public class Database
    {
        public static ShopDbContext GetContext()
        {
            string? connection = Environment.GetEnvironmentVariable("ELECTRONICS_SHOP_CONNECTION");

            if (string.IsNullOrWhiteSpace(connection))
            {
                string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                string json = File.ReadAllText(path);

                using (JsonDocument settings = JsonDocument.Parse(json))
                {
                    connection = settings.RootElement.GetProperty("ConnectionStrings")
                        .GetProperty("Shop").GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new Exception("Укажите строку подключения в appsettings.json");
            }

            DbContextOptionsBuilder<ShopDbContext> options = new DbContextOptionsBuilder<ShopDbContext>();
            options.UseSqlServer(connection, sql => sql.CommandTimeout(20));
            return new ShopDbContext(options.Options);
        }
    }
}
