using AssignmentTracker.Model;

namespace AssignmentTracker.Interfaces;

public interface IClassService
{
    List<ClassModel> AddClass(List<ClassModel> classes, ClassModel classModel); //class not allowed
    List<ClassModel> GetClass(List<ClassModel> classes);
    void ValidateNewClass(ClassModel newClass);
}