using Microsoft.EntityFrameworkCore;
using VideoUploader.Data.Database;
using VideoUploader.Models.Models;

namespace VideoUploader.Data.Repositories;

public class VideoAnalysisRepository(VideoUploaderContext context) : IVideoAnalysisRepository
{
    #region Properties

    private readonly VideoUploaderContext _context = context;

    #endregion

    #region Constructors
    #endregion

    #region Methods

    public async Task<VideoAnalysis> GetAnalysisStatus(Guid id)
    {
        return await _context.VideoAnalyses.FindAsync(id);
    }

    public async Task<IEnumerable<QrCodeData>> GetQrCodeDataAsync(Guid id)
    {
        return await _context.QrCodeDatas.Where(q => q.VideoAnalysisId == id).ToListAsync();
    }

    public async Task SaveAnalysisStatus(VideoAnalysis videoAnalysis)
    {
        _context.VideoAnalyses.Add(videoAnalysis);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAnalysisStatus(VideoAnalysis videoAnalysis)
    {
        _context.VideoAnalyses.Update(videoAnalysis);
        await _context.SaveChangesAsync();
    }

    public async Task SaveListQrCodeData(List<QrCodeData> listQrCodeData)
    {
        await _context.BulkInsertAsync(listQrCodeData);
    }

    #endregion
}