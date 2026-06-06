using System.Diagnostics;
using System.Net.Http;
using Serilog;
using SyncLoggingApp.Models; 

const int WarmUpIterations = 50;
const int MainIterations = 1000;
const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(
        path: "logs/sync-log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var random = new Random(42);

using var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(15);

static int Fibonacci(int n) => n <= 1 ? n : Fibonacci(n - 1) + Fibonacci(n - 2);

static string ReverseString(string input)
{
    char[] chars = input.ToCharArray();
    Array.Reverse(chars);
    return new string(chars);
}

static List<int> GenerateFilteredSortedIntegers(Random rng)
{
    return Enumerable.Range(0, 100)
        .Select(_ => rng.Next(1, 1000))
        .OrderBy(x => x)
        .Where(x => x % 2 == 0)
        .ToList();
}

static ProductModel CreateProduct(Random rng, int id)
{
    var categories = new[] { "Electronics", "Clothing", "Food", "Furniture", "Books" };
    var names = new[] { "Widget A", "Gadget B", "Item C", "Product D", "Article E" };
    return new ProductModel
    {
        ProductId = id,
        ProductName = names[rng.Next(names.Length)],
        Category = categories[rng.Next(categories.Length)],
        Price = Math.Round((decimal)(rng.NextDouble() * 999 + 1), 2),
        StockQuantity = rng.Next(0, 500),
        CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 365))
    };
}

static CustomerModel CreateCustomer(Random rng, int id)
{
    var names = new[] { "Alice Smith", "Bob Jones", "Carol White", "Dave Brown", "Eve Davis" };
    var countries = new[] { "US", "UK", "DE", "FR", "AU" };
    return new CustomerModel
    {
        CustomerId = id,
        FullName = names[rng.Next(names.Length)],
        Email = $"user{id}@example.com",
        Country = countries[rng.Next(countries.Length)],
        IsVerified = rng.Next(2) == 1,
        RegisteredAt = DateTime.UtcNow.AddDays(-rng.Next(1, 730))
    };
}

static OrderModel CreateOrder(Random rng, int id, int customerId)
{
    var statuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
    bool delivered = rng.Next(2) == 1;
    return new OrderModel
    {
        OrderId = id,
        CustomerId = customerId,
        TotalAmount = Math.Round((decimal)(rng.NextDouble() * 2000 + 5), 2),
        Status = statuses[rng.Next(statuses.Length)],
        OrderedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 60)),
        DeliveredAt = delivered ? DateTime.UtcNow.AddDays(-rng.Next(0, 30)) : null
    };
}

static OrderItemModel CreateOrderItem(Random rng, int id, int orderId, int productId, decimal unitPrice)
{
    return new OrderItemModel
    {
        OrderItemId = id,
        OrderId = orderId,
        ProductId = productId,
        Quantity = rng.Next(1, 20),
        UnitPrice = unitPrice
    };
}

static string BuildRandomString(Random rng, string chars, int length)
{
    return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
}

// Blocking API call + Thread.Sleep — thread is fully frozen for the entire duration.
static void CallApiAndSleep(HttpClient http, string url, string apiName, int iteration)
{
    try
    {
        string response = http.GetStringAsync(url).GetAwaiter().GetResult();
        Log.Information(
            "External API response received: ApiName={ApiName} Iteration={Iteration} ResponseLength={ResponseLength}",
            apiName, iteration, response.Length);
    }
    catch (Exception ex)
    {
        Log.Warning(
            "External API call failed: ApiName={ApiName} Iteration={Iteration} Error={Error}",
            apiName, iteration, ex.Message);
    }
}


static void RunIterations(HttpClient http, Random rng, string chars, int count)
{
    for (int i = 0; i < count; i++)
    {
        var product   = CreateProduct(rng, i + 1);
        var customer  = CreateCustomer(rng, i + 1);
        var order     = CreateOrder(rng, i + 1, customer.CustomerId);
        var orderItem = CreateOrderItem(rng, i + 1, order.OrderId, product.ProductId, product.Price);

        // Info — ProductModel
        Log.Information(
            "Product catalogued: ProductId={ProductId} ProductName={ProductName} Category={Category} Price={Price} StockQuantity={StockQuantity} CreatedAt={CreatedAt}",
            product.ProductId,
            product.ProductName,
            product.Category,
            product.Price,
            product.StockQuantity,
            product.CreatedAt);

        // CPU operation — Fibonacci(20)
        _ = Fibonacci(20);

        // Debug — CustomerModel
        Log.Debug(
            "Customer context: CustomerId={CustomerId} FullName={FullName} Email={Email} Country={Country} IsVerified={IsVerified} RegisteredAt={RegisteredAt}",
            customer.CustomerId,
            customer.FullName,
            customer.Email,
            customer.Country,
            customer.IsVerified,
            customer.RegisteredAt);

        // String manipulation — reverse 200-char string
        _ = ReverseString(BuildRandomString(rng, chars, 200));

        // Warning — OrderModel
        Log.Warning(
            "Order status change: OrderId={OrderId} CustomerId={CustomerId} TotalAmount={TotalAmount} Status={Status} OrderedAt={OrderedAt} DeliveredAt={DeliveredAt}",
            order.OrderId,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.OrderedAt,
            order.DeliveredAt);

        // LINQ operation — 100 random ints, sort and filter
        _ = GenerateFilteredSortedIntegers(rng);

        // Error — stock mismatch with OrderItemId and ProductId
        Log.Error(
            "Stock mismatch detected: OrderItemId={OrderItemId} ProductId={ProductId} — available quantity does not satisfy order line",
            orderItem.OrderItemId,
            orderItem.ProductId);

        // External API calls + Thread.Sleep — triggered 4 times across the full run.
        // Each call blocks the thread completely until the HTTP response arrives
        // and then again for 2 seconds during Thread.Sleep.
        if (i == 999)
            CallApiAndSleep(http,
                "https://official-joke-api.appspot.com/random_joke",
                "OfficialJokeApi", i);
        else if (i == 1999)
            CallApiAndSleep(http,
                "https://v2.jokeapi.dev/joke/Any?lang=en",
                "JokeApiV2", i);
        else if (i == 2999)
            CallApiAndSleep(http,
                "https://kimiquotes.pages.dev/api/quote",
                "KimiQuotesApi", i);
        else if (i == 3999)
            CallApiAndSleep(http,
                "https://official-joke-api.appspot.com/random_joke",
                "OfficialJokeApi", i);
    }
}

// ── Warm-up (50 iterations, untimed) ─────────────────────────────────────────
Console.WriteLine("[SYNC] Running warm-up (50 iterations)...");
RunIterations(httpClient, random, Chars, WarmUpIterations);
Console.WriteLine("[SYNC] Warm-up complete.\n");

// ── Timed section (5000 iterations) ──────────────────────────────────────────
var syncStart = Stopwatch.GetTimestamp();

RunIterations(httpClient, random, Chars, MainIterations);

// Post-loop — stock reconciliation using SupplierModel + InventoryModel
var supplierNames = new[] { "Acme Corp", "GlobalTrade Ltd", "FastSupply Inc", "PrimeSource Co" };
var warehouses    = new[] { "WH-NORTH-01", "WH-SOUTH-02", "WH-EAST-03", "WH-WEST-04" };
var supplier = new SupplierModel
{
    SupplierId    = 1001,
    SupplierName  = supplierNames[random.Next(supplierNames.Length)],
    ContactEmail  = "supplier@example.com",
    Country       = "US",
    Rating        = Math.Round(random.NextDouble() * 2 + 3, 1)
};
var inventory = new InventoryModel
{
    InventoryId       = 2001,
    ProductId         = random.Next(1, 5001),
    WarehouseLocation = warehouses[random.Next(warehouses.Length)],
    QuantityOnHand    = random.Next(0, 1000),
    LastUpdated       = DateTime.UtcNow
};
Log.Information(
    "Stock reconciliation completed: SupplierId={SupplierId} SupplierName={SupplierName} SupplierCountry={SupplierCountry} SupplierRating={SupplierRating} InventoryId={InventoryId} ProductId={ProductId} WarehouseLocation={WarehouseLocation} QuantityOnHand={QuantityOnHand} LastUpdated={LastUpdated}",
    supplier.SupplierId,
    supplier.SupplierName,
    supplier.Country,
    supplier.Rating,
    inventory.InventoryId,
    inventory.ProductId,
    inventory.WarehouseLocation,
    inventory.QuantityOnHand,
    inventory.LastUpdated);

var syncExecutionEnd = Stopwatch.GetTimestamp();

var syncFlushStart = Stopwatch.GetTimestamp();
Log.CloseAndFlush();
var syncFlushEnd = Stopwatch.GetTimestamp();

long executionMs =
    (long)Stopwatch.GetElapsedTime(syncStart, syncExecutionEnd)
    .TotalMilliseconds;

long flushMs =
    (long)Stopwatch.GetElapsedTime(syncFlushStart, syncFlushEnd)
    .TotalMilliseconds;

long totalMs =
    (long)Stopwatch.GetElapsedTime(syncStart, syncFlushEnd)
    .TotalMilliseconds;

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════════════════════");
Console.WriteLine($"[SYNC] Execution time (ms):                      {executionMs}");
Console.WriteLine($"[SYNC] Flush duration (ms):                      {flushMs}");
Console.WriteLine($"[SYNC] Total real time (ms):                     {totalMs}");
Console.WriteLine("══════════════════════════════════════════════════════════════════════");
