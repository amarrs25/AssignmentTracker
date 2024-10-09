using AssignmentTracker.Model;

namespace AssignmentTracker.Interfaces;

public interface IAssignmentService
{
    List<AssignmentModel> AddAssignment(List<AssignmentModel> assignments, AssignmentModel assignment);
}