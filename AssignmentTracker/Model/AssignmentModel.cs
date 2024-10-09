namespace AssignmentTracker.Model;

public class AssignmentModel
{
    public int AssignmentId { get; set; }
    
    public int ClassId { get; set; }
    
    public string AssignmentName { get; set; }
    
    public string AssignmentDesc { get; set; }
    
    public DateTime AssignmentDate { get; set; }
    
    public Boolean IsCompleted { get; set; }
}