using DesignStore.Application.Commerce;
using DesignStore.Domain.Catalog;
using DesignStore.Domain.Commerce;
using DesignStore.Infrastructure.Commerce;
using FluentValidation;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
namespace DesignStore.Tests.Infrastructure;
[Collection("SqlServer database")]
public sealed class CommerceSqlTests(SqlServerDatabaseFixture database)
{
 static CommerceService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db)=>new(db,[new BankTransferPaymentService()],new AddToCartRequestValidator(),new CheckoutRequestValidator());
 static CommerceService Service(DesignStore.Infrastructure.Persistence.ApplicationDbContext db,string environment)=>new(db,[new BankTransferPaymentService(),new TestPaymentService(new TestHostEnvironment{EnvironmentName=environment})],new AddToCartRequestValidator(),new CheckoutRequestValidator());
 static CheckoutRequest Checkout(string key)=>new(){IdempotencyKey=key,Email="buyer@example.test",BillingAddress=new AddressInput{FullName="Test Buyer",Street="Test 1",City="Brno",PostalCode="60200",CountryCode="CZ"},UseBillingAsShipping=true,ShippingMethod="pickup",PaymentMethod=PaymentMethod.BankTransfer};
 async Task<Guid> Product(int stock,bool made=false)
 {await using var db=database.CreateDb();var c=new Category{Translations=[new CategoryTranslation{Culture="cs-CZ",Name="Test",Slug=$"test-{Guid.NewGuid():N}",ShortDescription="Test"}]};var p=new Product{Category=c,Sku=$"C-{Guid.NewGuid():N}",BasePrice=1210,VatRate=21,IsPublished=true,AvailabilityStatus=AvailabilityStatus.InStock,StockQuantity=stock,CanBeMadeToOrder=made,Translations=[new ProductTranslation{Culture="cs-CZ",Name="Test produkt",Slug=$"product-{Guid.NewGuid():N}"}]};db.Products.Add(p);await db.SaveChangesAsync();return p.Id;}
 [Fact]public async Task Checkout_is_idempotent_and_cancellation_restores_stock_once()
 {var productId=await Product(2);var token=Guid.NewGuid().ToString("N");await using(var db=database.CreateDb()){var s=Service(db);await s.AddAsync(token,null,"cs-CZ",new(productId,null,1,[]),default);var first=await s.CheckoutAsync(token,null,Checkout("same-key"),default);var second=await s.CheckoutAsync(token,null,Checkout("same-key"),default);Assert.Equal(first.OrderId,second.OrderId);await s.CancelAsync(first.OrderId,"test",default);await s.CancelAsync(first.OrderId,"test",default);}await using(var verify=database.CreateDb()){Assert.Equal(2,await verify.Products.Where(x=>x.Id==productId).Select(x=>x.StockQuantity).SingleAsync());Assert.Equal(1,await verify.InventoryMovements.CountAsync(x=>x.ProductId==productId&&x.Type==InventoryMovementType.Return));}}
 [Fact]public async Task Two_checkouts_cannot_buy_the_same_last_item_and_zero_switches_to_unavailable()
 {var productId=await Product(1);var t1=Guid.NewGuid().ToString("N");var t2=Guid.NewGuid().ToString("N");await using(var prep=database.CreateDb()){var s=Service(prep);await s.AddAsync(t1,null,"cs-CZ",new(productId,null,1,[]),default);await s.AddAsync(t2,null,"cs-CZ",new(productId,null,1,[]),default);}var tasks=new[]{Run(t1,"race-1"),Run(t2,"race-2")};var results=await Task.WhenAll(tasks);Assert.Equal(1,results.Count(x=>x));await using var verify=database.CreateDb();var p=await verify.Products.SingleAsync(x=>x.Id==productId);Assert.Equal(0,p.StockQuantity);Assert.Equal(AvailabilityStatus.Unavailable,p.AvailabilityStatus);Assert.Equal(1,await verify.Orders.CountAsync(x=>x.Items.Any(i=>i.ProductId==productId)));}
 async Task<bool> Run(string token,string key){try{await using var db=database.CreateDb();await Service(db).CheckoutAsync(token,null,Checkout(key),default);return true;}catch(ValidationException){return false;}catch(DbUpdateException){return false;}}
 [Fact]public async Task Made_to_order_product_cannot_be_added_to_cart(){var id=await Product(0,true);await using var db=database.CreateDb();var p=await db.Products.SingleAsync(x=>x.Id==id);p.AvailabilityStatus=AvailabilityStatus.MadeToOrder;await db.SaveChangesAsync();await Assert.ThrowsAsync<ValidationException>(()=>Service(db).AddAsync(Guid.NewGuid().ToString("N"),null,"cs-CZ",new(id,null,1,[]),default));}
 [Fact]public async Task Configured_product_can_go_from_cart_to_bank_transfer_order_with_standard_shipping()
 {
  var fixture=await ConfiguredProduct(stock:3);
  var token=Guid.NewGuid().ToString("N");
  await using(var db=database.CreateDb())
  {
   var service=Service(db);
   await service.AddAsync(token,null,"cs-CZ",new AddToCartRequest(fixture.ProductId,null,2,[new CartConfigurationInput(fixture.OptionId,fixture.OptionValueId,null)]),default);
   var cart=await service.GetCartAsync(token,null,"cs-CZ",default);
   Assert.Single(cart.Items);
   Assert.Equal(2300m,cart.Subtotal);
   var checkout=Checkout($"bank-{Guid.NewGuid():N}");
   checkout.ShippingMethod="standard";
   checkout.PaymentMethod=PaymentMethod.BankTransfer;

   var result=await service.CheckoutAsync(token,null,checkout,default);

   Assert.Equal(PaymentStatus.Pending,result.PaymentStatus);
   Assert.Equal(2550m,result.Total);
  }
  await using(var verify=database.CreateDb())
  {
   var product=await verify.Products.AsNoTracking().SingleAsync(x=>x.Id==fixture.ProductId);
   var order=await verify.Orders.AsNoTracking().Include(x=>x.Items).ThenInclude(x=>x.Configuration).Include(x=>x.Addresses).Include(x=>x.Payments).Include(x=>x.Shipments).SingleAsync(x=>x.Items.Any(i=>i.ProductId==fixture.ProductId));
   var item=order.Items.Single();
   var config=item.Configuration.Single();

   Assert.Equal(1,product.StockQuantity);
   Assert.Equal(OrderStatus.PendingPayment,order.Status);
   Assert.Equal(PaymentStatus.Pending,order.PaymentStatus);
   Assert.Equal("standard",order.ShippingMethod);
   Assert.Equal(2300m,order.Subtotal);
   Assert.Equal(250m,order.ShippingTotal);
   Assert.Equal(2550m,order.Total);
   Assert.Equal("Konfigurovaná kabelka",item.ProductName);
   Assert.Equal("CFG-SKU",item.Sku);
   Assert.Equal(1150m,item.UnitPrice);
   Assert.Equal(2,item.Quantity);
   Assert.Equal("Povrch",config.OptionName);
   Assert.Equal("Mosaz",config.Value);
   Assert.Equal(150m,config.PriceAdjustment);
   Assert.Equal(2,order.Addresses.Count);
   Assert.Equal(PaymentMethod.BankTransfer,order.Payments.Single().Method);
   Assert.Equal(PaymentStatus.Pending,order.Payments.Single().Status);
   Assert.StartsWith("VS-",order.Payments.Single().ProviderReference);
   Assert.Equal("standard",order.Shipments.Single().Service);
   Assert.Equal(250m,order.Shipments.Single().Price);
   Assert.False(await verify.CartItems.AsNoTracking().AnyAsync(x=>x.ProductId==fixture.ProductId));
   Assert.Equal(1,await verify.InventoryMovements.AsNoTracking().CountAsync(x=>x.ProductId==fixture.ProductId&&x.Type==InventoryMovementType.Sale&&x.QuantityChange==-2&&x.PreviousQuantity==3&&x.NewQuantity==1));
   Assert.True(await verify.OutboxMessages.AsNoTracking().AnyAsync(x=>x.Type=="OrderConfirmation"));
  }
 }
 [Fact]public async Task Variant_product_can_checkout_with_test_payment_in_development()
 {
  var fixture=await VariantProduct(stock:2);
  var token=Guid.NewGuid().ToString("N");
  await using(var db=database.CreateDb())
  {
   var service=Service(db,Environments.Development);
   await service.AddAsync(token,null,"cs-CZ",new AddToCartRequest(fixture.ProductId,fixture.VariantId,1,[]),default);
   var checkout=Checkout($"testpay-{Guid.NewGuid():N}");
   checkout.PaymentMethod=PaymentMethod.Test;
   checkout.ShippingMethod="pickup";

   var result=await service.CheckoutAsync(token,null,checkout,default);

   Assert.Equal(PaymentStatus.Paid,result.PaymentStatus);
   Assert.Equal(1800m,result.Total);
  }
  await using(var verify=database.CreateDb())
  {
   var variant=await verify.ProductVariants.AsNoTracking().SingleAsync(x=>x.Id==fixture.VariantId);
   var order=await verify.Orders.AsNoTracking().Include(x=>x.Items).Include(x=>x.Payments).Include(x=>x.Shipments).SingleAsync(x=>x.Items.Any(i=>i.ProductVariantId==fixture.VariantId));

   Assert.Equal(1,variant.StockQuantity);
   Assert.Equal(OrderStatus.Paid,order.Status);
   Assert.Equal(PaymentStatus.Paid,order.PaymentStatus);
   Assert.Equal(0m,order.ShippingTotal);
   Assert.Equal("pickup",order.Shipments.Single().Service);
   Assert.Equal(PaymentMethod.Test,order.Payments.Single().Method);
   Assert.StartsWith("TEST-",order.Payments.Single().ProviderReference);
   Assert.Equal("Velká",order.Items.Single().VariantName);
   Assert.Equal("VAR-SKU",order.Items.Single().Sku);
  }
 }
 [Fact]public async Task Test_payment_is_blocked_outside_development()
 {
  var productId=await Product(1);
  var token=Guid.NewGuid().ToString("N");
  await using(var db=database.CreateDb())
  {
   var service=Service(db,Environments.Production);
   await service.AddAsync(token,null,"cs-CZ",new AddToCartRequest(productId,null,1,[]),default);
   var checkout=Checkout($"prod-testpay-{Guid.NewGuid():N}");
   checkout.PaymentMethod=PaymentMethod.Test;

   await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CheckoutAsync(token,null,checkout,default));
  }
  await using(var verify=database.CreateDb())
  {
   Assert.False(await verify.Orders.AsNoTracking().AnyAsync(x=>x.Items.Any(i=>i.ProductId==productId)));
   Assert.Equal(1,await verify.Products.AsNoTracking().Where(x=>x.Id==productId).Select(x=>x.StockQuantity).SingleAsync());
  }
 }
 async Task<(Guid ProductId,Guid OptionId,Guid OptionValueId)> ConfiguredProduct(int stock)
 {
  await using var db=database.CreateDb();
  var category=new Category{Translations=[new CategoryTranslation{Culture="cs-CZ",Name="Checkout config",Slug=$"checkout-config-{Guid.NewGuid():N}",ShortDescription="Test"}]};
  var optionValue=new ProductOptionValue{PriceAdjustment=150,Translations=[new ProductOptionValueTranslation{Culture="cs-CZ",Name="Mosaz"}]};
  var option=new ProductOption{Type=ProductOptionType.Select,IsRequired=true,Translations=[new ProductOptionTranslation{Culture="cs-CZ",Name="Povrch"}],Values=[optionValue]};
  var product=new Product{Category=category,Sku="CFG-SKU",BasePrice=1000,VatRate=21,IsPublished=true,AvailabilityStatus=AvailabilityStatus.InStock,StockQuantity=stock,MainImageUrl="/media/product/test",Translations=[new ProductTranslation{Culture="cs-CZ",Name="Konfigurovaná kabelka",Slug=$"konfigurovana-kabelka-{Guid.NewGuid():N}"}],Options=[option]};
  db.Products.Add(product);
  await db.SaveChangesAsync();
  return(product.Id,option.Id,optionValue.Id);
 }
 async Task<(Guid ProductId,Guid VariantId)> VariantProduct(int stock)
 {
  await using var db=database.CreateDb();
  var category=new Category{Translations=[new CategoryTranslation{Culture="cs-CZ",Name="Checkout variant",Slug=$"checkout-variant-{Guid.NewGuid():N}",ShortDescription="Test"}]};
  var variant=new ProductVariant{Sku="VAR-SKU",Name="Velká",Price=1800,AvailabilityStatus=AvailabilityStatus.InStock,StockQuantity=stock,Translations=[new ProductVariantTranslation{Culture="cs-CZ",Name="Velká"}]};
  var product=new Product{Category=category,Sku=$"VAR-P-{Guid.NewGuid():N}"[..18],BasePrice=0,VatRate=21,IsPublished=true,AvailabilityStatus=AvailabilityStatus.InStock,HasVariants=true,StockQuantity=0,Translations=[new ProductTranslation{Culture="cs-CZ",Name="Variantová kabelka",Slug=$"variantova-kabelka-{Guid.NewGuid():N}"}],Variants=[variant]};
  db.Products.Add(product);
  await db.SaveChangesAsync();
  return(product.Id,variant.Id);
 }
 private sealed class TestHostEnvironment:IHostEnvironment
 {
  public string EnvironmentName{get;set;}=Environments.Production;
  public string ApplicationName{get;set;}=nameof(CommerceSqlTests);
  public string ContentRootPath{get;set;}=AppContext.BaseDirectory;
  public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
 }
}
