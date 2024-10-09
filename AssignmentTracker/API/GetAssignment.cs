using System.Net;
using AssignmentTracker.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AssignmentTracker.API;

public class GetAssignment
{
    private readonly ILogger<GetAssignment> _logger;
    private readonly IAssignmentService _assignmentService;
    private readonly string _rootPath;

    public GetAssignment(ILogger<GetAssignment> logger, IAssignmentService assignmentService)
    {
        _logger = logger;
        _assignmentService = assignmentService;
        
        // Assume project root is two levels up from the base directory (e.g., bin/Debug/net8.0)
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    }

    private string GetFullFilePath(string relativePath)
    {
        // Combine the root path with the relative path to access files in the root directory.
        return Path.Combine(_rootPath, relativePath);
    }

    [Function("GetAssignment")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: GetAssignment at {DateTime.UtcNow}");

            // Resolve the path to the assignments.txt file
            var assignmentsFilePath = GetFullFilePath("assignments.txt");
            _logger.LogInformation($"Resolved file path: {assignmentsFilePath}");

            // Check if the file exists
            if (!File.Exists(assignmentsFilePath))
            {
                _logger.LogWarning("Assignments file not found.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Assignments file not found.");
                return notFoundResponse;
            }

            // Read the content of the assignments.txt file
            var fileContent = await File.ReadAllTextAsync(assignmentsFilePath);
            _logger.LogInformation("Contents of assignments.txt:");
            _logger.LogInformation(fileContent);

            // Create the response with the file content
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(fileContent);

            _logger.LogInformation($"Function completed: GetAssignment at {DateTime.UtcNow}");
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
