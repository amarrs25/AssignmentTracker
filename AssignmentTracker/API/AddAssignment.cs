using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AssignmentTracker.API;

public class AddAssignment
{
    private readonly IAssignmentService _assignmentService;
    private readonly IDataService _dataService;
    private readonly ILogger<AddAssignment> _logger;

    public AddAssignment(ILogger<AddAssignment> logger, IAssignmentService assignmentService, IDataService dataService)
    {
        _logger = logger;
        _assignmentService = assignmentService;
        _dataService = dataService;
    }

    [Function("AddAssignment")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: AddAssignment at {DateTime.UtcNow}");

            // Resolve the path to the assignments.txt file
            var assignmentsFilePath = _dataService.GetFullFilePath("assignments.txt");
            _logger.LogInformation($"Resolved file path: {assignmentsFilePath}");

            // Use IDataService to read the existing content of the file
            var jsonData = await _dataService.ReadFile(assignmentsFilePath);
            var assignments = JsonConvert.DeserializeObject<List<AssignmentModel>>(jsonData) ?? new List<AssignmentModel>();
            _logger.LogInformation("Assignments List Created");

            // Read the request body
            var requestBody = await req.ReadAsStringAsync();

            // Deserialize the request body to an AssignmentModel
            var newAssignment = JsonConvert.DeserializeObject<AssignmentModel>(requestBody!);

            // Validate that the new assignment is not null
            _assignmentService.ValidateNewAssignment(newAssignment!);

            // Add the new assignment using the assignment service
            var updatedAssignments = _assignmentService.AddAssignment(assignments, newAssignment!);

            // Write updated data back to the file using IDataService
            await _dataService.WriteFile(assignmentsFilePath, updatedAssignments);

            // Respond with success
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Assignment added successfully!");
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
