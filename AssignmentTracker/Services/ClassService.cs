using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Extensions.Logging;

namespace AssignmentTracker.Services;

public class ClassService : IClassService
{
    private readonly ILogger<ClassService> _logger;

    public ClassService(ILogger<ClassService> logger)
    {
        _logger = logger;
    }

    public List<ClassModel> AddClass(List<ClassModel> classes, ClassModel newClass)
    {
        try
        {
            _logger.LogInformation($"AddClass function accessed at: {DateTime.UtcNow}");

            // Generate a new ID for the class
            newClass.ClassId = NewId(classes);

            _logger.LogInformation($" \n ClassId value: {newClass.ClassId}" +
                                   $" \n ClassName value: {newClass.ClassName}" +
                                   $" \n ClassDesc value: {newClass.ClassDesc}" +
                                   $" \n ProfessorName value: {newClass.ProfessorName}");

            classes.Add(newClass);

            _logger.LogInformation($"AddClass completed: {DateTime.UtcNow} \n Result: {newClass}");

            return classes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }

    public List<ClassModel> GetClass(List<ClassModel> classes)
    {
        if (classes == null || classes.Count == 0)
        {
            _logger.LogError("Classes list is null or empty");
            throw new ArgumentNullException(nameof(classes), "The classes list cannot be null or empty.");
        }

        return classes;
    }

    public void ValidateNewClass(ClassModel newClass)
    {
        if (newClass == null)
        {
            _logger.LogError("Invalid class entered");
            throw new ArgumentNullException(nameof(newClass), "Empty or null class entered.");
        }
    }

    private int NewId(List<ClassModel> classes)
    {
        var maxId = classes.Any() ? classes.Max(c => c.ClassId) : 0;
        return maxId + 1;
    }
}