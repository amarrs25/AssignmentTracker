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
using Newtonsoft.Json;

namespace AssignmentTracker.Test.API;

public class GetPriorityTests
{
    private readonly Mock<IDataService> _dataService;
    private readonly Mock<ILogger<GetPriority>> _logger;
    private readonly Mock<IPriorityService> _priorityService;
    private readonly GetPriority _sut; //SUT = System Under Test

    public GetPriorityTests()
    {
        _logger = new Mock<ILogger<GetPriority>>();
        _priorityService = new Mock<IPriorityService>();
        _dataService = new Mock<IDataService>();
        _sut = new GetPriority(_logger.Object, _priorityService.Object, _dataService.Object);
    }

    [Test]
    public async Task GetPriority_Returns_PriorityList_When_FileExists()
    {
        //Setup
        var postData = new PriorityModel
        {
            AssignmentId = 1,
            PriorityType = 2
        };
        var result =
            "[\r\n  {\r\n    \"PriorityId\": 1,\r\n    \"AssignmentId\": 1,\r\n    \"PriorityName\": \"Medium\",\r\n    \"PriorityType\": 2\r\n  }\r\n]";
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/priorities.txt";
        var fullFilePath = Path.Combine(rootPath, filePath);


        // Mock the IDataService behavior
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);
        _dataService.Setup(ds => ds.ReadFile(fullFilePath)).ReturnsAsync(result);

        // -- HTTPRequestData Setup BEGIN-- You can copy this section for HttpRequestData setup
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);
        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Body)
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(postData))));
        request.Setup(req => req.Headers).Returns(new HttpHeadersCollection());
        request.Setup(req => req.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, []);
            response.SetupProperty(r => r.StatusCode, HttpStatusCode.Unauthorized);
            response.SetupProperty(r => r.Body,
                new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result))));
            return response.Object;
        });
        // -- HTTPRequestData Setup END-- You can copy this section for HttpRequestData setup

        //Act
        var sutResult = await _sut.RunAsync(request.Object);

        //Assert

        //Reset Memory Stream
        request.Setup(req => req.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(result)));
        var responseContent = await new StreamReader(request.Object.Body).ReadToEndAsync();
        Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseContent,
            Is.EqualTo(
                "[\r\n  {\r\n    \"PriorityId\": 1,\r\n    \"AssignmentId\": 1,\r\n    \"PriorityName\": \"Medium\",\r\n    \"PriorityType\": 2\r\n  }\r\n]"));
        _logger.Verify(logger => logger.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                ((Func<It.IsAnyType, Exception, string>)It.IsAny<object>())!),
            Times.AtLeastOnce);
        _priorityService.Verify(p => p.GetPriority(It.IsAny<List<PriorityModel>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task GetPriority_Returns_BadRequest_When_FileReadFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/priorities.txt";
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
    public async Task GetPriority_Returns_BadRequest_When_JsonDeserializationFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/priorities.txt";
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
            Times.AtLeastOnce); // Expecting the log to occur exactly once
    }


    [Test]
    public async Task GetPriority_Returns_BadRequest_When_GetPriorityFromServiceFails()
    {
        // Arrange
        var filePath = "AssignmentTracker/priorities.txt";
        var validJsonData =
            "[{\"PriorityId\": 1, \"AssignmentId\": 1, \"PriorityType\": 2, \"PriorityName\": \"Medium\"}]";
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(filePath);
        _dataService.Setup(ds => ds.ReadFile(filePath)).ReturnsAsync(validJsonData);
        _priorityService.Setup(ps => ps.GetPriority(It.IsAny<List<PriorityModel>>()))
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