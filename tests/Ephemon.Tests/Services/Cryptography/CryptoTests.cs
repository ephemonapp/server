using System.Text;
using Ephemon.Services.Cryptography;

namespace Ephemon.Tests.Services.Cryptography;

[TestClass]
public class CryptoTests
{
    [TestMethod]
    public void VerifySignature_Should_Return_True_When_Signature_Is_Valid()
    {
        // Arrange
        var publicKeyBase64 = "HZgxXdQVCnhPWq03UXxvPWOOfXIPcoNM4dxs6mxIGTc=";
        var dataString = "{\"a\":\"HZgxXdQVCnhPWq03UXxvPWOOfXIPcoNM4dxs6mxIGTc=\"}";
        var signatureBase64 = "bmV8RhD62LaoKh7m7UlODwl037zb5Gt33+XyQQHBsN+UXXGYZtl0xlg24wRtF2M+ilruSnqyK8otnQ0dBLDkAg==";
        var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        var dataBytes = Encoding.UTF8.GetBytes(dataString);
        var signatureBytes = Convert.FromBase64String(signatureBase64);
        var crypto = new Crypto();

        // Act
        var result = crypto.VerifySignature(publicKeyBytes, dataBytes, signatureBytes);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifySignature_Should_Return_False_When_Signature_Is_Invalid()
    {
        // Arrange
        const string base64SafeChoices = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        var publicKeyBase64 = "HZgxXdQVCnhPWq03UXxvPWOOfXIPcoNM4dxs6mxIGTc=";
        var dataString = "{\"a\":\"HZgxXdQVCnhPWq03UXxvPWOOfXIPcoNM4dxs6mxIGTc=\"}";
        var signatureBase64 = $"{Unique.String(86, base64SafeChoices)}==";
        var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        var dataBytes = Encoding.UTF8.GetBytes(dataString);
        var signatureBytes = Convert.FromBase64String(signatureBase64);
        var crypto = new Crypto();

        // Act
        var result = crypto.VerifySignature(publicKeyBytes, dataBytes, signatureBytes);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySignature_Should_Return_False_When_Exception_Thrown()
    {
        // Arrange
        var dataString = Unique.String();
        var dataBytes = Encoding.UTF8.GetBytes(dataString);
        var crypto = new Crypto();

        // Act
        var result = crypto.VerifySignature([], dataBytes, []);

        // Assert
        Assert.IsFalse(result);
    }
}