using AssignmentTracker.Model;
using AssignmentTracker.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssignmentTracker.Test.Services;

public class ClassServiceTests
{
    private readonly ClassService _classService;
    private readonly Mock<ILogger<ClassService>> _logger;

    public ClassServiceTests()
    {
        _logger = new Mock<ILogger<ClassService>>();
        _classService = new ClassService(_logger.Object);
    }

    [Test]
    public void AddClass_AddsNewClass_WithCorrectDetails()
    {
        // Arrange
        var classes = new List<ClassModel>();
        var newClass = new ClassModel
        {
            ClassName = "Math 101",
            ClassDesc = "Introduction to Mathematics",
            ProfessorName = "Dr. John Doe"
        };

        // Act
        var result = _classService.AddClass(classes, newClass);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var addedClass = result.First();
        Assert.That(addedClass.ClassId, Is.EqualTo(1));
        Assert.That(addedClass.ClassName, Is.EqualTo(newClass.ClassName));
        Assert.That(addedClass.ClassDesc, Is.EqualTo(newClass.ClassDesc));
        Assert.That(addedClass.ProfessorName, Is.EqualTo(newClass.ProfessorName));

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AddClass function accessed at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public void AddClass_ThrowsException_AndLogsError_WhenErrorOccurs()
    {
        // Arrange
        List<ClassModel> classes = null; // Force an error by passing a null list
        var newClass = new ClassModel
        {
            ClassName = "Math 101",
            ClassDesc = "Introduction to Mathematics",
            ProfessorName = "Dr. John Doe"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _classService.AddClass(classes, newClass));
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
    public void GetClass_Returns_ClassList_When_NotEmpty()
    {
        // Arrange
        var classes = new List<ClassModel>
        {
            new()
            {
                ClassId = 1, ClassName = "Math 101", ClassDesc = "Introduction to Mathematics",
                ProfessorName = "Dr. John Doe"
            }
        };

        // Act
        var result = _classService.GetClass(classes);

        // Assert
        Assert.That(result, Is.EqualTo(classes));
    }

    [Test]
    public void GetClass_Throws_ArgumentNullException_When_ListIsEmpty()
    {
        // Arrange
        var classes = new List<ClassModel>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _classService.GetClass(classes));
        Assert.That(ex.Message, Contains.Substring("The classes list cannot be null"));
    }

    [Test]
    public void ValidateNewClass_ThrowsException_When_ClassIsNull()
    {
        // Arrange
        ClassModel nullClass = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _classService.ValidateNewClass(nullClass));
        Assert.That(ex.Message, Contains.Substring("Empty or null class entered."));
    }

    [Test]
    public void ValidateNewClass_DoesNotThrowException_When_ClassIsValid()
    {
        // Arrange
        var validClass = new ClassModel
        {
            ClassId = 1,
            ClassName = "Math 101",
            ClassDesc = "Introduction to Mathematics",
            ProfessorName = "Dr. John Doe"
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _classService.ValidateNewClass(validClass));
    }

    [Test]
    public void NewId_ReturnsCorrectId_When_ListIsNotEmpty()
    {
        // Arrange
        var classes = new List<ClassModel>
        {
            new() { ClassId = 1, ClassName = "Math 101" }
        };

        // Act
        var newId = _classService.AddClass(classes, new ClassModel { ClassName = "Science 101" }).Last().ClassId;

        // Assert
        Assert.That(newId, Is.EqualTo(2));
    }

    [Test]
    public void NewId_ReturnsOne_When_ListIsEmpty()
    {
        // Arrange
        var classes = new List<ClassModel>();

        // Act
        var newId = _classService.AddClass(classes, new ClassModel { ClassName = "Science 101" }).Last().ClassId;

        // Assert
        Assert.That(newId, Is.EqualTo(1));
    }
}