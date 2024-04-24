using Microsoft.Extensions.Caching.Memory;
using NOS.Engineering.Challenge.Database;
using NOS.Engineering.Challenge.Models;

namespace NOS.Engineering.Challenge.Managers;

public class ContentsManager : IContentsManager
{
    private readonly IDatabase<Content?, ContentDto> _database;
    private readonly IMemoryCache _memoryCache;

    public ContentsManager(IDatabase<Content?, ContentDto> database, IMemoryCache memoryCache)
    {
        _database = database;
        _memoryCache = memoryCache;
    }

    public async Task<IEnumerable<Content?>> GetManyContents()
    {
        var all  = await _database.ReadAll();
        
        foreach (var content in all)
        {
            _memoryCache.Set(content.Id , content, 
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(30))
            );
        }
        
        return all;
    }

    public async Task<Content?> CreateContent(ContentDto content)
    {
        var createdContent = await _database.Create(content);
        
        _memoryCache.Set(createdContent.Id , createdContent, 
            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(30))
        );
        
        return createdContent;
    }

    public async Task<Content?> GetContent(Guid id)
    {
        if (_memoryCache.TryGetValue(id, out Content? content))
        {
            return content;
        }
        
        var dbContent = await _database.Read(id);
        
        if (dbContent != null)
        {
            _memoryCache.Set(dbContent.Id , dbContent, 
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(30))
            );
        }
        
        return dbContent;
    }

    public async Task<Content?> UpdateContent(Guid id, ContentDto content)
    {
        var updatedContent = await _database.Update(id, content);
        
        if (updatedContent != null)
        {
            _memoryCache.Set(updatedContent.Id , updatedContent, 
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(30))
            );
        }
        
        return updatedContent;
    }

    public async Task<Guid> DeleteContent(Guid id)
    {
        _memoryCache.Remove(id);
        return await _database.Delete(id);
    }
}