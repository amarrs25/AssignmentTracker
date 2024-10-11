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

public class GetClassTests
{
    private readonly Mock<IClassService> _classService;
    private readonly Mock<IDataService> _dataService;
    private readonly Mock<ILogger<GetClass>> _logger;
    private readonly GetClass _sut; // SUT = System Under Test

    public GetClassTests()
    {
        _logger = new Mock<ILogger<GetClass>>();
        _classService = new Mock<IClassService>();
        _dataService = new Mock<IDataService>();
        _sut = new GetClass(_logger.Object, _classService.Object, _dataService.Object);
    }

   [Test]
public async Task GetClass_Returns_ClassList_When_FileExists()
{
    // Setup
    var result =
        "[\r\n  {\r\n    \"ClassId\": 1,\r\n    \"ClassName\": \"Math 101\",\r\n    \"ClassDesc\": \"Introduction to Math\",\r\n    \"ProfessorName\": \"Dr. Smith\"\r\n  }\r\n]";
    var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var filePath = "AssignmentTracker/classes.txt";
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
    _classService.Verify(c => c.GetClass(It.IsAny<List<ClassModel>>()),
        Times.AtLeastOnce);
}


    [Test]
    public async Task GetClass_Returns_BadRequest_When_FileReadFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/classes.txt";
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
    public async Task GetClass_Returns_BadRequest_When_JsonDeserializationFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/classes.txt";
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
    public async Task GetClass_Returns_BadRequest_When_GetClassFromServiceFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/classes.txt";
        var validJsonData =
            "[{\"ClassId\": 1, \"ClassName\": \"Math 101\", \"ClassDesc\": \"Intro to Math\", \"ProfessorName\": \"Dr. Smith\"}]";
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(filePath);
        _dataService.Setup(ds => ds.ReadFile(filePath)).ReturnsAsync(validJsonData);
        _classService.Setup(cs => cs.GetClass(It.IsAny<List<ClassModel>>()))
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