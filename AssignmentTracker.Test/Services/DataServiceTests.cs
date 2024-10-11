using AssignmentTracker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace AssignmentTracker.Test.Services;

public class DataServiceTests
{
    private readonly DataService _dataService;
    private readonly Mock<ILogger<DataService>> _logger;
    private readonly string _rootPath;

    public DataServiceTests()
    {
        _logger = new Mock<ILogger<DataService>>();
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        _dataService = new DataService(_logger.Object);
    }

    [Test]
    public void GetFullFilePath_Returns_FullPath_When_FileExists()
    {
        // Arrange
        var filePath = "existingfile.txt";
        var fullFilePath =
            "C:\\Users\\aaron\\RiderProjects\\AssignmentTracker\\AssignmentTracker.Test\\existingfile.txt";

        // Mock the file existence check to simulate that the file exists
        File.Create(fullFilePath).Dispose(); // Create the file temporarily for the test

        try
        {
            // Act
            var result = _dataService.GetFullFilePath(filePath);

            // Assert
            Assert.That(result, Is.EqualTo(fullFilePath));
        }
        finally
        {
            // Cleanup - delete the file after the test
            File.Delete(fullFilePath);
        }
    }

    [Test]
    public void GetFullFilePath_Throws_ArgumentNullException_When_FileDoesNotExist()
    {
        // Arrange
        var relativePath = "nonexistentfile.txt";
        var fullFilePath = Path.Combine(_rootPath, relativePath);

        // Ensure the file does not exist
        if (File.Exists(fullFilePath)) File.Delete(fullFilePath);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _dataService.GetFullFilePath(relativePath));

        // Verify that the exception message contains the expected file name
        Assert.That(exception.Message, Contains.Substring("nonexistentfile.txt file not found."));

        // Verify that a warning was logged
        _logger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("nonexistentfile.txt file not found.")),
                null,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task ReadFile_Returns_Content_When_FileExists()
    {
        // Arrange
        var filePath = Path.Combine(_rootPath, "testfile.txt");
        var expectedContent = "This is a test file content.";

        // Create a temporary file with the expected content
        await File.WriteAllTextAsync(filePath, expectedContent);

        try
        {
            // Act
            var result = await _dataService.ReadFile(filePath);

            // Assert
            Assert.That(result, Is.EqualTo(expectedContent));
            _logger.Verify(logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Read contents of testfile.txt")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        finally
        {
            // Cleanup - delete the file after the test
            File.Delete(filePath);
        }
    }

    [Test]
    public void ReadFile_Throws_Exception_When_FileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_rootPath, "nonexistentfile.txt");

        // Ensure the file does not exist
        if (File.Exists(filePath)) File.Delete(filePath);

        // Act & Assert
        var exception = Assert.ThrowsAsync<FileNotFoundException>(async () => await _dataService.ReadFile(filePath));
        Assert.That(exception.Message, Contains.Substring("nonexistentfile.txt"));
    }

    [Test]
    public async Task WriteFile_Writes_Content_To_File()
    {
        // Arrange
        var filePath = Path.Combine(_rootPath, "outputfile.txt");
        var list = new List<string> { "item1", "item2", "item3" };
        var expectedJson = JsonConvert.SerializeObject(list, Formatting.Indented);

        try
        {
            // Act
            await _dataService.WriteFile(filePath, list);

            // Assert
            var writtenContent = await File.ReadAllTextAsync(filePath);
            Assert.That(writtenContent, Is.EqualTo(expectedJson));
            _logger.Verify(logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Wrote contents of outputfile.txt")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        finally
        {
            // Cleanup - delete the file after the test
            File.Delete(filePath);
        }
    }
}