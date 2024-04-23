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
    public ContentController(IContentsManager manager)
    {
        _manager = manager;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetManyContents()
    {
        var contents = await _manager.GetManyContents().ConfigureAwait(false);

        if (!contents.Any())
            return NotFound();
        
        return Ok(contents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContent(Guid id)
    {
        var content = await _manager.GetContent(id).ConfigureAwait(false);

        if (content == null)
            return NotFound();
        
        return Ok(content);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateContent(
        [FromBody] ContentInput content
        )
    {
        var createdContent = await _manager.CreateContent(content.ToDto()).ConfigureAwait(false);

        return createdContent == null ? Problem() : Ok(createdContent);
    }
    
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateContent(
        Guid id,
        [FromBody] ContentInput content
        )
    {
        var updatedContent = await _manager.UpdateContent(id, content.ToDto()).ConfigureAwait(false);

        return updatedContent == null ? NotFound() : Ok(updatedContent);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(
        Guid id
    )
    {
        var deletedId = await _manager.DeleteContent(id).ConfigureAwait(false);
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
            return NotFound();
        
        var currentGenres = currentContent.GenreList.ToHashSet(); // using a set to ensure no duplicates, duplicates get ignored
        currentGenres.UnionWith(genre);
        
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
            return Problem();
        
        // if unchanged return 304
        if (updatedContent.GenreList.Count() == currentContent.GenreList.Count())
            return StatusCode((int)HttpStatusCode.NotModified);
        
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
            return NotFound();

        var currentGenres = currentContent.GenreList;
        var genreToRemove = genre.ToList();
        var updatedGenres = currentGenres.Except(genreToRemove).ToList(); // removing genres, when removing genres that aren't there those are ignored
        
        if (!updatedGenres.Any() && genreToRemove.Any()) // if removed all genres return 304, can't remove all genres, the ContentMapper requires at least one genre
            return StatusCode((int)HttpStatusCode.NotModified);
        
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
            return Problem();
        
        // if unchanged return 304
        if (updatedContent.GenreList.Count() == currentContent.GenreList.Count())
            return StatusCode((int)HttpStatusCode.NotModified);
        
        return Ok(updatedContent);
    }
}