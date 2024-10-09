using System.Net;
using AssignmentTracker.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AssignmentTracker.API;

public class GetClass
{
    private readonly ILogger<GetClass> _logger;
    private readonly IClassService _classService;
    private readonly string _rootPath;

    public GetClass(ILogger<GetClass> logger, IClassService classService)
    {
        _logger = logger;
        _classService = classService;
        
        // Assume project root is two levels up from the base directory (e.g., bin/Debug/net8.0)
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    }

    private string GetFullFilePath(string relativePath)
    {
        // Combine the root path with the relative path to access files in the root directory.
        return Path.Combine(_rootPath, relativePath);
    }

    [Function("GetClass")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: GetClass at {DateTime.UtcNow}");

            // Resolve the path to the classes.txt file
            var classesFilePath = GetFullFilePath("classes.txt");
            _logger.LogInformation($"Resolved file path: {classesFilePath}");

            // Check if the file exists
            if (!File.Exists(classesFilePath))
            {
                _logger.LogWarning("Classes file not found.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Classes file not found.");
                return notFoundResponse;
            }

            // Read the content of the classes.txt file
            var fileContent = await File.ReadAllTextAsync(classesFilePath);
            _logger.LogInformation("Contents of classes.txt:");
            _logger.LogInformation(fileContent);

            // Create the response with the file content
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(fileContent);

            _logger.LogInformation($"Function completed: GetClass at {DateTime.UtcNow}");
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
