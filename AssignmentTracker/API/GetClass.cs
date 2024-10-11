using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AssignmentTracker.API;

public class GetClass
{
    private readonly IClassService _classService;
    private readonly IDataService _dataService;
    private readonly ILogger<GetClass> _logger;

    public GetClass(ILogger<GetClass> logger, IClassService classService, IDataService dataService)
    {
        _logger = logger;
        _classService = classService;
        _dataService = dataService;
    }

    [Function("GetClass")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: GetClass at {DateTime.UtcNow}");

            // Resolve the path to the classes.txt file
            var classesFilePath = _dataService.GetFullFilePath("classes.txt");
            _logger.LogInformation($"Resolved file path: {classesFilePath}");

            // Read the class data from the file
            var jsonData = await File.ReadAllTextAsync(classesFilePath);
            var classes = JsonConvert.DeserializeObject<List<ClassModel>>(jsonData) ?? new List<ClassModel>();
            _classService.GetClass(classes);
            _logger.LogInformation("Class list is not null");

            // Create the response with the file content
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(await _dataService.ReadFile(classesFilePath));

            _logger.LogInformation($"Function completed: GetClass at {DateTime.UtcNow}");
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