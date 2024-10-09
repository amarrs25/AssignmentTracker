using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Extensions.Logging;

namespace AssignmentTracker.Services;

public class PriorityService: IPriorityService
{
    private readonly ILogger<PriorityService> _logger;
    public PriorityService(ILogger<PriorityService> logger)
    {
        _logger = logger;
    }

    public List<PriorityModel> AddPriority(List<PriorityModel> priorities, PriorityModel newPriority)
    {
        try
        {
            _logger.LogInformation($"AddPriority function accessed at: {DateTime.Now}");

            newPriority.PriorityId = NewId(priorities);
            newPriority.PriorityName = NewPriority(newPriority.PriorityType);
            
            _logger.LogInformation($" \n ClassId value: {newPriority.PriorityId}" +
                                   $" \n ClassName value: {newPriority.AssignmentId}" +
                                   $" \n ClassDesc value: {newPriority.PriorityName}" +
                                   $" \n ProfessorName value: {newPriority.PriorityType}");

            var result = new PriorityModel
            {
                PriorityId = newPriority.PriorityId,
                AssignmentId = newPriority.AssignmentId,
                PriorityName = newPriority.PriorityName,
                PriorityType = newPriority.PriorityType
            };
            priorities.Add(result);
            
            _logger.LogInformation($"AddPriority completed: {DateTime.Now} \n Result: {result}");
            
            return priorities;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }
    
    private int NewId(List<PriorityModel> priorities)
    {
        var maxId = priorities.Any() ? priorities.Max(p => int.Parse(p.PriorityId.ToString())) : 0;
        var newId = maxId + 1;
        
        return newId;
    }

    private string NewPriority(int priorityType)
    {
        if (priorityType == 0)
            return "None";
        else if (priorityType == 1)
            return "Low";
        else if (priorityType == 2)
            return "Medium";
        else if (priorityType == 3)
            return "High";
        else
            return "No priority selected";
    }
}