﻿using Atata;
using NUnit.Framework;
using Sample.AspNetCore.SystemTests.Services;
using Sample.AspNetCore.SystemTests.Test.Helpers;
using Svea.WebPay.SDK.PaymentAdminApi;
using System.Linq;
namespace Sample.AspNetCore.SystemTests.Test.PaymentTests.Payment
{
    public class TrustlyOrderTests : Base.PaymentTests
    {
        public TrustlyOrderTests(string driverAlias)
            : base(driverAlias)
        {
        }
        
        [Test(Description = "4781: Köp som privatperson i anonyma flödet(Trustly) -> kreditera transaktion")]
        [TestCaseSource(nameof(TestData), new object[] { true })]
        public async System.Threading.Tasks.Task CreateOrderWithTrustlyAsPrivateAnonymousAsync(Product[] products)
        {
            GoToOrdersPage(products, Checkout.Option.Anonymous, Entity.Option.Private, PaymentMethods.Option.Trustly)
                .Orders.Last().Order.OrderId.StoreValue(out var orderId)

                // Validate order info
                .Orders.Last().Order.OrderStatus.Should.Equal(nameof(OrderStatus.Delivered))
                .Orders.Last().Order.PaymentType.Should.Equal(nameof(PaymentType.DirectBank))

                // Validate order rows info
                .Orders.Last().OrderRows.Should.HaveCount(0)

                // Validate deliveries info
                .Orders.Last().Deliveries.Count.Should.Equal(1)
                .Orders.Last().Deliveries.First().Status.Should.BeNull();

            // Assert sdk/api response
            var response = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(orderId));

            Assert.That(response.Currency, Is.EqualTo("SEK"));
            Assert.That(response.IsCompany, Is.False);
            Assert.That(response.EmailAddress.ToString(), Is.EqualTo(TestDataService.Email));
            Assert.That(response.OrderAmount.Value, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice) * 100));
            Assert.That(response.PaymentType.ToString(), Is.EqualTo(nameof(PaymentType.DirectBank)));
            Assert.That(response.OrderStatus.ToString(), Is.EqualTo(nameof(OrderStatus.Delivered)));

            Assert.That(response.AvailableActions, Is.Empty);
            Assert.That(response.OrderRows, Is.Null);

            Assert.That(response.Deliveries.Count, Is.EqualTo(1));
            Assert.That(response.Deliveries.First().DeliveryAmount, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice) * 100));
            Assert.That(response.Deliveries.First().CreditedAmount, Is.EqualTo(0));
            Assert.That(response.Deliveries.First().Status, Is.Null);
            CollectionAssert.AreEquivalent(
                new string[] { DeliveryActionType.CanCreditAmount },
                response.Deliveries.First().AvailableActions
            );
        }

        [Test(Description = "4781: Köp som privatperson i anonyma flödet(Trustly) -> kreditera transaktion")]
        [TestCaseSource(nameof(TestData), new object[] { true })]
        public async System.Threading.Tasks.Task CreditWithTrustlyAsPrivateAnonymousAsync(Product[] products)
        {
            GoToOrdersPage(products, Checkout.Option.Anonymous, Entity.Option.Private, PaymentMethods.Option.Trustly)

                // Credit
                .Orders.Last().Order.OrderId.StoreValue(out var orderId)
                .Orders.Last().Deliveries.First().Table.Toggle.Click()
                .Orders.Last().Deliveries.First().Table.CreditAmount.ClickAndGo()

                // Validate order info
                .Orders.Last().Order.OrderStatus.Should.Equal(nameof(OrderStatus.Delivered))
                .Orders.Last().Order.PaymentType.Should.Equal(nameof(PaymentType.DirectBank))

                // Validate order rows info
                .Orders.Last().OrderRows.Should.HaveCount(0)

                // Validate deliveries info
                .Orders.Last().Deliveries.Count.Should.Equal(1)
                .Orders.Last().Deliveries.First().Status.Should.BeNull();

            // Assert sdk/api response
            var response = await _sveaClient.PaymentAdmin.GetOrder(long.Parse(orderId));

            Assert.That(response.Currency, Is.EqualTo("SEK"));
            Assert.That(response.IsCompany, Is.False);
            Assert.That(response.EmailAddress.ToString(), Is.EqualTo(TestDataService.Email));
            Assert.That(response.OrderAmount.Value, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice) * 100));
            Assert.That(response.PaymentType.ToString(), Is.EqualTo(nameof(PaymentType.DirectBank)));
            Assert.That(response.OrderStatus.ToString(), Is.EqualTo(nameof(OrderStatus.Delivered)));

            Assert.That(response.AvailableActions, Is.Empty);
            Assert.That(response.OrderRows, Is.Null);

            Assert.That(response.Deliveries.Count, Is.EqualTo(1));
            Assert.That(response.Deliveries.First().DeliveryAmount, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice) * 100));
            Assert.That(response.Deliveries.First().CreditedAmount, Is.EqualTo(0));
            Assert.That(response.Deliveries.First().Status, Is.Null);
            Assert.That(response.Deliveries.First().AvailableActions, Is.Empty);
        }

    }
}
