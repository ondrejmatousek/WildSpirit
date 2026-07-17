using DesignStore.Application.Commerce;
using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Commerce;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace DesignStore.Tests.Infrastructure;
[Collection("SqlServer database")]
public sealed class CommerceSqlTests(SqlServerDatabaseFixture database)
{
 static CommerceService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db)=>new(db,[new BankTransferPaymentService()],new AddToCartRequestValidator(),new CheckoutRequestValidator());
 static CheckoutRequest Checkout(string key)=>new(){IdempotencyKey=key,Email="buyer@example.test",BillingAddress=new AddressInput{FullName="Test Buyer",Street="Test 1",City="Brno",PostalCode="60200",CountryCode="CZ"},UseBillingAsShipping=true,ShippingMethod="pickup",PaymentMethod=PaymentMethod.BankTransfer};
 async Task<Guid> Product(int stock,bool made=false)
 {await using var db=database.CreateDb();var c=new Category{Translations=[new CategoryTranslation{Culture="cs-CZ",Name="Test",Slug=$"test-{Guid.NewGuid():N}",ShortDescription="Test"}]};var p=new Product{Category=c,Sku=$"C-{Guid.NewGuid():N}",BasePrice=1210,VatRate=21,IsPublished=true,AvailabilityStatus=AvailabilityStatus.InStock,StockQuantity=stock,CanBeMadeToOrder=made,Translations=[new ProductTranslation{Culture="cs-CZ",Name="Test produkt",Slug=$"product-{Guid.NewGuid():N}"}]};db.Products.Add(p);await db.SaveChangesAsync();return p.Id;}
 [Fact]public async Task Checkout_is_idempotent_and_cancellation_restores_stock_once()
 {var productId=await Product(2);var token=Guid.NewGuid().ToString("N");await using(var db=database.CreateDb()){var s=Service(db);await s.AddAsync(token,null,"cs-CZ",new(productId,null,1,[]),default);var first=await s.CheckoutAsync(token,null,Checkout("same-key"),default);var second=await s.CheckoutAsync(token,null,Checkout("same-key"),default);Assert.Equal(first.OrderId,second.OrderId);await s.CancelAsync(first.OrderId,"test",default);await s.CancelAsync(first.OrderId,"test",default);}await using(var verify=database.CreateDb()){Assert.Equal(2,await verify.Products.Where(x=>x.Id==productId).Select(x=>x.StockQuantity).SingleAsync());Assert.Equal(1,await verify.InventoryMovements.CountAsync(x=>x.ProductId==productId&&x.Type==InventoryMovementType.Return));}}
 [Fact]public async Task Two_checkouts_cannot_buy_the_same_last_item_and_zero_switches_to_unavailable()
 {var productId=await Product(1);var t1=Guid.NewGuid().ToString("N");var t2=Guid.NewGuid().ToString("N");await using(var prep=database.CreateDb()){var s=Service(prep);await s.AddAsync(t1,null,"cs-CZ",new(productId,null,1,[]),default);await s.AddAsync(t2,null,"cs-CZ",new(productId,null,1,[]),default);}var tasks=new[]{Run(t1,"race-1"),Run(t2,"race-2")};var results=await Task.WhenAll(tasks);Assert.Equal(1,results.Count(x=>x));await using var verify=database.CreateDb();var p=await verify.Products.SingleAsync(x=>x.Id==productId);Assert.Equal(0,p.StockQuantity);Assert.Equal(AvailabilityStatus.Unavailable,p.AvailabilityStatus);Assert.Equal(1,await verify.Orders.CountAsync(x=>x.Items.Any(i=>i.ProductId==productId)));}
 async Task<bool> Run(string token,string key){try{await using var db=database.CreateDb();await Service(db).CheckoutAsync(token,null,Checkout(key),default);return true;}catch(ValidationException){return false;}catch(DbUpdateException){return false;}}
 [Fact]public async Task Made_to_order_product_cannot_be_added_to_cart(){var id=await Product(0,true);await using var db=database.CreateDb();var p=await db.Products.SingleAsync(x=>x.Id==id);p.AvailabilityStatus=AvailabilityStatus.MadeToOrder;await db.SaveChangesAsync();await Assert.ThrowsAsync<ValidationException>(()=>Service(db).AddAsync(Guid.NewGuid().ToString("N"),null,"cs-CZ",new(id,null,1,[]),default));}
}
