using System.IO;
using System.Threading.Tasks;
using AssignmentTracker.API;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;

namespace AssignmentTracker.Test
{
    [TestFixture]
    public class GetPriorityTests
    {
        private GetPriority _function;
        private Mock<IPriorityService> _priorityService;

        [SetUp]
        public void Setup()
        {
            // Initialize the mock service
            _priorityService = new Mock<IPriorityService>();

            // Initialize the GetPriority function with a NullLogger to avoid real logging
            _function = new GetPriority(NullLogger<GetPriority>.Instance, _priorityService.Object);
        }

        [Test]
        public async Task Run_ShouldReturnEmptyList_WhenCategoriesFileIsEmpty()
        {
            // Arrange: Mock the service to return an empty list
             _priorityService.Setup(service => service.GetPriority())
                .ReturnsAsync(new List<PriorityModel>());

            // Create a mock HttpRequestData
            var context = new Mock<FunctionContext>();
            var request = new Mock<HttpRequestData>(context.Object);
            request.Setup(req => req.CreateResponse()).Returns(() =>
            {
                var response = new Mock<HttpResponseData>(context.Object);
                response.SetupProperty(r => r.StatusCode, System.Net.HttpStatusCode.OK);
                response.SetupProperty(r => r.Body, new MemoryStream());
                return response.Object;
            });

            // Act: Call the GetPriority function
            var result = await _function.RunAsync(request.Object);

            // Assert: Check that the result has a 200 OK status
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

            // Read the response body
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(result.Body).ReadToEndAsync();

            // Deserialize the response content to a list of PriorityModel using Newtonsoft.Json
            var returnedPriorities = JsonConvert.DeserializeObject<List<PriorityModel>>(responseContent);

            // Assert: Verify that an empty list is returned
            Assert.That(returnedPriorities, Is.Not.Null);
            Assert.That(returnedPriorities, Is.Empty);
        }
    }
}
