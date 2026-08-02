using FluentValidation;
using FluentValidation.Results;
using Moq;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.DbDateTime;

namespace Ephemon.Tests.Services.Call;

[TestClass]
public class CallProcessorTests
{
    private Mock<ICallRequest> _callRequestMock = null!;
    private Mock<ICallRequestProcessorFactory> _callRequestProcessorFactoryMock = null!;
    private Mock<ICallRequestProcessor> _callRequestProcessorMock = null!;
    private Mock<IValidator<ICallRequest>> _callRequestValidatorMock = null!;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private Mock<IDbDateTimeProvider> _dbDateTimeProviderMock = null!;
    private string _method = null!;
    private long _timestamp;

    [TestInitialize]
    public void TestInitialize()
    {
        _timestamp = Unique.Int64();
        _method = Unique.String();
        _cancellationToken = CancellationToken.None;

        _callRequestMock = new Mock<ICallRequest>(MockBehavior.Strict);
        _callRequestMock
            .Setup(request => request.SetServerUnixTimeMilliseconds(_timestamp))
            .Verifiable(Times.Once);
        _callRequestMock
            .SetupGet(request => request.Method)
            .Returns(_method)
            .Verifiable(Times.Once);

        _callRequestValidatorMock = new Mock<IValidator<ICallRequest>>(MockBehavior.Strict);

        _callRequestProcessorMock = new Mock<ICallRequestProcessor>(MockBehavior.Strict);

        _callRequestProcessorFactoryMock = new Mock<ICallRequestProcessorFactory>(MockBehavior.Strict);
        _callRequestProcessorFactoryMock
            .Setup(factory => factory
                .GetForRequest(_callRequestMock.Object))
            .Returns(_callRequestProcessorMock.Object)
            .Verifiable(Times.Once);

        _dbDateTimeProviderMock = new Mock<IDbDateTimeProvider>(MockBehavior.Strict);
        _dbDateTimeProviderMock
            .Setup(provider => provider
                .GetServerTimeAsync())
            .ReturnsAsync(_timestamp)
            .Verifiable(Times.Once);
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Invoke_CallRequestProcessor_And_Return_CallResponse_When_CallRequest_Is_Valid_And_CallResponse_Is_Not_Null()
    {
        // Arrange
        var callResponseMock = new Mock<ICallResponse>(MockBehavior.Strict);
        SetupValidationResult(new ValidationResult
        {
            Errors = [],
            RuleSetsExecuted = []
        });
        SetupProcessAsync(callResponseMock.Object);
        var callProcessor = CreateProcessor();

        // Act
        var result = await callProcessor
            .ProcessAsync(
                _callRequestMock.Object,
                _cancellationToken);

        // Assert
        Assert.AreEqual(callResponseMock.Object, result);
        VerifyAllMocks();
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Invoke_CallRequestProcessor_And_Return_Null_When_CallRequest_Is_Valid_And_CallResponse_Is_Null()
    {
        // Arrange
        SetupValidationResult(new ValidationResult
        {
            Errors = [],
            RuleSetsExecuted = []
        });
        SetupProcessAsync(null);
        var callProcessor = CreateProcessor();

        // Act
        var result = await callProcessor
            .ProcessAsync(
                _callRequestMock.Object,
                _cancellationToken);

        // Assert
        Assert.IsNull(result);
        VerifyAllMocks();
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Not_Invoke_CallRequestProcessor_And_Return_CallResponse_With_Errors_When_CallRequest_Is_Invalid()
    {
        // Arrange
        var errorProperty = Unique.String();
        var errorMessage = Unique.String();
        SetupValidationResult(new ValidationResult
        {
            Errors =
            [
                new ValidationFailure(errorProperty, errorMessage)
            ],
            RuleSetsExecuted = []
        });
        var callProcessor = CreateProcessor();

        // Act
        var result = await callProcessor
            .ProcessAsync(
                _callRequestMock.Object,
                _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Ok);
        Assert.AreEqual(_timestamp, result.Timestamp);
        Assert.AreEqual("There are one or more errors.", result.Reason);
        Assert.IsNotNull(result.Errors);
        string[] expectedErrors = [errorMessage];
        string[] actualErrors = [.. result.Errors];
        CollectionAssert.AreEquivalent(expectedErrors, actualErrors);
        VerifyAllMocks();
    }

    private CallProcessor CreateProcessor()
    {
        return new CallProcessor(
            _callRequestValidatorMock.Object,
            _callRequestProcessorFactoryMock.Object,
            _dbDateTimeProviderMock.Object
        );
    }

    private void SetupValidationResult(ValidationResult result)
    {
        _callRequestValidatorMock
            .Setup(validator => validator
                .ValidateAsync(
                    _callRequestMock.Object,
                    _cancellationToken))
            .ReturnsAsync(result)
            .Verifiable(Times.Once);
    }

    private void SetupProcessAsync(ICallResponse? response)
    {
        _callRequestProcessorMock
            .Setup(processor => processor
                .ProcessAsync(
                    _callRequestMock.Object,
                    _cancellationToken))
            .ReturnsAsync(response)
            .Verifiable(Times.Once);
    }

    private void VerifyAllMocks()
    {
        _callRequestMock.VerifyAll();
        _callRequestMock.VerifyNoOtherCalls();

        _callRequestValidatorMock.VerifyAll();
        _callRequestValidatorMock.VerifyNoOtherCalls();

        _dbDateTimeProviderMock.VerifyAll();
        _dbDateTimeProviderMock.VerifyNoOtherCalls();

        _callRequestProcessorMock.VerifyAll();
        _callRequestProcessorMock.VerifyNoOtherCalls();
    }
}