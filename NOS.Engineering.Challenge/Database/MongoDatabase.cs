using Microsoft.VisualBasic;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace NOS.Engineering.Challenge.Database;

public class MongoDatabase<TOut, TIn> : IDatabase<TOut, TIn>
{
    
    private readonly IMapper<TOut?, TIn> _mapper;
    private readonly MongoClient _client;
    private readonly IMongoDatabase _moviesDB;
    private readonly IMongoCollection<BsonDocument> _moviesCollection;
    

    public MongoDatabase(IMapper<TOut?, TIn> mapper, IMockData<TOut> mockData)
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");
        if (connectionString == null)
        {
            Console.WriteLine("You must set your 'MONGODB_URI' environment variable. To learn how to set it, see https://www.mongodb.com/docs/drivers/csharp/current/quick-start/#set-your-connection-string");
            Environment.Exit(0);
        }
        
        _mapper = mapper;
        _client = new MongoClient(connectionString);
        _moviesDB = _client.GetDatabase("moviesDB");
        _moviesCollection = _moviesDB.GetCollection<BsonDocument>("movies");

        var mocks = mockData.GenerateMocks();
        foreach (var mock in mocks)
        {
            var doc = mock.Value.ToBsonDocument();
            doc["_id"] = mock.Key.ToString();
            _moviesCollection.InsertOne(doc);
        }
    }

    public async Task<TOut?> Create(TIn item)
    {
        var id = Guid.NewGuid();
        var createdItem = _mapper.Map(id, item);
        var exists = _moviesCollection.Find(d => d["_id"] == id.ToString()).Any();
        if (exists)
        {
            throw new Exception("Could not add content to database");
        }
        var doc = createdItem.ToBsonDocument();
        doc["_id"] = id.ToString();
        await _moviesCollection.InsertOneAsync(doc);
        return createdItem;
    }

    public async Task<TOut?> Read(Guid id)
    {
        var doc = await _moviesCollection.Find(d => d["_id"] == id.ToString()).FirstOrDefaultAsync();
        if (doc == null)
        {
            return default;
        }
        var item = BsonSerializer.Deserialize<TOut?>(doc);
        return item;
    }

    public async Task<IEnumerable<TOut?>> ReadAll()
    {
        var docs = await _moviesCollection.Find(_ => true).ToListAsync();
        var items = new List<TOut?>();
        foreach (var doc in docs)
        {
            items.Add(BsonSerializer.Deserialize<TOut?>(doc));
        }
        return items.AsEnumerable();
    }

    public async Task<TOut?> Update(Guid id, TIn item)
    {
        var doc = await _moviesCollection.Find(d => d["_id"] == id.ToString()).FirstOrDefaultAsync();
        if (doc == null)
        {
            return default;
        }
        var dbItem = BsonSerializer.Deserialize<TOut?>(doc);
        var updatedItem = _mapper.Patch(dbItem, item);
        var updatedDoc = updatedItem.ToBsonDocument();
        updatedDoc["_id"] = id.ToString();
        await _moviesCollection.ReplaceOneAsync(d => d["_id"] == id.ToString(), updatedDoc);
        return updatedItem;
    }

    public async Task<Guid> Delete(Guid id)
    {
        var exists = await _moviesCollection.Find(d => d["_id"] == id.ToString()).AnyAsync();
        if (!exists)
        {
            return Guid.Empty;
        }
        await _moviesCollection.DeleteOneAsync(d => d["_id"] == id.ToString());
        return id;
    }
}