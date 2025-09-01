using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VideoUploader.Models.Configurations;
using VideoUploader.Models.Models;

namespace VideoUploader.Data.Repositories;

public class VideoAnalysisMongoRepository : IVideoAnalysisMongoRepository
{
    #region Properties

    private readonly IMongoCollection<VideoAnalysis> _videoAnalysesCollection;

    #endregion

    #region Constructors

    public VideoAnalysisMongoRepository(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _videoAnalysesCollection = mongoDatabase.GetCollection<VideoAnalysis>(mongoDbSettings.Value.CollectionName);
    }

    #endregion

    #region Methods

    public async Task CreateAsync(VideoAnalysis analysis)
    {
        await _videoAnalysesCollection.InsertOneAsync(analysis);
    }

    public async Task<VideoAnalysis?> GetByIdAsync(Guid id)
    {
        return await _videoAnalysesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(VideoAnalysis analysis)
    {
        await _videoAnalysesCollection.ReplaceOneAsync(x => x.Id == analysis.Id, analysis);
    }

    #endregion
}