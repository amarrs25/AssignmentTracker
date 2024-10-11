using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace AssignmentTracker.API;

public class GetPriority
{
    private readonly ILogger<GetPriority> _logger;
    private readonly IPriorityService _priorityService;
    private readonly IDataService _dataService;

    public GetPriority(ILogger<GetPriority> logger, IPriorityService priorityService, IDataService dataService)
    {
        _logger = logger;
        _priorityService = priorityService;
        _dataService = dataService;
    }

    [Function("GetPriority")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: GetPriority at {DateTime.UtcNow}");
            
            // Resolve the path to the priorities.txt file
            var prioritiesFilePath = _dataService.GetFullFilePath("priorities.txt");  
            _logger.LogInformation($"Resolved file path: {prioritiesFilePath}");

            //Call Priority Service to check if there are any priorities
            var jsonData = await File.ReadAllTextAsync(prioritiesFilePath);
            var priorities = JsonConvert.DeserializeObject<List<PriorityModel>>(jsonData) ?? new List<PriorityModel>();
            _priorityService.GetPriority(priorities);
            _logger.LogInformation("Priority list is not null");
            
            // Create the response with the file content
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(await _dataService.ReadFile(prioritiesFilePath));
            
            _logger.LogInformation($"Function completed: GetPriority at {DateTime.UtcNow}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
