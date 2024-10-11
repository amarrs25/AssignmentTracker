using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AssignmentTracker.API;

public class GetAssignment
{
    private readonly IAssignmentService _assignmentService;
    private readonly IDataService _dataService;
    private readonly ILogger<GetAssignment> _logger;

    public GetAssignment(ILogger<GetAssignment> logger, IAssignmentService assignmentService, IDataService dataService)
    {
        _logger = logger;
        _assignmentService = assignmentService;
        _dataService = dataService;
    }

    [Function("GetAssignment")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: GetAssignment at {DateTime.UtcNow}");

            // Resolve the path to the assignments.txt file
            var assignmentsFilePath = _dataService.GetFullFilePath("assignments.txt");
            _logger.LogInformation($"Resolved file path: {assignmentsFilePath}");

            // Read the assignments data from the file
            var jsonData = await File.ReadAllTextAsync(assignmentsFilePath);
            var assignments = JsonConvert.DeserializeObject<List<AssignmentModel>>(jsonData) ??
                              new List<AssignmentModel>();
            _assignmentService.GetAssignment(assignments);
            _logger.LogInformation("Assignment list is not null");

            // Create the response with the file content
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(await _dataService.ReadFile(assignmentsFilePath));

            _logger.LogInformation($"Function completed: GetAssignment at {DateTime.UtcNow}");
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