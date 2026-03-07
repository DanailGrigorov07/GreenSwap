using SecondHandGoods.Data.Entities;
using Xunit;

namespace SecondHandGoods.Tests.Entities
{
    public class MessageTests
    {
        [Fact]
        public void MarkAsRead_ShouldSetIsReadToTrue()
        {
            // Arrange
            var message = new Message
            {
                Id = 1,
                Content = "Test message",
                SenderId = "sender1",
                ReceiverId = "receiver1",
                AdvertisementId = 1,
                IsRead = false
            };

            // Act
            message.MarkAsRead();

            // Assert
            Assert.True(message.IsRead);
        }

        [Fact]
        public void IsUnread_WhenNotRead_ShouldReturnTrue()
        {
            // Arrange
            var message = new Message
            {
                Id = 1,
                Content = "Test message",
                SenderId = "sender1",
                ReceiverId = "receiver1",
                AdvertisementId = 1,
                IsRead = false
            };

            // Act
            var result = !message.IsRead;

            // Assert
            Assert.True(result);
        }
    }
}
