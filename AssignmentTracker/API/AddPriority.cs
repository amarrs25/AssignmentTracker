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

            // Add new priority (example: deserialized from the request body)
            var requestBody = await req.ReadAsStringAsync();
            var newPriority = JsonConvert.DeserializeObject<PriorityModel>(requestBody);
            
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
