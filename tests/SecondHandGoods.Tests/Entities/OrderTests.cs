using SecondHandGoods.Data.Entities;
using Xunit;

namespace SecondHandGoods.Tests.Entities
{
    public class OrderTests
    {
        [Fact]
        public void GenerateOrderNumber_ShouldCreateUniqueFormat()
        {
            // Act
            var orderNumber = Order.GenerateOrderNumber();

            // Assert
            Assert.NotNull(orderNumber);
            Assert.StartsWith("ORD-", orderNumber);
            Assert.True(orderNumber.Length > 10);
        }

        [Fact]
        public void Complete_ShouldSetStatusToCompleted()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                BuyerId = "buyer1",
                SellerId = "seller1",
                AdvertisementId = 1,
                FinalPrice = 100,
                Status = OrderStatus.Pending
            };

            // Act
            order.Complete();

            // Assert
            Assert.Equal(OrderStatus.Completed, order.Status);
        }

        [Fact]
        public void Cancel_ShouldSetStatusToCancelled()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                BuyerId = "buyer1",
                SellerId = "seller1",
                AdvertisementId = 1,
                FinalPrice = 100,
                Status = OrderStatus.Pending
            };

            // Act
            order.Cancel();

            // Assert
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }
    }
}
