using AssignmentTracker.Model;

namespace AssignmentTracker.Interfaces;

public interface IPriorityService
{
    List<PriorityModel> AddPriority(List<PriorityModel> priorities, PriorityModel priority);
    List<PriorityModel> GetPriority(List<PriorityModel> priorities);
    void ValidateNewPriority(PriorityModel priority);
}