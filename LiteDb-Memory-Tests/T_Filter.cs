using System.Linq.Expressions;
using LiteDb_Memory_Lib;
using LiteDB;

namespace LiteDb_Memory_Tests;


#region Data-Test FindOne with reference
public struct MarketOrder(ObjectId orderId, double bidPrice, double askPrice, string underlying, DateTime date, 
    TraderEntity trader, string currency)
{
    [BsonId]
    public ObjectId OrderId { get; set; } = orderId;
    public string Underlying { get; set; } = underlying;
    public double BidPrice { get; set; } =  bidPrice;
    public double AskPrice { get; set; } =  askPrice;
    public DateTime OrderTime { get; set; } = date;
    
    public string Currency { get; set; } = currency;
    
    [BsonRef("TRADERS")]
    public TraderEntity Trader { get; set; } = trader;
}
public struct TraderEntity(ObjectId traderId,  string entity, string tiering)
{
    [BsonId]
    public ObjectId TraderId { get; set; } = traderId;
    public string Entity { get; set; } = entity;
    public string Tiering { get; set; } = tiering;
}

#endregion

public class FindDocuments
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void T_Find_One_With_Dependencies()
    {
        const string idDataBase = "TEST_MARKET_DATA";
        var manager = ConnectionManager.Instance();
        
        // Create database
        manager.CreateDatabase(idDataBase);
        
        // Create Trader collection
        var traders = new List<TraderEntity>()
        {
            new()
            {
                TraderId = ObjectId.NewObjectId(),
                Entity = "CITI",
                Tiering = "TIER-1",
            },
            
            new()
            {
                TraderId = ObjectId.NewObjectId(),
                Entity = "JP",
                Tiering = "TIER-2",
            }
        };
        
        
        // Create list MarketOder 
        var marketOrders = new List<MarketOrder>()
        {
            new ()
            {
                OrderId = ObjectId.NewObjectId(), BidPrice = 4400.0, AskPrice = 4500.0,  Underlying = "STOXX50E", 
                OrderTime = DateTime.UtcNow, Trader = traders[0], Currency = "EUR"
            },

            new ()
            {
                OrderId = ObjectId.NewObjectId(), BidPrice = 4500.0, AskPrice = 4600.0, Underlying = "STOXX-50E",
                OrderTime = DateTime.UtcNow.AddMilliseconds(100), Trader = traders[0],  Currency = "USD"
            },

            new ()
            {
                OrderId = ObjectId.NewObjectId(), BidPrice = 14400.0, AskPrice = 14500.0, Underlying = "SP500",
                OrderTime = DateTime.UtcNow.AddMilliseconds(200), Trader = traders[0],  Currency = "EUR"
            },
            
            new ()
            {
                OrderId = ObjectId.NewObjectId(), BidPrice = 6.0, AskPrice = 6.1,  Underlying = "IBERDROLA",
                OrderTime = DateTime.UtcNow.AddMilliseconds(300), Trader = traders[1],   Currency = "GBP"
            },

            new ()
            {
                OrderId = ObjectId.NewObjectId(), BidPrice = 6.3, AskPrice = 6.4, Underlying = "IBERDROLA",
                OrderTime = DateTime.UtcNow.AddMilliseconds(400), Trader = traders[1],   Currency = "USD"
            },

            new ()
            {
                OrderId = ObjectId.NewObjectId(), BidPrice = 14.1, AskPrice = 14.2, Underlying = "BBVA",
                OrderTime = DateTime.UtcNow.AddMilliseconds(500), Trader = traders[1]
            },

        };
        
        // Create collections
        manager.CreateCollection(idDataBase, "TRADERS", traders);
        manager.CreateCollection(idDataBase, "MARKET_ORDERS", marketOrders);
        
        // Filter
        Expression<Func<MarketOrder, bool>> filter = x => x.Currency == "EUR";
        Expression<Func<MarketOrder, TraderEntity>> include = x => x.Trader;
        
        var output = FilterTools.FindOne(manager, idDataBase, "MARKET_ORDERS", 
            filter, include);
        
        Assert.That(output.Trader.Entity, Is.EqualTo("CITI"));
    }

    [Test]
    public void T_Find_One()
    {
        const string idDataBase = "TEST_DB";
        const string idCollection = "CUSTOMERS";
        var manager = ConnectionManager.Instance();
        
        // Create database
        manager.CreateDatabase(idDataBase);

        // Create list customers 
        var customers = new List<Customer>()
        {
            new()
            {
                CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(2000, 1, 1),
                IsActive = true, Name = "David", Phones = [new Phone(34, 658566844)]
            },

            new()
            {
                CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(2004, 3, 27),
                IsActive = true, Name = "Martin", Phones = [new Phone(34, 658576842)]
            },

            new()
            {
                CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(2004, 3, 27),
                IsActive = true, Name = "Nerea", Phones = [new Phone(34, 645576842)]
            },
        };
        
        // Create customer collection
        manager.CreateCollection(idDataBase, idCollection, customers);
        
        // Create order
        var order1 = new Order{OrderId = ObjectId.NewObjectId(), Customer = customers[0]};
        var order2 = new Order{OrderId = ObjectId.NewObjectId(), Customer = customers[1]};
        var orders = new List<Order>{order1, order2};
        
        // Insert order with associated customer is not in CUSTOMER collection.
        var ghostCustomer = new Customer()
        {
            CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(1980, 3, 15),
            IsActive = true, Name = "Ghost", Phones = [new Phone(34, 666666666)]
        };
        
        orders.Add(new Order{OrderId = ObjectId.NewObjectId(), Customer = ghostCustomer});
        
        // Create order collection
        manager.CreateCollection(idDataBase, "ORDERS", orders);
        
        
        // Filter with predicate
        Expression<Func<Customer, bool>> filter = x => x.Name == "David";
        var resultWithPredicate = FilterTools.FindOne(manager, idDataBase, idCollection, filter);
        Assert.That(resultWithPredicate.Name, Is.EqualTo("David"));
        
        // Filter with query
        var query = Query.EQ("name", "David");
        var resultWithQry = FilterTools.FindOne<Customer>(manager, idDataBase, idCollection, query);
        Assert.That(resultWithQry.Name, Is.EqualTo("David"));
        
        // Filter with query
        var bsonExpression = BsonExpression.Create("name='david'");
        var resultWithBsonDocument = FilterTools.FindOne<Customer>(manager, idDataBase, idCollection, bsonExpression);
        Assert.That(resultWithBsonDocument.Name, Is.EqualTo("David"));
        
    }
    
}