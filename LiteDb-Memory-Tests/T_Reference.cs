using System.Linq.Expressions;
using LiteDb_Memory_Lib;
using LiteDB;

namespace LiteDb_Memory_Tests;

#region Data-Test FindOne
public struct Phone
{
    public int Prefix { get; set; }
    public long Number { get; set; } 
    
    public Phone(int p, int n)
    {
        Prefix = p;
        Number = n;
    }
    
    public long GetPhoneNumber()
    {
        return long.Parse($"00{Prefix}{Number}");
    }
}
public struct Customer(ObjectId customerId, string name, DateTime createdDate, List<Phone> phones, bool isActive)
{
    [BsonId]
    public ObjectId CustomerId { get; set; } = customerId;
    public string Name { get; set; } = name;
    public DateTime CreateDate { get; set; } = createdDate;
    public List<Phone> Phones { get; set; } = phones;
    public bool IsActive { get; set; } = isActive;
}

public struct Order
{
    [BsonId]
    public ObjectId OrderId { get; set; }
    
    [BsonRef("Customers")]
    public Customer Customer { get; set; }
}

#endregion

public class CrossReference
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void T_CrossQuery()
    {
        const string idDataBase = "TEST_DB";
        const string idCollection = "CUSTOMERS";
        var manager = ConnectionManager.Instance();
        
        // Create database
        manager.CreateDatabase(idDataBase);

        // Create list customers 
        var customers = new List<Customer>
        {
            new ()
            {
                CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(2000, 1, 1),
                IsActive = true, Name = "David", Phones = [new Phone(34, 658566844)]
            },

            new ()
            {
                CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(2004, 3, 27),
                IsActive = true, Name = "Martin", Phones = [new Phone(34, 658576842)]
            },

            new ()
            {
                CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(2004, 3, 27),
                IsActive = true, Name = "Nerea", Phones = [new Phone(34, 645576842)]
            },
        };
        
        // Create customer collection
        manager.CreateCollection<Customer>(idDataBase, idCollection, customers);
        
        // Create order
        var order1 = new Order(){OrderId = ObjectId.NewObjectId(), Customer = customers[0]};
        var order2 = new Order(){OrderId = ObjectId.NewObjectId(), Customer = customers[1]};
        var orders = new List<Order>(){order1, order2};
        
        // Insert order with associated customer is not in CUSTOMER collection.
        var ghostCustomer = new Customer()
        {
            CustomerId = ObjectId.NewObjectId(), CreateDate = new DateTime(1980, 3, 15),
            IsActive = true, Name = "Ghost", Phones = [new Phone(34, 666666666)]
        };
        
        orders.Add(new Order(){OrderId = ObjectId.NewObjectId(), Customer = ghostCustomer});
        
        // Create order collection
        manager.CreateCollection<Order>(idDataBase, "ORDERS", orders);
        
        // Find element
        var collection = manager.GetCollection<Order>(idDataBase, "ORDERS");
        
        if (collection != null)
        {
            Expression<Func<Order, Customer>> refFunctional = x => x.Customer;
            
            var filteredOrder = FilterTools.FindById(manager, idDataBase, "ORDERS", 
                refFunctional, order1.OrderId);
            
            Assert.That(order1.Customer.CustomerId, Is.EqualTo(filteredOrder.Customer.CustomerId));
            
            var filteredGhostOrder =  FilterTools.FindById(manager, idDataBase, "ORDERS", 
                refFunctional, orders.Last().OrderId);
            
            Assert.That(filteredGhostOrder.Customer.CustomerId is null);
        }
    }

}