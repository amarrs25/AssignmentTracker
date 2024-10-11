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

public class AddClassTests
{
    private readonly Mock<IClassService> _classService;
    private readonly Mock<IDataService> _dataService;
    private readonly Mock<ILogger<AddClass>> _logger;
    private readonly AddClass _sut; // SUT = System Under Test

    public AddClassTests()
    {
        _logger = new Mock<ILogger<AddClass>>();
        _classService = new Mock<IClassService>();
        _dataService = new Mock<IDataService>();
        _sut = new AddClass(_logger.Object, _classService.Object, _dataService.Object);
    }

    [Test]
    public async Task RunAsync_ReturnsOk_When_ClassIsAddedSuccessfully()
    {
        // Arrange
        var postData = new ClassModel
        {
            ClassName = "Math 101",
            ClassDesc = "Introduction to Mathematics",
            ProfessorName = "Dr. Smith"
        };
        var existingClasses = new List<ClassModel>
        {
            new()
            {
                ClassId = 1, ClassName = "History 101", ClassDesc = "History Overview", ProfessorName = "Prof. Jones"
            }
        };
        var updatedClasses = new List<ClassModel>(existingClasses)
        {
            new()
            {
                ClassId = 2, // Example new ID for the added class
                ClassName = postData.ClassName,
                ClassDesc = postData.ClassDesc,
                ProfessorName = postData.ProfessorName
            }
        };

        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/classes.txt";
        var fullFilePath = Path.Combine(rootPath, filePath);
        var jsonData = JsonConvert.SerializeObject(existingClasses);

        // Mock the IDataService behavior
        _dataService.Setup(ds => ds.GetFullFilePath(It.IsAny<string>())).Returns(fullFilePath);
        _dataService.Setup(ds => ds.ReadFile(fullFilePath)).ReturnsAsync(jsonData);
        _classService
            .Setup(cs => cs.AddClass(It.IsAny<List<ClassModel>>(), It.Is<ClassModel>(
                c => c.ClassName == postData.ClassName && c.ClassDesc == postData.ClassDesc)))
            .Returns(updatedClasses);
        _classService.Setup(cs => cs.ValidateNewClass(It.IsAny<ClassModel>())).Verifiable();

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
        _classService.Verify(cs => cs.ValidateNewClass(It.IsAny<ClassModel>()), Times.AtLeastOnce);
        _classService.Verify(cs => cs.AddClass(It.IsAny<List<ClassModel>>(), It.Is<ClassModel>(
            c => c.ClassName == postData.ClassName && c.ClassDesc == postData.ClassDesc)), Times.Once);
        _dataService.Verify(ds => ds.WriteFile(fullFilePath, It.IsAny<List<ClassModel>>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_ReturnsBadRequest_When_InvalidRequestBody()
    {
        // Arrange
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var filePath = "AssignmentTracker/classes.txt";
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
        var filePath = "AssignmentTracker/classes.txt";
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
            .Returns(new MemoryStream(
                Encoding.UTF8.GetBytes("{\"ClassName\": \"Math 101\", \"ClassDesc\": \"Intro to Math\"}")));
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