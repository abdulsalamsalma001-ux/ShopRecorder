using SQLite;

namespace ShopRecorder.Data;

/// <summary>
/// All data access for ShopRecorder, backed by SQLite via sqlite-net-pcl.
/// The database file is created automatically if it does not exist, so the app
/// works out of the box locally, on Render and on Azure.
/// </summary>
public interface ISQLiteService
{
    Task Init();

    Task<List<Product>> GetProducts();
    Task<Product?> GetProduct(int id);
    Task<int> AddProduct(Product product);
    Task<int> UpdateProduct(Product product);
    Task<int> DeleteProduct(Product product);

    /// <summary>Records a sale atomically: validates stock, reduces stock and saves the sale. Returns the new sale id.</summary>
    Task<int> RecordSale(List<SaleItem> items, decimal amountPaid);

    Task<List<Sale>> GetTodaysSales();
    Task<List<Sale>> GetAllSales();
    Task<Sale?> GetSale(int id);
    Task<List<Product>> GetLowStockProducts();
}

public class SQLiteService : ISQLiteService
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;

    public async Task Init()
    {
        if (_db is not null) return;
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return;

            // Where to keep the database file:
            //  - DATA_DIR env var if provided (e.g. a mounted disk on Render)
            //  - otherwise the app folder (writable locally, in the Render container and on Azure App Service)
            var dir = Environment.GetEnvironmentVariable("DATA_DIR");
            if (string.IsNullOrWhiteSpace(dir)) dir = AppContext.BaseDirectory;
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, "shoprecorder.db");

            _db = new SQLiteAsyncConnection(path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);

            await _db.CreateTableAsync<Product>();
            await _db.CreateTableAsync<Sale>();
            await SeedSampleProductsIfEmptyAsync();
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ---------- Products ----------

    public async Task<List<Product>> GetProducts() =>
        await (await Db()).Table<Product>().OrderBy(p => p.Name).ToListAsync();

    public async Task<Product?> GetProduct(int id) =>
        await (await Db()).Table<Product>().Where(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<int> AddProduct(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        var db = await Db();
        await db.InsertAsync(product); // sqlite-net sets the auto-increment Id on the object
        return product.Id;
    }

    public async Task<int> UpdateProduct(Product product) =>
        await (await Db()).UpdateAsync(product);

    public async Task<int> DeleteProduct(Product product) =>
        await (await Db()).DeleteAsync(product);

    // ---------- Sales ----------

    public async Task<int> RecordSale(List<SaleItem> items, decimal amountPaid)
    {
        var total = items.Sum(i => i.Subtotal);
        var sale = new Sale
        {
            SaleDate = DateTime.UtcNow,
            TotalAmount = total,
            AmountPaid = amountPaid,
            ChangeDue = amountPaid - total,
            ItemsJson = Sale.SerializeItems(items),
        };

        var db = await Db();

        // One atomic transaction: check + reduce stock for every line, then insert the sale.
        await db.RunInTransactionAsync(conn =>
        {
            foreach (var item in items)
            {
                var product = conn.Find<Product>(item.ProductId)
                    ?? throw new InvalidOperationException($"Product '{item.ProductName}' no longer exists.");

                if (product.StockQuantity < item.Quantity)
                    throw new InsufficientStockException(product.Name, product.StockQuantity);

                product.StockQuantity -= item.Quantity;
                conn.Update(product);
            }

            conn.Insert(sale);
        });

        return sale.Id;
    }

    public async Task<List<Sale>> GetTodaysSales()
    {
        var start = ShopClock.TodayStartUtc();
        return await (await Db()).Table<Sale>()
            .Where(s => s.SaleDate >= start)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<List<Sale>> GetAllSales() =>
        await (await Db()).Table<Sale>().OrderByDescending(s => s.SaleDate).ToListAsync();

    public async Task<Sale?> GetSale(int id) =>
        await (await Db()).Table<Sale>().Where(s => s.Id == id).FirstOrDefaultAsync();

    // ---------- Low stock ----------

    public async Task<List<Product>> GetLowStockProducts() =>
        (await GetProducts()).Where(p => p.StockQuantity <= p.LowStockThreshold).ToList();

    // ---------- Internals ----------

    private async Task<SQLiteAsyncConnection> Db()
    {
        if (_db is null) await Init();
        return _db!;
    }

    /// <summary>
    /// First run only: sample stock for an electronics &amp; home-appliance shop
    /// (phones, appliances, accessories) so the app is instantly demoable. Edit or delete freely.
    /// </summary>
    private async Task SeedSampleProductsIfEmptyAsync()
    {
        if (await (await Db()).Table<Product>().CountAsync() > 0) return;

        var samples = new List<Product>
        {
            // ---- Phones ----
            new() { Name = "Samsung Galaxy A15",      Price = 2600.00m, StockQuantity = 4  },  // low stock demo
            new() { Name = "Tecno Spark 20C",         Price = 1150.00m, StockQuantity = 8  },
            new() { Name = "Infinix Hot 40i",         Price = 1450.00m, StockQuantity = 6  },
            new() { Name = "Itel A70",                Price = 780.00m,  StockQuantity = 12 },
            new() { Name = "Nokia 105",               Price = 240.00m,  StockQuantity = 20 },

            // ---- Home appliances & power ----
            new() { Name = "32\" LED TV",             Price = 1900.00m, StockQuantity = 2  },  // low stock demo
            new() { Name = "16\" Standing Fan",       Price = 420.00m,  StockQuantity = 5  },  // low stock demo
            new() { Name = "Electric Kettle 1.7L",    Price = 180.00m,  StockQuantity = 7  },
            new() { Name = "Blender 2L",              Price = 350.00m,  StockQuantity = 3  },  // low stock demo
            new() { Name = "Solar Rechargeable Lamp", Price = 130.00m,  StockQuantity = 22 },

            // ---- Accessories ----
            new() { Name = "Power Bank 20,000mAh",    Price = 220.00m,  StockQuantity = 9  },
            new() { Name = "Wireless Earbuds",        Price = 180.00m,  StockQuantity = 14 },
            new() { Name = "USB-C Fast Charger",      Price = 45.00m,   StockQuantity = 40 },
            new() { Name = "USB Flash Drive 64GB",    Price = 95.00m,   StockQuantity = 18 },
            new() { Name = "Extension Board 4-Way",   Price = 60.00m,   StockQuantity = 25 },
        };
        await (await Db()).InsertAllAsync(samples);
    }
}
