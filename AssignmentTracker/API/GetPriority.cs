using System.Net;
using AssignmentTracker.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AssignmentTracker.API;

public class GetPriority
{
    private readonly ILogger<GetPriority> _logger;
    private readonly IPriorityService _priorityService;
    private readonly string _rootPath;

    public GetPriority(ILogger<GetPriority> logger, IPriorityService priorityService)
    {
        _logger = logger;
        _priorityService = priorityService;
        
        // Assume project root is two levels up from the base directory (e.g., bin/Debug/net8.0)
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    }

    private string GetFullFilePath(string relativePath)
    {
        // Combine the root path with the relative path to access files in the root directory.
        return Path.Combine(_rootPath, relativePath);
    }

    [Function("GetPriority")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: GetPriority at {DateTime.UtcNow}");

            // Resolve the path to the priorities.txt file
            var prioritiesFilePath = GetFullFilePath("priorities.txt");
            _logger.LogInformation($"Resolved file path: {prioritiesFilePath}");

            // Check if the file exists
            if (!File.Exists(prioritiesFilePath))
            {
                _logger.LogWarning("priorities file not found.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("priorities file not found.");
                return notFoundResponse;
            }

            // Read the content of the priorities.txt file
            var fileContent = await File.ReadAllTextAsync(prioritiesFilePath);
            _logger.LogInformation("Contents of priorities.txt:");
            _logger.LogInformation(fileContent);

            // Create the response with the file content
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(fileContent);

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
