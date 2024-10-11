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
    private readonly IDataService _dataService;

    public AddPriority(ILogger<AddPriority> logger, IPriorityService priorityService, IDataService dataService)
    {
        _logger = logger;
        _priorityService = priorityService;
        _dataService = dataService;
    }

    [Function("AddPriority")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: AddPriority at {DateTime.UtcNow}");
            
            // Resolve the path to the priorities.txt file
            var prioritiesFilePath = _dataService.GetFullFilePath("priorities.txt");  
            _logger.LogInformation($"Resolved file path: {prioritiesFilePath}");
            
            // Read the existing content of the file
            var jsonData = await File.ReadAllTextAsync(prioritiesFilePath);
            var priorities = JsonConvert.DeserializeObject<List<PriorityModel>>(jsonData) ?? new List<PriorityModel>();
            _logger.LogInformation($"Priorities List Created");
            
            // Read the request body
            var requestBody = await req.ReadAsStringAsync();

            // Deserialize the request body to a PriorityModel
            var newPriority = JsonConvert.DeserializeObject<PriorityModel>(requestBody!);
            
            //Validate that the new priority is not null
            _priorityService.ValidateNewPriority(newPriority!);

            // Add the new priority using the priority service
            var updatedPriorities = _priorityService.AddPriority(priorities, newPriority!);

            // Write updated data back to the file
            await _dataService.WriteFile(prioritiesFilePath, updatedPriorities);

            // Respond with success
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Priority added successfully!");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
