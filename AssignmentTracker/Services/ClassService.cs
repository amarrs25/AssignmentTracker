using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Extensions.Logging;

namespace AssignmentTracker.Services;

public class ClassService: IClassService
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
            _logger.LogInformation($"AddClass function accessed at: {DateTime.Now}");

            newClass.ClassId = NewId(classes);
            
            _logger.LogInformation($" \n ClassId value: {newClass.ClassId}" +
                                   $" \n ClassName value: {newClass.ClassName}" +
                                   $" \n ClassDesc value: {newClass.ClassDesc}" +
                                   $" \n ProfessorName value: {newClass.ProfessorName}");
            
            var result = new ClassModel
            {
                ClassId = newClass.ClassId,
                ClassName = newClass.ClassName,
                ClassDesc = newClass.ClassDesc,
                ProfessorName = newClass.ProfessorName
            };
            classes.Add(result);
            
            _logger.LogInformation($"AddClass completed: {DateTime.Now} \n Result: {result}");
            
            return classes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }
    
    private int NewId(List<ClassModel> classes)
    {
        var maxId = classes.Any() ? classes.Max(c => int.Parse(c.ClassId.ToString())) : 0;
        var newId = maxId + 1;
        
        return newId;
    }
}