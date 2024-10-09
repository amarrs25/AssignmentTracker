using System.Net;
using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace  AssignmentTracker.API;

public class AddClass
{
    private readonly ILogger<AddClass> _logger;
    private readonly IClassService _classService;

    public AddClass(ILogger<AddClass> logger, IClassService classService)
    {
        _logger = logger;
        _classService = classService;
    }

    [Function("AddClass")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var classesFilePath = Path.Combine(AppContext.BaseDirectory, "../../../classes.txt");
        _logger.LogInformation($"Resolved file path: {classesFilePath}");

        try
        {
            // Read the existing content of the file
            var jsonData = await File.ReadAllTextAsync(classesFilePath);
            var classes = JsonConvert.DeserializeObject<List<ClassModel>>(jsonData) ?? new List<ClassModel>();

            // Add new class (example: deserialized from the request body)
            var requestBody = await req.ReadAsStringAsync();
            var newClass = JsonConvert.DeserializeObject<ClassModel>(requestBody);
            
            var updatedClasses = _classService.AddClass(classes, newClass);
            
            // Write updated data back to the file
            var updatedJsonData = JsonConvert.SerializeObject(updatedClasses, Formatting.Indented);
            await File.WriteAllTextAsync(classesFilePath, updatedJsonData);

            // Respond with success
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Class added successfully!");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while adding the class.");
            return errorResponse;
        }
    }
}
