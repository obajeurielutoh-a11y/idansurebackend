using Moq;
using SubscriptionSystem.Application.Services;

using SubscriptionSystem.Application.Interfaces;
using Xunit;
using SubscriptionSystem.Domain.Entities;

public class SubscriptionServiceTests
{
    [Fact]
    public async Task PurchaseSubscriptionAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var subscriptionRepoMock = new Mock<SubscriptionSystem.Domain.Interfaces.ISubscriptionRepository>();
        var emailServiceMock = new Mock<IEmailService>();

        var service = new SubscriptionService(subscriptionRepoMock.Object, emailServiceMock.Object);
        var request = new SubscriptionPurchaseRequest
        {
            TransactionId = "txn123",
            Email = "test@example.com",
            AmountPaid = 100,
            Currency = "USD",
            PhoneNumber = "1234567890",
            Plan = SubscriptionPlan.OneMonth
        };

        // Act
        var result = await service.PurchaseSubscriptionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Subscription purchased successfully.", result.Message);
    }
}
