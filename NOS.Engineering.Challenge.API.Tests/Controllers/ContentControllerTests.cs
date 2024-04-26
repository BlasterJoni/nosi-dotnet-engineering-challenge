using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NOS.Engineering.Challenge.API.Controllers;
using NOS.Engineering.Challenge.Managers;
using NOS.Engineering.Challenge.Models;
using Xunit.Abstractions;

namespace NOS.Engineering.Challenge.API.Tests.Controllers;

public class ContentControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ContentControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task GetFilteredContents_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var contents = new List<Content>
        {
            new Content
            (
                id,
                "Title",
                "SubTitle",
                "Description",
                "ImageUrl",
                1,
                DateTime.Now,
                DateTime.Now,
                new List<string> { "Genre" }
            )
        };
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetManyContents()).ReturnsAsync(contents);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.GetFilteredContents("Genre", "Title");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Content>>(okResult.Value);
        var content = Assert.Single(model);
        Assert.Contains("Genre", content.GenreList);
        Assert.Contains("Title", content.Title);
        Assert.Equal(id, content.Id);
    }
    
    [Fact]
    public async Task GetFilteredContents_ReturnsNotFound()
    {
        // Arrange
        var contents = new List<Content>();
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetManyContents()).ReturnsAsync(contents);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.GetFilteredContents("Genre", "Title");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task AddGenres_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var content = new Content
        (
            id,
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre" }
        );
        var content2 = new Content
        (
            id,
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre", "Genre2" }
        );
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetContent(id)).ReturnsAsync(content);
        mockManager.Setup(m => m.UpdateContent(id, It.IsAny<ContentDto>())).ReturnsAsync(content2);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.AddGenres(id, new List<string> { "Genre2" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<Content>(okResult.Value);
        Assert.Equal(new HashSet<string> { "Genre", "Genre2" }, model.GenreList.ToHashSet());
    }
    
    [Fact]
    public async Task AddGenres_ReturnsNotFound()
    {
        // Arrange
        var mockManager = new Mock<IContentsManager>();
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.AddGenres(Guid.NewGuid(), new List<string> { "Genre" });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddGenres_ReturnsUnmodified()
    {
        // Arrange
        var content = new Content
        (
            Guid.NewGuid(),
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre" }
        );
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetContent(It.IsAny<Guid>())).ReturnsAsync(content);
        mockManager.Setup(m => m.UpdateContent(It.IsAny<Guid>(), It.IsAny<ContentDto>())).ReturnsAsync(content);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.AddGenres(Guid.NewGuid(), new List<string> { "Genre" });

        // Assert
        var objectResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(304, objectResult.StatusCode);
    }

    [Fact]
    public async Task RemoveGenres_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var content = new Content
        (
            id,
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre", "Genre2" }
        );
        var content2 = new Content
        (
            id,
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre" }
        );
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetContent(id)).ReturnsAsync(content);
        mockManager.Setup(m => m.UpdateContent(id, It.IsAny<ContentDto>())).ReturnsAsync(content2);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.RemoveGenres(id, new List<string> { "Genre2" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<Content>(okResult.Value);
        Assert.Equal(new HashSet<string> { "Genre" }, model.GenreList.ToHashSet());
    }
    
    [Fact]
    public async Task RemoveGenres_ReturnsUnmodifiedNotPresent()
    {
        // Arrange
        var content = new Content
        (
            Guid.NewGuid(),
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre" }
        );
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetContent(It.IsAny<Guid>())).ReturnsAsync(content);
        mockManager.Setup(m => m.UpdateContent(It.IsAny<Guid>(), It.IsAny<ContentDto>())).ReturnsAsync(content);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.RemoveGenres(Guid.NewGuid(), new List<string> { "Genre2" });

        // Assert
        var objectResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(304, objectResult.StatusCode);
    }

    [Fact]
    public async Task RemoveGenres_ReturnsUnmodifiedCantBeZero()
    {
        // Arrange
        var content = new Content
        (
            Guid.NewGuid(),
            "Title",
            "SubTitle",
            "Description",
            "ImageUrl",
            1,
            DateTime.Now,
            DateTime.Now,
            new List<string> { "Genre" }
        );
        var mockManager = new Mock<IContentsManager>();
        mockManager.Setup(m => m.GetContent(It.IsAny<Guid>())).ReturnsAsync(content);
        mockManager.Setup(m => m.UpdateContent(It.IsAny<Guid>(), It.IsAny<ContentDto>())).ReturnsAsync(content);
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.RemoveGenres(Guid.NewGuid(), new List<string> { "Genre" });

        // Assert
        var objectResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(304, objectResult.StatusCode);
    }
    
    [Fact]
    public async Task RemoveGenres_ReturnsNotFound()
    {
        // Arrange
        var mockManager = new Mock<IContentsManager>();
        var mockLogger = new Mock<ILogger<ContentController>>();
        var controller = new ContentController(mockManager.Object, mockLogger.Object);

        // Act
        var result = await controller.RemoveGenres(Guid.NewGuid(), new List<string> { "Genre" });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}