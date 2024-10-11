using AssignmentTracker.Model;

namespace AssignmentTracker.Interfaces;

public interface IDataService
{
     string GetFullFilePath(string relativePath);
     Task<string> ReadFile(string filePath);
     Task WriteFile<T>(string filePath, List<T> list);
}