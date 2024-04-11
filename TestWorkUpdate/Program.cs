using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

List<int> orders = new List<int>();
List<OrderData> ordersData = new List<OrderData>();

ReadingLine();
QueryDB(orders);
SortAndPrint(ordersData);

void ReadingLine()
{
    Console.WriteLine("Введите номера заказов через запятую:");
    string readLine = Console.ReadLine();
    string[] numbersOrders = readLine.Split(',');

    foreach (string num in numbersOrders)
    {
        if (int.TryParse(num, out int orderNum))
        {
            orders.Add(orderNum);
        }
        else
        {
            Console.WriteLine("Некорректный ввод.");
            ReadingLine();
        }
    }
}

void QueryDB(List<int> orders)
{
    using (var dbContext = new YourDbContext())
    {
        foreach (int orderNum in orders)
        {
            var order = dbContext.Orders.First(o => o.OrderNumber == orderNum);

            var orderData = new OrderData
            {
                Order = order,
                GoodsInOrderDataList = new List<GoodsInOrderData>()
            };

            var goodsInOrder = dbContext.GoodsInOrders.Where(gio => gio.OrderId == order.Id).ToList();

            foreach (var gio in goodsInOrder)
            {
                var goods = dbContext.Goods.First(g => g.Id == gio.GoodsId);
                var racksWithGoods = dbContext.RacksWithGoods.Where(rwg => rwg.GoodsId == gio.GoodsId).ToList();

                var goodsInOrderData = new GoodsInOrderData
                {
                    GoodsInOrder = gio,
                    Goods = goods,
                    RacksWithGoodsDataList = new List<RackWithGoodsData>()
                };

                foreach (var rwg in racksWithGoods)
                {
                    var rack = dbContext.Racks.First(r => r.Id == rwg.RackId);

                    var rackWithGoodsData = new RackWithGoodsData
                    {
                        RacksWithGoods = rwg,
                        Rack = rack
                    };
                    goodsInOrderData.RacksWithGoodsDataList.Add(rackWithGoodsData);
                }
                orderData.GoodsInOrderDataList.Add(goodsInOrderData);
            }
            ordersData.Add(orderData);
        }
    }
}

void SortAndPrint(List<OrderData> ordersData)
{
    var sortedData = ordersData
        .SelectMany(orderData => orderData.GoodsInOrderDataList.SelectMany(goodsInOrderData =>
            goodsInOrderData.RacksWithGoodsDataList.Select(rackWithGoodsData =>
                new
                {
                    RackName = rackWithGoodsData.Rack.Name,
                    RackId = rackWithGoodsData.Rack.Id,
                    GoodsName = goodsInOrderData.Goods.Name,
                    GoodsId = goodsInOrderData.Goods.Id,
                    GoodsQuantity = goodsInOrderData.GoodsInOrder.Quantity,
                    OrderNumber = orderData.Order.OrderNumber,
                    IsMainRack = rackWithGoodsData.RacksWithGoods.IsMainRack
                })))
        .Where(data => data.IsMainRack)
        .OrderBy(data => data.RackName[0])
        .ThenBy(data => data.OrderNumber)
        .ThenBy(data => data.GoodsId);

    string currentRack = "";

    foreach (var data in sortedData)
    {
        if (data.RackName != currentRack)
        {
            currentRack = data.RackName;
            Console.WriteLine($"===Стеллаж {currentRack}");
        }
        Console.WriteLine($"{data.GoodsName} (id={data.GoodsId})");
        Console.WriteLine($"Заказ {data.OrderNumber}, {data.GoodsQuantity} шт");

        var additionalRacks = string.Join(",", ordersData
            .SelectMany(orderData => orderData.GoodsInOrderDataList.Where(goodsData => goodsData.Goods.Id == data.GoodsId)
            .SelectMany(goodsData => goodsData.RacksWithGoodsDataList
            .Where(rackData => rackData.RacksWithGoods.RackId != data.RackId)
            .Select(rackData => rackData.Rack.Name))));

        if (!string.IsNullOrEmpty(additionalRacks))
        {
            Console.WriteLine($"доп стеллажи: {additionalRacks}");
        }
        Console.WriteLine();
    }
}
public class YourDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Rack> Racks { get; set; }
    public DbSet<Goods> Goods { get; set; }
    public DbSet<RacksWithGoods> RacksWithGoods { get; set; }
    public DbSet<GoodsInOrder> GoodsInOrders { get; set; }


    //private static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=0639;Database=TestDB;");
            //.UseLoggerFactory(MyLoggerFactory)
            //.EnableSensitiveDataLogging();
    }
}

public class OrderData
{
    public Order Order { get; set; }
    public List<GoodsInOrderData> GoodsInOrderDataList { get; set; }
}

public class GoodsInOrderData
{
    public GoodsInOrder GoodsInOrder { get; set; }
    public Goods Goods { get; set; }
    public List<RackWithGoodsData> RacksWithGoodsDataList { get; set; }
}

public class RackWithGoodsData
{
    public RacksWithGoods RacksWithGoods { get; set; }
    public Rack Rack { get; set; }
}
public class Order
{
    public int Id { get; set; }
    public int OrderNumber { get; set; }
}

public class Rack
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Goods
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class RacksWithGoods
{
    public int Id { get; set; }
    public int RackId { get; set; }
    public int GoodsId { get; set; }
    public bool IsMainRack { get; set; }
}

public class GoodsInOrder
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int GoodsId { get; set; }
    public int Quantity { get; set; }
}