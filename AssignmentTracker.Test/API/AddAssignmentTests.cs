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

public class AddAssignmentTests
{
    private readonly Mock<IAssignmentService> _assignmentService;
    private readonly Mock<IDataService> _dataService;
    private readonly Mock<ILogger<AddAssignment>> _logger;
    private readonly AddAssignment _sut; // SUT = System Under Test

    public AddAssignmentTests()
    {
        _logger = new Mock<ILogger<AddAssignment>>();
        _assignmentService = new Mock<IAssignmentService>();
        _dataService = new Mock<IDataService>();
        _sut = new AddAssignment(_logger.Object, _assignmentService.Object, _dataService.Object);
    }

    [Test]
    public async Task RunAsync_ReturnsOk_When_AssignmentIsAddedSuccessfully()
    {
        // Arrange
        var postData = new AssignmentModel
        {
            ClassId = 1,
            AssignmentName = "Math Homework",
            AssignmentDesc = "Complete chapters 1-3",
            AssignmentDate = DateTime.UtcNow,
            IsCompleted = false
        };
        var existingAssignments = new List<AssignmentModel>
        {
            new()
            {
                AssignmentId = 1, ClassId = 1, AssignmentName = "History Essay",
                AssignmentDesc = "Write about World War II", AssignmentDate = DateTime.UtcNow.AddDays(-2),
                IsCompleted = true
            }
        };
        var updatedAssignments = new List<AssignmentModel>(existingAssignments)
        {
            new()
            {
                AssignmentId = 2, // Example new ID for the added assignment
                ClassId = postData.ClassId,
                AssignmentName = postData.AssignmentName,
                AssignmentDesc = postData.AssignmentDesc,
                AssignmentDate = postData.AssignmentDate,
                IsCompleted = postData.IsCompleted
            }
        };

        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/assignments.txt";
        var fullFilePath = Path.Combine(rootPath, filePath);
        var jsonData = JsonConvert.SerializeObject(existingAssignments);

        // Mock the IDataService behavior
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);
        _dataService.Setup(ds => ds.ReadFile(fullFilePath)).ReturnsAsync(jsonData);
        _assignmentService
            .Setup(a => a.AddAssignment(It.IsAny<List<AssignmentModel>>(), It.Is<AssignmentModel>(
                a => a.ClassId == postData.ClassId && a.AssignmentName == postData.AssignmentName)))
            .Returns(updatedAssignments);
        _assignmentService.Setup(a => a.ValidateNewAssignment(It.IsAny<AssignmentModel>())).Verifiable();

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
        _assignmentService.Verify(a => a.ValidateNewAssignment(It.IsAny<AssignmentModel>()), Times.AtLeastOnce);
        _assignmentService.Verify(a => a.AddAssignment(It.IsAny<List<AssignmentModel>>(), It.Is<AssignmentModel>(
            a => a.ClassId == postData.ClassId && a.AssignmentName == postData.AssignmentName)), Times.AtLeastOnce);
        _dataService.Verify(ds => ds.WriteFile(fullFilePath, It.IsAny<List<AssignmentModel>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_ReturnsBadRequest_When_InvalidRequestBody()
    {
        // Arrange
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/assignments.txt";
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
        var filePath = "AssignmentTracker/assignments.txt";
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
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(
                "{\"ClassId\": 1, \"AssignmentName\": \"Math Homework\", \"AssignmentDesc\": \"Complete chapters 1-3\"}")));
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