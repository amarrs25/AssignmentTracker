using AssignmentTracker.Model;
using AssignmentTracker.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssignmentTracker.Test.Services;

public class AssignmentServiceTests
{
    private readonly AssignmentService _assignmentService;
    private readonly Mock<ILogger<AssignmentService>> _logger;

    public AssignmentServiceTests()
    {
        _logger = new Mock<ILogger<AssignmentService>>();
        _assignmentService = new AssignmentService(_logger.Object);
    }

    [Test]
    public void AddAssignment_AddsNewAssignment_WithCorrectDetails()
    {
        // Arrange
        var assignments = new List<AssignmentModel>();
        var newAssignment = new AssignmentModel
        {
            ClassId = 1,
            AssignmentName = "Math Homework",
            AssignmentDesc = "Complete chapters 1-3",
            AssignmentDate = DateTime.UtcNow,
            IsCompleted = false
        };

        // Act
        var result = _assignmentService.AddAssignment(assignments, newAssignment);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var addedAssignment = result.First();
        Assert.That(addedAssignment.AssignmentId, Is.EqualTo(1));
        Assert.That(addedAssignment.AssignmentName, Is.EqualTo(newAssignment.AssignmentName));
        Assert.That(addedAssignment.ClassId, Is.EqualTo(newAssignment.ClassId));
        Assert.That(addedAssignment.AssignmentDesc, Is.EqualTo(newAssignment.AssignmentDesc));
        Assert.That(addedAssignment.AssignmentDate, Is.EqualTo(newAssignment.AssignmentDate));
        Assert.That(addedAssignment.IsCompleted, Is.EqualTo(newAssignment.IsCompleted));

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AddAssignment function accessed at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public void AddAssignment_ThrowsException_AndLogsError_WhenErrorOccurs()
    {
        // Arrange
        List<AssignmentModel> assignments = null; // Force an error by passing a null list
        var newAssignment = new AssignmentModel
        {
            ClassId = 1,
            AssignmentName = "Math Homework",
            AssignmentDesc = "Complete chapters 1-3"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => _assignmentService.AddAssignment(assignments, newAssignment));
        Assert.That(ex.Message, Contains.Substring("Value cannot be null"));

        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public void GetAssignment_Returns_AssignmentList_When_NotEmpty()
    {
        // Arrange
        var assignments = new List<AssignmentModel>
        {
            new()
            {
                AssignmentId = 1, ClassId = 1, AssignmentName = "Math Homework",
                AssignmentDesc = "Complete chapters 1-3", AssignmentDate = DateTime.UtcNow, IsCompleted = false
            }
        };

        // Act
        var result = _assignmentService.GetAssignment(assignments);

        // Assert
        Assert.That(result, Is.EqualTo(assignments));
    }

    [Test]
    public void GetAssignment_Throws_ArgumentNullException_When_ListIsEmpty()
    {
        // Arrange
        var assignments = new List<AssignmentModel>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _assignmentService.GetAssignment(assignments));
        Assert.That(ex.Message, Contains.Substring("The assignments list cannot be null"));
    }

    [Test]
    public void ValidateNewAssignment_ThrowsException_When_AssignmentIsNull()
    {
        // Arrange
        AssignmentModel nullAssignment = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _assignmentService.ValidateNewAssignment(nullAssignment));
        Assert.That(ex.Message, Contains.Substring("Empty or null assignment entered."));
    }

    [Test]
    public void ValidateNewAssignment_DoesNotThrowException_When_AssignmentIsValid()
    {
        // Arrange
        var validAssignment = new AssignmentModel
        {
            AssignmentId = 1,
            ClassId = 1,
            AssignmentName = "Math Homework",
            AssignmentDesc = "Complete chapters 1-3",
            AssignmentDate = DateTime.UtcNow,
            IsCompleted = false
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _assignmentService.ValidateNewAssignment(validAssignment));
    }

    [Test]
    public void NewId_ReturnsCorrectId_When_ListIsNotEmpty()
    {
        // Arrange
        var assignments = new List<AssignmentModel>
        {
            new() { AssignmentId = 1, ClassId = 1, AssignmentName = "Math Homework" }
        };

        // Act
        var newId = _assignmentService
            .AddAssignment(assignments, new AssignmentModel { ClassId = 2, AssignmentName = "Science Homework" }).Last()
            .AssignmentId;

        // Assert
        Assert.That(newId, Is.EqualTo(2));
    }

    [Test]
    public void NewId_ReturnsOne_When_ListIsEmpty()
    {
        // Arrange
        var assignments = new List<AssignmentModel>();

        // Act
        var newId = _assignmentService
            .AddAssignment(assignments, new AssignmentModel { ClassId = 2, AssignmentName = "Science Homework" }).Last()
            .AssignmentId;

        // Assert
        Assert.That(newId, Is.EqualTo(1));
    }
}