using System.Net;
using Microsoft.AspNetCore.Mvc;
using NOS.Engineering.Challenge.API.Models;
using NOS.Engineering.Challenge.Managers;
using NOS.Engineering.Challenge.Models;

namespace NOS.Engineering.Challenge.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ContentController : Controller
{
    private readonly IContentsManager _manager;
    private readonly ILogger _logger;
    public ContentController(IContentsManager manager, ILogger<ContentController> logger)
    {
        _manager = manager;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetManyContents()
    {
        var contents = await _manager.GetManyContents().ConfigureAwait(false);
        contents = contents.ToList();
        
        if (!contents.Any())
        {
            _logger.LogWarning("GET:/api/v1/content - StatusCode:404 - no contents found");
            return NotFound();
        }
        
        //_logger.LogInformation($"GET:/api/v1/content - StatusCode:200 - returned {contents.Count()} contents");
        return Ok(contents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContent(Guid id)
    {
        var content = await _manager.GetContent(id).ConfigureAwait(false);

        if (content == null)
        {
            _logger.LogWarning($"GET:/api/v1/content/{id} - StatusCode:404 - id:{id} does not exist");
            return NotFound();
        }

        //_logger.LogInformation($"GET:/api/v1/content/{id} - StatusCode:200 - returned content title:{content.Title}");
        return Ok(content);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateContent(
        [FromBody] ContentInput content
        )
    {
        var createdContent = await _manager.CreateContent(content.ToDto()).ConfigureAwait(false);

        if (createdContent == null)
        {
            _logger.LogError($"POST:/api/v1/content - StatusCode:500 - failed to create content {content.Title}"); // maybe output the content object here
            return Problem();
        }
        
        _logger.LogInformation($"POST:/api/v1/content - StatusCode:200 - created content id:{createdContent.Id}, title:{createdContent.Title}");
        return Ok(createdContent);
    }
    
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateContent(
        Guid id,
        [FromBody] ContentInput content
        )
    {
        var updatedContent = await _manager.UpdateContent(id, content.ToDto()).ConfigureAwait(false);
        
        if (updatedContent == null)
        {
            _logger.LogWarning($"PATCH:/api/v1/content/{id} - StatusCode:404 - failed to update content, id:{id} not found");
            return NotFound();
        }

        _logger.LogInformation($"PATCH:/api/v1/content/{id} - StatusCode:200 - updated content id:{updatedContent.Id}, title:{updatedContent.Title}");
        return Ok(updatedContent);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(
        Guid id
    )
    {
        var deletedId = await _manager.DeleteContent(id).ConfigureAwait(false);
        _logger.LogInformation($"DELETE:/api/v1/content/{id} - StatusCode:200 - deleted content id:{deletedId}");
        return Ok(deletedId);
    }
    
    [HttpPost("{id}/genre")]
    public async Task<IActionResult> AddGenres(
        Guid id,
        [FromBody] IEnumerable<string> genre
    )
    {
        var currentContent = await _manager.GetContent(id).ConfigureAwait(false);

        if (currentContent == null) // if content not found return 404
        {
            _logger.LogWarning($"POST:/api/v1/content/{id}/genre - StatusCode:404 - failed to add genres, content id:{id} not found");
            return NotFound(); 
        } 
        
        var currentGenres = currentContent.GenreList.ToHashSet(); // using a set to ensure no duplicates, duplicates get ignored
        var newGenres = genre.ToList();
        currentGenres.UnionWith(newGenres);
        
        var updatedContent = await _manager.UpdateContent(id, new ContentDto(
            currentContent.Title,
            currentContent.SubTitle,
            currentContent.Description,
            currentContent.ImageUrl,
            currentContent.Duration,
            currentContent.StartTime,
            currentContent.EndTime,
            currentGenres)
        ).ConfigureAwait(false);
        
        if (updatedContent == null) // if update failed return 500
        { 
            _logger.LogError($"POST:/api/v1/content/{id}/genre - StatusCode:500 - failed to add genres, update content id:{id} failed");
            return Problem(); 
        }
        
        // if unchanged return 304
        if (updatedContent.GenreList.Count() == currentContent.GenreList.Count())
        {
            _logger.LogInformation($"POST:/api/v1/content/{id}/genre - StatusCode:304 - no new genres added, content genres unchanged");
            return StatusCode((int)HttpStatusCode.NotModified);
        }

        _logger.LogInformation($"POST:/api/v1/content/{id}/genre - StatusCode:200 - added genres [{string.Join(", ", newGenres)}] to content id:{id}");
        return Ok(updatedContent);
    }
    
    [HttpDelete("{id}/genre")]
    public async Task<IActionResult> RemoveGenres(
        Guid id,
        [FromBody] IEnumerable<string> genre
    )
    {
        var currentContent = await _manager.GetContent(id).ConfigureAwait(false);

        if (currentContent == null) // if content not found return 404
        {
            _logger.LogWarning($"DELETE:/api/v1/content/{id}/genre - StatusCode:404 - failed to remove genres, content id:{id} not found");
            return NotFound();
        }

        var currentGenres = currentContent.GenreList;
        var genreToRemove = genre.ToList();
        var updatedGenres = currentGenres.Except(genreToRemove).ToList(); // removing genres, when removing genres that aren't there those are ignored

        if (!updatedGenres.Any() &&
            genreToRemove
                .Any()) // if removed all genres return 304, can't remove all genres, the ContentMapper requires at least one genre
        {
            _logger.LogWarning($"DELETE:/api/v1/content/{id}/genre - StatusCode:304 - failed to remove genres, content id:{id} must have at least one genre");
            return StatusCode((int)HttpStatusCode.NotModified);
        }
        
        var updatedContent = await _manager.UpdateContent(id, new ContentDto(
            currentContent.Title,
            currentContent.SubTitle,
            currentContent.Description,
            currentContent.ImageUrl,
            currentContent.Duration,
            currentContent.StartTime,
            currentContent.EndTime,
            updatedGenres)
        ).ConfigureAwait(false);

        if (updatedContent == null) // if update failed return 500
        {
            _logger.LogError($"DELETE:/api/v1/content/{id}/genre - StatusCode:500 - failed to remove genres, update content id:{id} failed");
            return Problem();
        }
        
        // if unchanged return 304
        if (updatedContent.GenreList.Count() == currentContent.GenreList.Count())
        {
            _logger.LogInformation($"DELETE:/api/v1/content/{id}/genre - StatusCode:304 - no genres removed, content genres unchanged");
            return StatusCode((int)HttpStatusCode.NotModified);
        }
        
        _logger.LogInformation($"DELETE:/api/v1/content/{id}/genre - StatusCode:200 - removed genres [{string.Join(", ", genreToRemove)}] from content id:{id}");
        return Ok(updatedContent);
    }
}