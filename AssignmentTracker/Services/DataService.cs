using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AssignmentTracker.Services;

public class DataService : IDataService
{
    private readonly string _rootPath;
    private readonly ILogger<DataService> _logger;

    public DataService(ILogger<DataService> logger)
    {
        _logger = logger;
        
        // Assume project root is two levels up from the base directory (e.g., bin/Debug/net8.0)
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    }
    
     public string GetFullFilePath(string relativePath)
     {
         var prioritiesFilePath = Path.Combine(_rootPath, relativePath);
         
         // Check if the file exists
         if (!File.Exists(prioritiesFilePath))
         {
             _logger.LogWarning("Priorities file not found.");
             throw new ArgumentNullException(nameof(prioritiesFilePath), "Priorities file not found.");
         }
         
        // Combine the root path with the relative path to access files in the root directory.
        return prioritiesFilePath;
    }

     public async Task<string> ReadFile(string filePath)
     {
         // Read the content of the .txt file
         var fileContent = await File.ReadAllTextAsync(filePath);
         _logger.LogInformation($"Read contents of {Path.GetFileName(filePath)}");
         _logger.LogInformation(fileContent);
         
         return fileContent;
     }
     
     public async Task WriteFile<T>(string filePath, List<T> list)
     {
         var updatedJsonData = JsonConvert.SerializeObject(list, Formatting.Indented);
         await File.WriteAllTextAsync(filePath, updatedJsonData);
         _logger.LogInformation($"Wrote contents of {Path.GetFileName(filePath)}");
     }
}