using AssignmentTracker.Model;

namespace AssignmentTracker.Interfaces;

public interface IAssignmentService
{
    List<AssignmentModel> AddAssignment(List<AssignmentModel> assignments, AssignmentModel newAssignment);
    List<AssignmentModel> GetAssignment(List<AssignmentModel> assignments);
    void ValidateNewAssignment(AssignmentModel newAssignment);
}