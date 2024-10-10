using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace  AssignmentTracker.API;

public class AddPriority
{
    private readonly ILogger<AddPriority> _logger;
    private readonly IPriorityService _priorityService;

    public AddPriority(ILogger<AddPriority> logger, IPriorityService priorityService)
    {
        _logger = logger;
        _priorityService = priorityService;
    }

    [Function("AddPriority")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var prioritiesFilePath = Path.Combine(AppContext.BaseDirectory, "../../../priorities.txt");
        _logger.LogInformation($"Resolved file path: {prioritiesFilePath}");

        try
        {
            // Read the existing content of the file
            var jsonData = await File.ReadAllTextAsync(prioritiesFilePath);
            var priorities = JsonConvert.DeserializeObject<List<PriorityModel>>(jsonData) ?? new List<PriorityModel>();

            // Read the request body
            var requestBody = await req.ReadAsStringAsync();

            // Validate the request body
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Request body is null or empty.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body cannot be null or empty.");
                return badRequestResponse;
            }

            // Deserialize the request body to a PriorityModel
            var newPriority = JsonConvert.DeserializeObject<PriorityModel>(requestBody);
            if (newPriority == null)
            {
                _logger.LogWarning("Deserialization of request body failed.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync(
                    "Invalid request body. Please provide a valid PriorityModel.");
                return badRequestResponse;
            }

            // Add the new priority using the priority service
            var updatedPriorities = _priorityService.AddPriority(priorities, newPriority);

            // Write updated data back to the file
            var updatedJsonData = JsonConvert.SerializeObject(updatedPriorities, Formatting.Indented);
            await File.WriteAllTextAsync(prioritiesFilePath, updatedJsonData);

            // Respond with success
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Priority added successfully!");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while adding the priority.");
            return errorResponse;
        }
    }
}
