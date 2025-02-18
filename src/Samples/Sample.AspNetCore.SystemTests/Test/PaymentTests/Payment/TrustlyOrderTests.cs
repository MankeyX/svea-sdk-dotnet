﻿using Atata;
using NUnit.Framework;
using Sample.AspNetCore.SystemTests.Services;
using Sample.AspNetCore.SystemTests.Test.Base;
using Sample.AspNetCore.SystemTests.Test.Helpers;
using Svea.WebPay.SDK.PaymentAdminApi;
using System;
using System.Linq;

namespace Sample.AspNetCore.SystemTests.Test.PaymentTests.Payment
{
    public class TrustlyOrderTests : Base.PaymentTests
    {
        public TrustlyOrderTests(string driverAlias)
            : base(driverAlias)
        {
        }

        [RetryWithException(2)]
        [Test(Description = "4781: Köp som privatperson i anonyma flödet(Trustly) -> kreditera transaktion")]
        [TestCaseSource(nameof(TestData), new object[] { true, false, false, false })]
        public void CreateOrderWithTrustlyAsPrivateAnonymousAsync(Product[] products)
        {
            Assert.DoesNotThrowAsync(async () => 
            {
                GoToOrdersPage(products, Checkout.Option.Anonymous, Entity.Option.Private, PaymentMethods.Option.Trustly)

                .RefreshPageUntil(x => x.PageUri.Value.AbsoluteUri.Contains("Orders/Details") && 
                    x.Details.Exists(new SearchOptions { IsSafely = true, Timeout = TimeSpan.FromSeconds(1) }) && 
                    x.Orders.Count() > 0, 15, 3)

                .Orders.Last().Order.OrderId.StoreValue(out var orderId)

                // Validate order info
                .RefreshPageUntil(x => x.Orders.Last().Order.OrderStatus.Value == nameof(OrderStatus.Delivered), 10, 3)
                .Orders.Last().Order.OrderStatus.Should.Equal(nameof(OrderStatus.Delivered))
                .Orders.Last().Order.PaymentType.Should.Equal(nameof(PaymentType.DirectBank))

                // Validate order rows info
                .Orders.Last().OrderRows.Should.HaveCount(0)

                // Validate deliveries info
                .Orders.Last().Deliveries.Count.Should.Equal(1)
                .Orders.Last().Deliveries.First().Status.Should.BeNull();

            // Assert sdk/api response
            var response = await _sveaClientSweden.PaymentAdmin.GetOrder(long.Parse(orderId)).ConfigureAwait(false);

                Assert.That(response.Currency, Is.EqualTo("SEK"));
                Assert.That(response.IsCompany, Is.False);
                Assert.That(response.EmailAddress.ToString(), Is.EqualTo("aaa@bbb.ccc"));
                Assert.That(response.OrderAmount.InLowestMonetaryUnit, Is.EqualTo(_amount * 100));
                Assert.That(response.PaymentType.ToString(), Is.EqualTo(nameof(PaymentType.DirectBank)));
                Assert.That(response.OrderStatus.ToString(), Is.EqualTo(nameof(OrderStatus.Delivered)));

                Assert.That(response.AvailableActions, Is.Empty);
                Assert.That(response.OrderRows, Is.Null);

                Assert.That(response.Deliveries.Count, Is.EqualTo(1));
                Assert.That(response.Deliveries.First().DeliveryAmount.InLowestMonetaryUnit, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice) * 100));
                Assert.That(response.Deliveries.First().CreditedAmount.InLowestMonetaryUnit, Is.EqualTo(0));
                Assert.That(response.Deliveries.First().Status, Is.Null);
                CollectionAssert.AreEquivalent(
                    new string[] { DeliveryActionType.CanCreditAmount },
                    response.Deliveries.First().AvailableActions
                );
            });
        }

        [RetryWithException(2)]
        [Test(Description = "4781: Köp som privatperson i anonyma flödet(Trustly) -> kreditera transaktion")]
        [TestCaseSource(nameof(TestData), new object[] { true, false, false, false })]
        public void CreditWithTrustlyAsPrivateAnonymousAsync(Product[] products)
        {
            Assert.DoesNotThrowAsync(async () => 
            {
                GoToOrdersPage(products, Checkout.Option.Anonymous, Entity.Option.Private, PaymentMethods.Option.Trustly)

                .RefreshPageUntil(x =>
                    x.PageUri.Value.AbsoluteUri.Contains("Orders/Details") &&
                    x.Details.Exists(new SearchOptions { IsSafely = true, Timeout = TimeSpan.FromSeconds(1) }) &&
                    x.Orders.Count() > 0, 15, 3)

                // Credit
                .Orders.Last().Order.OrderId.StoreValue(out var orderId)
                .Orders.Last().Deliveries.First().Table.Toggle.Click()
                .Orders.Last().Deliveries.First().Table.CreditAmount.ClickAndGo()

                // Validate order info
                .RefreshPageUntil(x => x.Orders.Last().Order.OrderStatus.Value == nameof(OrderStatus.Delivered), 10, 3)
                .Orders.Last().Order.OrderStatus.Should.Equal(nameof(OrderStatus.Delivered))
                .Orders.Last().Order.PaymentType.Should.Equal(nameof(PaymentType.DirectBank))

                // Validate order rows info
                .Orders.Last().OrderRows.Should.HaveCount(0)

                // Validate deliveries info
                .Orders.Last().Deliveries.Count.Should.Equal(1)
                .Orders.Last().Deliveries.First().Status.Should.BeNull();

            // Assert sdk/api response
            var response = await _sveaClientSweden.PaymentAdmin.GetOrder(long.Parse(orderId)).ConfigureAwait(false);

                Assert.That(response.Currency, Is.EqualTo("SEK"));
                Assert.That(response.IsCompany, Is.False);
                Assert.That(response.EmailAddress.ToString(), Is.EqualTo("aaa@bbb.ccc"));
                Assert.That(response.OrderAmount.InLowestMonetaryUnit, Is.EqualTo(_amount * 100));
                Assert.That(response.PaymentType.ToString(), Is.EqualTo(nameof(PaymentType.DirectBank)));
                Assert.That(response.OrderStatus.ToString(), Is.EqualTo(nameof(OrderStatus.Delivered)));

                Assert.That(response.AvailableActions, Is.Empty);
                Assert.That(response.OrderRows, Is.Null);

                Assert.That(response.Deliveries.Count, Is.EqualTo(1));
                Assert.That(response.Deliveries.First().DeliveryAmount.InLowestMonetaryUnit, Is.EqualTo(products.Sum(x => x.Quantity * x.UnitPrice) * 100));
                Assert.That(response.Deliveries.First().CreditedAmount.InLowestMonetaryUnit, Is.EqualTo(0));
                Assert.That(response.Deliveries.First().Status, Is.Null);
                Assert.That(response.Deliveries.First().AvailableActions, Is.Empty);
            });
        }
    }
}
