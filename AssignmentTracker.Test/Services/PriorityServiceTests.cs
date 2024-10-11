using AssignmentTracker.Model;
using AssignmentTracker.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssignmentTracker.Test.Services;

public class PriorityServiceTests
{
    private readonly Mock<ILogger<PriorityService>> _logger;
    private readonly PriorityService _priorityService;

    public PriorityServiceTests()
    {
        _logger = new Mock<ILogger<PriorityService>>();
        _priorityService = new PriorityService(_logger.Object);
    }

    [Test]
    public void AddPriority_AddsNewPriority_WithCorrectDetails()
    {
        // Arrange
        var priorities = new List<PriorityModel>();
        var newPriority = new PriorityModel
        {
            AssignmentId = 1,
            PriorityType = 2
        };

        // Act
        var result = _priorityService.AddPriority(priorities, newPriority);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var addedPriority = result.First();
        Assert.That(addedPriority.PriorityId, Is.EqualTo(1));
        Assert.That(addedPriority.PriorityName, Is.EqualTo("Medium"));
        Assert.That(addedPriority.AssignmentId, Is.EqualTo(newPriority.AssignmentId));
        Assert.That(addedPriority.PriorityType, Is.EqualTo(newPriority.PriorityType));

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AddPriority function accessed at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Test]
    public void AddPriority_ThrowsException_AndLogsError_WhenErrorOccurs()
    {
        // Arrange
        List<PriorityModel> priorities = null; // Force an error by passing a null list
        var newPriority = new PriorityModel
        {
            AssignmentId = 1,
            PriorityType = 2
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _priorityService.AddPriority(priorities, newPriority));
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
    public void GetPriority_Returns_PriorityList_When_NotEmpty()
    {
        // Arrange
        var priorities = new List<PriorityModel>
        {
            new() { PriorityId = 1, AssignmentId = 1, PriorityName = "High", PriorityType = 3 }
        };

        // Act
        var result = _priorityService.GetPriority(priorities);

        // Assert
        Assert.That(result, Is.EqualTo(priorities));
    }

    [Test]
    public void GetPriority_Throws_ArgumentNullException_When_ListIsEmpty()
    {
        // Arrange
        var priorities = new List<PriorityModel>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _priorityService.GetPriority(priorities));
        Assert.That(ex.Message, Contains.Substring("The priorities list cannot be null"));
    }

    [Test]
    public void ValidateNewPriority_ThrowsException_When_PriorityIsNull()
    {
        // Arrange
        PriorityModel nullPriority = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _priorityService.ValidateNewPriority(nullPriority));
        Assert.That(ex.Message, Contains.Substring("Empty or null priority entered."));
    }

    [Test]
    public void ValidateNewPriority_DoesNotThrowException_When_PriorityIsValid()
    {
        // Arrange
        var validPriority = new PriorityModel
        {
            PriorityId = 1,
            AssignmentId = 1,
            PriorityType = 2,
            PriorityName = "Medium"
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _priorityService.ValidateNewPriority(validPriority));
    }

    [Test]
    public void NewId_ReturnsCorrectId_When_ListIsNotEmpty()
    {
        // Arrange
        var priorities = new List<PriorityModel>
        {
            new() { PriorityId = 1, AssignmentId = 1, PriorityType = 2 }
        };

        // Act
        var newId = _priorityService.AddPriority(priorities, new PriorityModel { AssignmentId = 2, PriorityType = 1 })
            .Last().PriorityId;

        // Assert
        Assert.That(newId, Is.EqualTo(2));
    }

    [Test]
    public void NewId_ReturnsOne_When_ListIsEmpty()
    {
        // Arrange
        var priorities = new List<PriorityModel>();

        // Act
        var newId = _priorityService.AddPriority(priorities, new PriorityModel { AssignmentId = 2, PriorityType = 1 })
            .Last().PriorityId;

        // Assert
        Assert.That(newId, Is.EqualTo(1));
    }

    [Test]
    public void NewPriority_ReturnsCorrectPriorityName_ForValidPriorityType()
    {
        // Arrange
        var priorityType = 2; // Medium priority

        // Act
        var priorityName = _priorityService.AddPriority(new List<PriorityModel>(),
            new PriorityModel { AssignmentId = 1, PriorityType = priorityType }).Last().PriorityName;

        // Assert
        Assert.That(priorityName, Is.EqualTo("Medium"));
    }

    [Test]
    public void NewPriority_ReturnsDefaultMessage_ForInvalidPriorityType()
    {
        // Arrange
        var invalidPriorityType = 999;

        // Act
        var priorityName = _priorityService.AddPriority(new List<PriorityModel>(),
            new PriorityModel { AssignmentId = 1, PriorityType = invalidPriorityType }).Last().PriorityName;

        // Assert
        Assert.That(priorityName, Is.EqualTo("No priority selected"));
    }
}