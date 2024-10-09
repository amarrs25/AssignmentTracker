using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace  AssignmentTracker.API;

public class AddAssignment
{
    private readonly ILogger<AddAssignment> _logger;
    private readonly IAssignmentService _assignmentService;

    public AddAssignment(ILogger<AddAssignment> logger, IAssignmentService assignmentService)
    {
        _logger = logger;
        _assignmentService = assignmentService;
    }

    [Function("AddAssignment")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var assignmentsFilePath = Path.Combine(AppContext.BaseDirectory, "../../../assignments.txt");
        _logger.LogInformation($"Resolved file path: {assignmentsFilePath}");

        try
        {
            // Read the existing content of the file
            var jsonData = await File.ReadAllTextAsync(assignmentsFilePath);
            var assignments = JsonConvert.DeserializeObject<List<AssignmentModel>>(jsonData) ?? new List<AssignmentModel>();

            // Add new assignment (example: deserialized from the request body)
            var requestBody = await req.ReadAsStringAsync();
            var newAssignment = JsonConvert.DeserializeObject<AssignmentModel>(requestBody);
            
            var updatedAssignments = _assignmentService.AddAssignment(assignments, newAssignment);
            
            // Write updated data back to the file
            var updatedJsonData = JsonConvert.SerializeObject(updatedAssignments, Formatting.Indented);
            await File.WriteAllTextAsync(assignmentsFilePath, updatedJsonData);

            // Respond with success
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Assignment added successfully!");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while adding the assignment.");
            return errorResponse;
        }
    }
}
