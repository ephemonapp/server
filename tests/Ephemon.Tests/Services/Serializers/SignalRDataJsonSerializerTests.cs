using Newtonsoft.Json;
using Ephemon.Data;
using Ephemon.Services.Serializers;

namespace Ephemon.Tests.Services.Serializers;

[TestClass]
public class SignalRDataJsonSerializerTests
{
    [TestMethod]
    public void Serialize_Should_Return_Data_Property()
    {
        // Arrange
        var data = Unique.String();
        var settings = new JsonSerializerSettings();
        var serializer = new SignalRDataJsonSerializer(settings);

        // Act
        var result = serializer.Serialize(new SignalRData(data));

        // Assert
        Assert.AreEqual($"\"{data}\"", result);
    }

    [TestMethod]
    public void Deserialize_Should_Fill_Data_Property()
    {
        // Arrange
        var data = Unique.String();
        var settings = new JsonSerializerSettings();
        var serializer = new SignalRDataJsonSerializer(settings);

        // Act
        var result = serializer.Deserialize($"\"{data}\"");

        // Assert
        Assert.AreEqual(data, result.Data);
    }
}