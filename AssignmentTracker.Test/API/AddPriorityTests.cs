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

public class AddPriorityTests
{
    private readonly Mock<IDataService> _dataService;
    private readonly Mock<ILogger<AddPriority>> _logger;
    private readonly Mock<IPriorityService> _priorityService;
    private readonly AddPriority _sut; // SUT = System Under Test

    public AddPriorityTests()
    {
        _logger = new Mock<ILogger<AddPriority>>();
        _priorityService = new Mock<IPriorityService>();
        _dataService = new Mock<IDataService>();
        _sut = new AddPriority(_logger.Object, _priorityService.Object, _dataService.Object);
    }

    [Test]
    public async Task RunAsync_ReturnsOk_When_PriorityIsAddedSuccessfully()
    {
        // Arrange
        var postData = new PriorityModel
        {
            AssignmentId = 1,
            PriorityType = 2,
            PriorityName = "Medium"
        };
        var existingPriorities = new List<PriorityModel>
        {
            new() { PriorityId = 1, AssignmentId = 1, PriorityType = 1, PriorityName = "Low" }
        };
        var updatedPriorities = new List<PriorityModel>(existingPriorities)
        {
            new()
            {
                PriorityId = 2, // Example new ID for the added priority
                AssignmentId = postData.AssignmentId,
                PriorityType = postData.PriorityType,
                PriorityName = "Medium"
            }
        };

        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/priorities.txt";
        var fullFilePath = Path.Combine(rootPath, filePath);
        var jsonData = JsonConvert.SerializeObject(existingPriorities);

        // Mock the IDataService behavior
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);
        _dataService.Setup(ds => ds.ReadFile(fullFilePath)).ReturnsAsync(jsonData);
        _priorityService
            .Setup(ps => ps.AddPriority(It.IsAny<List<PriorityModel>>(), It.Is<PriorityModel>(
                p => p.AssignmentId == postData.AssignmentId && p.PriorityType == postData.PriorityType)))
            .Returns(updatedPriorities);
        _priorityService.Setup(ps => ps.ValidateNewPriority(It.IsAny<PriorityModel>())).Verifiable();

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
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode, HttpStatusCode.OK);
            response.SetupProperty(r => r.Body, new MemoryStream());

            return response.Object;
        });

        // Act
        var sutResult = await _sut.RunAsync(request.Object);

        // Assert
        Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        _logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        _priorityService.Verify(ps => ps.ValidateNewPriority(It.IsAny<PriorityModel>()), Times.AtLeastOnce);
        _priorityService.Verify(ps => ps.AddPriority(It.IsAny<List<PriorityModel>>(), It.Is<PriorityModel>(
            p => p.AssignmentId == postData.AssignmentId && p.PriorityType == postData.PriorityType)), Times.Once);
        _dataService.Verify(ds => ds.WriteFile(fullFilePath, It.IsAny<List<PriorityModel>>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_ReturnsBadRequest_When_InvalidRequestBody()
    {
        // Arrange
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/priorities.txt";
        var fullFilePath = Path.Combine(rootPath, filePath);

        // Mock the IDataService behavior
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Body)
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("Invalid JSON")));
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

        // Adjust the verification to allow multiple error logs
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }


    [Test]
    public async Task RunAsync_ReturnsBadRequest_When_FileReadFails()
    {
        // Arrange
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/priorities.tx";
        var fullFilePath = Path.Combine(rootPath, filePath);

        // Mock the IDataService behavior
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);
        _dataService.Setup(ds => ds.ReadFile(fullFilePath)).ThrowsAsync(new Exception("File read error"));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Body)
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("{\"AssignmentId\": 1, \"PriorityType\": 2}")));
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
        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}