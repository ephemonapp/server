using System.Net;
using Lib.Net.Http.WebPush;
using Moq;
using Ephemon.Data;
using Ephemon.Services.Notifications;
using Ephemon.Services.Serializers.Infrastructure;

namespace Ephemon.Tests.Services.Notifications;

[TestClass]
public class NotificationProviderTests
{
    [TestMethod]
    public async Task TrySendAsync_Should_Send_Push_Notification()
    {
        // Arrange
        var title = Unique.String();
        var body = Unique.String();
        var data = new Dictionary<string, string>
        {
            [Unique.String()] = Unique.String(),
            [Unique.String()] = Unique.String()
        };
        var notification = new Notification(title, body, data);
        var endpoint = Unique.Url();
        var p256Dh = Unique.String();
        var auth = Unique.String();
        var subscription = new Subscription
        {
            Endpoint = endpoint,
            Keys = new SubscriptionKeys
            {
                P256Dh = p256Dh,
                Auth = auth
            }
        };
        var cancellationToken = CancellationToken.None;
        var content = Unique.String();

        var jsonSerializerMock = new Mock<IJsonSerializer<INotification>>(MockBehavior.Strict);
        jsonSerializerMock
            .Setup(serializer => serializer
                .Serialize(It.Is<INotification>(x =>
                    x.Title == title &&
                    x.Body == body &&
                    x.Data == data)))
            .Returns(content)
            .Verifiable(Times.Once);

        var pushServiceClientMock = new Mock<IPushServiceClient>(MockBehavior.Strict);
        pushServiceClientMock
            .Setup(pushServiceClient => pushServiceClient
                .RequestPushMessageDeliveryAsync(
                    It.Is<PushSubscription>(pushSubscription =>
                        pushSubscription.Endpoint == endpoint &&
                        pushSubscription.Keys["p256dh"] == p256Dh &&
                        pushSubscription.Keys["auth"] == auth),
                    It.Is<PushMessage>(pushMessage =>
                        pushMessage.Content == content &&
                        pushMessage.Urgency == PushMessageUrgency.High),
                    cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

        var notificationProvider = new NotificationProvider(
            jsonSerializerMock.Object,
            pushServiceClientMock.Object);

        // Act
        var status = await notificationProvider.TrySendAsync(
            subscription,
            notification,
            cancellationToken);

        // Assert
        Assert.AreEqual(PushDeliveryStatus.Delivered, status);
        jsonSerializerMock.VerifyAll();
        jsonSerializerMock.VerifyNoOtherCalls();
        pushServiceClientMock.VerifyAll();
        pushServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.Gone)]
    public async Task TrySendAsync_Should_Report_Expired_When_Push_Service_Forgot_The_Subscription(
        HttpStatusCode statusCode)
    {
        // Arrange
        var status = await SendAndCatch(new PushServiceClientException(Unique.String(), statusCode));

        // Assert
        Assert.AreEqual(PushDeliveryStatus.Expired, status);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.Unauthorized)]
    public async Task TrySendAsync_Should_Report_Failed_When_Push_Service_Refused_For_Another_Reason(
        HttpStatusCode statusCode)
    {
        // Arrange
        var status = await SendAndCatch(new PushServiceClientException(Unique.String(), statusCode));

        // Assert
        Assert.AreEqual(PushDeliveryStatus.Failed, status);
    }

    [TestMethod]
    public async Task TrySendAsync_Should_Report_Failed_When_Push_Service_Cannot_Be_Reached()
    {
        // Arrange
        var status = await SendAndCatch(new HttpRequestException(Unique.String()));

        // Assert
        Assert.AreEqual(PushDeliveryStatus.Failed, status);
    }

    private static async Task<PushDeliveryStatus> SendAndCatch(Exception thrown)
    {
        var subscription = new Subscription
        {
            Endpoint = Unique.Url(),
            Keys = new SubscriptionKeys
            {
                P256Dh = Unique.String(),
                Auth = Unique.String()
            }
        };

        var jsonSerializerMock = new Mock<IJsonSerializer<INotification>>(MockBehavior.Strict);
        jsonSerializerMock
            .Setup(serializer => serializer.Serialize(It.IsAny<INotification>()))
            .Returns(Unique.String());

        var pushServiceClientMock = new Mock<IPushServiceClient>(MockBehavior.Strict);
        pushServiceClientMock
            .Setup(pushServiceClient => pushServiceClient
                .RequestPushMessageDeliveryAsync(
                    It.IsAny<PushSubscription>(),
                    It.IsAny<PushMessage>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(thrown);

        var notificationProvider = new NotificationProvider(
            jsonSerializerMock.Object,
            pushServiceClientMock.Object);

        return await notificationProvider.TrySendAsync(
            subscription,
            new Notification(Unique.String(), Unique.String(), []),
            CancellationToken.None);
    }

    private record Notification(string Title,
        string Body,
        Dictionary<string, string> Data) : INotification;
}