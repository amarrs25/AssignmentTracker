using System.Net;
using System.Text;
using AssignmentTracker.API;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssignmentTracker.Test.API;

public class GetAssignmentTests
{
    private readonly Mock<IAssignmentService> _assignmentService;
    private readonly Mock<IDataService> _dataService;
    private readonly Mock<ILogger<GetAssignment>> _logger;
    private readonly GetAssignment _sut; // SUT = System Under Test

    public GetAssignmentTests()
    {
        _logger = new Mock<ILogger<GetAssignment>>();
        _assignmentService = new Mock<IAssignmentService>();
        _dataService = new Mock<IDataService>();
        _sut = new GetAssignment(_logger.Object, _assignmentService.Object, _dataService.Object);
    }

    [Test]
public async Task GetAssignment_Returns_AssignmentList_When_FileExists()
{
    // Setup
    var result =
        "[\r\n  {\r\n    \"AssignmentId\": 1,\r\n    \"ClassId\": 1,\r\n    \"AssignmentName\": \"Math Homework\",\r\n    \"AssignmentDesc\": \"Complete chapters 1-3\",\r\n    \"AssignmentDate\": \"2023-10-05T00:00:00Z\",\r\n    \"IsCompleted\": false\r\n  }\r\n]";
    var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var filePath = "AssignmentTracker/assignments.txt";
    var fullFilePath = Path.Combine(rootPath, filePath);

    // Mock the IDataService behavior
    _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);
    _dataService.Setup(ds => ds.ReadFile(fullFilePath)).ReturnsAsync(result);

    // -- HTTPRequestData Setup BEGIN--
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var context = new Mock<FunctionContext>();
    context.SetupProperty(c => c.InstanceServices, serviceProvider);
    var request = new Mock<HttpRequestData>(context.Object);
    request.Setup(req => req.Headers).Returns(new HttpHeadersCollection());
    request.Setup(req => req.CreateResponse()).Returns(() =>
    {
        var response = new Mock<HttpResponseData>(context.Object);
        response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
        response.SetupProperty(r => r.StatusCode, HttpStatusCode.OK);
        var memoryStream = new MemoryStream();
        response.SetupProperty(r => r.Body, memoryStream);
        return response.Object;
    });

    // Act
    var sutResult = await _sut.RunAsync(request.Object);

    // Rewind the memory stream to read the content written by the API
    sutResult.Body.Seek(0, SeekOrigin.Begin);
    var responseContent = await new StreamReader(sutResult.Body).ReadToEndAsync();

    // Assert
    Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    Assert.That(responseContent, Is.EqualTo(result));
    _logger.Verify(logger => logger.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.AtLeastOnce);
    _assignmentService.Verify(a => a.GetAssignment(It.IsAny<List<AssignmentModel>>()),
        Times.AtLeastOnce);
}


    [Test]
    public async Task GetAssignment_Returns_BadRequest_When_FileReadFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/assignments.txt";
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(filePath);
        _dataService.Setup(ds => ds.ReadFile(filePath)).ThrowsAsync(new IOException("File read error"));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Headers).Returns(new HttpHeadersCollection());
        request.Setup(req => req.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode, HttpStatusCode.BadRequest);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        // Act
        var sutResult = await _sut.RunAsync(request.Object);

        // Assert
        Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Test]
    public async Task GetAssignment_Returns_BadRequest_When_JsonDeserializationFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/assignments.txt";
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(filePath);
        _dataService.Setup(ds => ds.ReadFile(filePath)).ReturnsAsync("Invalid JSON content");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Headers).Returns(new HttpHeadersCollection());
        request.Setup(req => req.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode, HttpStatusCode.BadRequest);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        // Act
        var sutResult = await _sut.RunAsync(request.Object);

        // Assert
        Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _logger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task GetAssignment_Returns_BadRequest_When_GetAssignmentFromServiceFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/assignments.txt";
        var validJsonData =
            "[{\"AssignmentId\": 1, \"ClassId\": 1, \"AssignmentName\": \"Math Homework\", \"AssignmentDesc\": \"Complete chapters 1-3\", \"AssignmentDate\": \"2023-10-05T00:00:00Z\", \"IsCompleted\": false}]";
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(filePath);
        _dataService.Setup(ds => ds.ReadFile(filePath)).ReturnsAsync(validJsonData);
        _assignmentService.Setup(a => a.GetAssignment(It.IsAny<List<AssignmentModel>>()))
            .Throws(new Exception("Service error"));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Headers).Returns(new HttpHeadersCollection());
        request.Setup(req => req.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode, HttpStatusCode.BadRequest);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        // Act
        var sutResult = await _sut.RunAsync(request.Object);

        // Assert
        Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}