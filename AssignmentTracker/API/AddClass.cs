using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AssignmentTracker.API;

public class AddClass
{
    private readonly IClassService _classService;
    private readonly IDataService _dataService;
    private readonly ILogger<AddClass> _logger;

    public AddClass(ILogger<AddClass> logger, IClassService classService, IDataService dataService)
    {
        _logger = logger;
        _classService = classService;
        _dataService = dataService;
    }

    [Function("AddClass")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation($"Function Triggered: AddClass at {DateTime.UtcNow}");

            // Resolve the path to the classes.txt file
            var classesFilePath = _dataService.GetFullFilePath("classes.txt");
            _logger.LogInformation($"Resolved file path: {classesFilePath}");

            // Read the existing content of the file
            var jsonData = await File.ReadAllTextAsync(classesFilePath);
            var classes = JsonConvert.DeserializeObject<List<ClassModel>>(jsonData) ?? new List<ClassModel>();
            _logger.LogInformation("Classes List Created");

            // Read the request body
            var requestBody = await req.ReadAsStringAsync();

            // Deserialize the request body to a ClassModel
            var newClass = JsonConvert.DeserializeObject<ClassModel>(requestBody!);

            // Validate that the new class is not null
            _classService.ValidateNewClass(newClass!);

            // Add the new class using the class service
            var updatedClasses = _classService.AddClass(classes, newClass!);

            // Write updated data back to the file
            await _dataService.WriteFile(classesFilePath, updatedClasses);

            // Respond with success
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Class added successfully!");
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