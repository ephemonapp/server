using System.Net;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Ephemon.Services.Notifications;

namespace Ephemon.Tests.Services.Notifications;

[TestClass]
public class DefaultPushServiceClientTests
{
    [TestMethod]
    public async Task RequestPushMessageDeliveryAsync_Should_Call_HttpClient_With_Correct_Endpoint_And_Method()
    {
        // Arrange
        var stub = new StubHandler();
        var httpClient = new HttpClient(stub);
        var thirdParty = new PushServiceClient(httpClient)
        {
            DefaultAuthentication = new VapidAuthentication(
                "BC39Y1fvYKc8y_zRWt0Kfys-aLQk2wavzctMKRew_xto3nB_VwG971cuSvNAkyKqr35df7rl1n4QHDBf3GkbKto",
                "fuDS3du1pBcCyYUgYRafFSTQpXQwSPRzo_HQsIWaS3k")
            {
                Subject = "mailto:test@example.com"
            },
            DefaultAuthenticationScheme = VapidAuthenticationScheme.Vapid
        };
        var adapter = new DefaultPushServiceClient(thirdParty);

        var endpoint = $"{Unique.Url()}/";
        var p256Dh = "BLAU9NcJooySjFiQ04d0IHYY/7RGzyIbBHcMHpyuIjxxiF5vEFFxOYwN4HT1UFEyaBDetBvPgBTKfW2AmBeOww8=";
        var auth = "BnvickCQztv/1QA/svR5AA==";
        var subscription = new PushSubscription
        {
            Endpoint = endpoint,
            Keys = new Dictionary<string, string>
            {
                ["p256dh"] = p256Dh,
                ["auth"] = auth
            }
        };
        var content = Unique.String();
        var message = new PushMessage(content);

        // Act
        await adapter.RequestPushMessageDeliveryAsync(subscription, message, CancellationToken.None);

        // Assert
        var request = stub.LastRequest;
        Assert.IsNotNull(request, "No HTTP request was sent through HttpClient.");
        Assert.AreEqual(HttpMethod.Post, request.Method, "Expected POST method.");
        Assert.AreEqual(endpoint, request.RequestUri!.ToString(), "Request URI did not match subscription endpoint.");
    }

    private class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));
        }
    }
}