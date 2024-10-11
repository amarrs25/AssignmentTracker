using AssignmentTracker.Interfaces;
using AssignmentTracker.Model;
using Microsoft.Extensions.Logging;

namespace AssignmentTracker.Services;

public class AssignmentService : IAssignmentService
{
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(ILogger<AssignmentService> logger)
    {
        _logger = logger;
    }

    public List<AssignmentModel> AddAssignment(List<AssignmentModel> assignments, AssignmentModel newAssignment)
    {
        try
        {
            _logger.LogInformation($"AddAssignment function accessed at: {DateTime.Now}");

            // Generate a new ID for the assignment
            newAssignment.AssignmentId = NewId(assignments);

            _logger.LogInformation($" \n AssignmentId value: {newAssignment.AssignmentId}" +
                                   $" \n ClassId value: {newAssignment.ClassId}" +
                                   $" \n AssignmentName value: {newAssignment.AssignmentName}" +
                                   $" \n AssignmentDesc value: {newAssignment.AssignmentDesc}" +
                                   $" \n AssignmentDate value: {newAssignment.AssignmentDate}" +
                                   $" \n IsCompleted value: {newAssignment.IsCompleted}");

            var result = new AssignmentModel
            {
                AssignmentId = newAssignment.AssignmentId,
                ClassId = newAssignment.ClassId,
                AssignmentName = newAssignment.AssignmentName,
                AssignmentDesc = newAssignment.AssignmentDesc,
                AssignmentDate = newAssignment.AssignmentDate,
                IsCompleted = newAssignment.IsCompleted
            };

            assignments.Add(result);

            _logger.LogInformation($"AddAssignment completed: {DateTime.Now} \n Result: {result}");

            return assignments;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }

    public List<AssignmentModel> GetAssignment(List<AssignmentModel> assignments)
    {
        if (assignments == null || assignments.Count == 0)
        {
            _logger.LogError("Assignments list is null or empty");
            throw new ArgumentNullException(nameof(assignments), "The assignments list cannot be null or empty.");
        }

        return assignments;
    }

    public void ValidateNewAssignment(AssignmentModel newAssignment)
    {
        if (newAssignment == null)
        {
            _logger.LogError("Invalid assignment entered");
            throw new ArgumentNullException(nameof(newAssignment), "Empty or null assignment entered.");
        }
    }

    private int NewId(List<AssignmentModel> assignments)
    {
        var maxId = assignments.Any() ? assignments.Max(a => a.AssignmentId) : 0;
        return maxId + 1;
    }
}