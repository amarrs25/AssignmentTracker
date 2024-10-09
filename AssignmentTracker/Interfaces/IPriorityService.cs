using AssignmentTracker.Model;

namespace AssignmentTracker.Interfaces;

public interface IPriorityService
{
    List<PriorityModel> AddPriority(List<PriorityModel> priorities, PriorityModel priority);
}