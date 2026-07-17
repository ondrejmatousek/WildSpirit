using DesignStore.Application.Commerce;
using DesignStore.Domain.Commerce;
using Microsoft.Extensions.Hosting;
namespace DesignStore.Infrastructure.Commerce;
public sealed class BankTransferPaymentService:IPaymentService
{public PaymentMethod Method=>PaymentMethod.BankTransfer;public Task<PaymentResult>CreateAsync(string orderNumber,decimal total,string currency,string idempotencyKey,CancellationToken ct)=>Task.FromResult(new PaymentResult(PaymentStatus.Pending,$"VS-{orderNumber}",""));}
public sealed class TestPaymentService(IHostEnvironment environment):IPaymentService
{public PaymentMethod Method=>PaymentMethod.Test;public Task<PaymentResult>CreateAsync(string orderNumber,decimal total,string currency,string idempotencyKey,CancellationToken ct){if(!environment.IsDevelopment())throw new InvalidOperationException("Testovací platba není mimo Development dostupná.");return Task.FromResult(new PaymentResult(PaymentStatus.Paid,$"TEST-{orderNumber}",$"test-{idempotencyKey}"));}}
