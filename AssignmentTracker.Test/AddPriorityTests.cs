using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using AssignmentTracker.API;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Newtonsoft.Json;

namespace AssignmentTracker.Test;

public class AddPriorityTests
{
    private readonly Mock<ILogger<AddPriority>> _logger;
    private readonly Mock<IPriorityService> _quickCalculations;
    private readonly AddPriority _sut; //SUT = System Under Test
    
    public AddPriorityTests()
    {
        _logger = new Mock<ILogger<AddPriority>>();
        _quickCalculations = new Mock<IPriorityService>();
        _sut = new AddPriority(_logger.Object, _quickCalculations.Object);
    }

    [Test]
    public async Task Given_PostDataIsCorrect_Then_AddData_Return_Result()
    {
        //Setup
        var postData = new PriorityModel  //
        {
            AssignmentId = 1,
            PriorityType = 2
        };
        var result = "Priority added successfully!";
        
        // -- HTTPRequestData Setup BEGIN-- You can copy this section for HttpRequestData setup
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);
        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(req => req.Body).
            Returns(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(postData))));
        request.Setup(req => req.Headers).Returns(new HttpHeadersCollection());
        request.Setup(req => req.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, []);
            response.SetupProperty(r => r.StatusCode, HttpStatusCode.Unauthorized);
            response.SetupProperty(r => r.Body, 
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result))));
            return response.Object;
        });
        // -- HTTPRequestData Setup END-- You can copy this section for HttpRequestData setup
        
        
        //Act
        var sutResult = await _sut.RunAsync(request.Object);
        //Assert
        
        var responseContent = await new StreamReader(sutResult.Body).ReadToEndAsync();
        Assert.That(sutResult.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseContent, Is.EqualTo("{\"PriorityId\":1,\"AssignmentId\":1,\"PriorityName\":\"Medium\",\"PriorityType\":2}"));
        _logger.Verify(logger => logger.Log(LogLevel.Information, It.IsAny<EventId>(), 
                It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), ((Func<It.IsAnyType, Exception, string>)It.IsAny<object>())!), 
            Times.Exactly(2));
        _quickCalculations.Verify(p => p.AddPriority(It.IsAny<List<PriorityModel>>(), It.IsAny<PriorityModel>()), 
            Times.Exactly(1));

    }
}