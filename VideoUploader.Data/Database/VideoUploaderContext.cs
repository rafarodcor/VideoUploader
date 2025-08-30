using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using VideoUploader.Models.Models;

namespace VideoUploader.Data.Database;

public class VideoUploaderContext : DbContext
{
    #region Properties

    private readonly IConfiguration? _configuration;

    #endregion

    #region Constructors

    public VideoUploaderContext(IConfiguration configuration, DbContextOptions options) : base(options)
    {
        _configuration = configuration;
    }

    #endregion

    #region DbSets

    // DbSet para a entidade VideoAnalysis
    public DbSet<VideoAnalysis> VideoAnalyses { get; set; } = null!;

    // DbSet para a entidade QrCodeData
    public DbSet<QrCodeData> QrCodeDatas { get; set; } = null!;

    #endregion

    #region Methods

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VideoAnalysis>(video =>
        {
            video.Property(c => c.Id).IsRequired().ValueGeneratedOnAdd();
            video.Property(c => c.OriginalFileName).IsRequired().HasMaxLength(255);
            video.Property(c => c.Extension).IsRequired().HasMaxLength(10);
            video.Property(c => c.Status).IsRequired();
            video.Property(c => c.SubmittedAt).IsRequired().HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<QrCodeData>(qrCode =>
        {            
            qrCode.Property(c => c.Id).IsRequired().ValueGeneratedOnAdd();
            qrCode.Property(c => c.Content).IsRequired(false);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(_configuration?.GetConnectionString("DefaultConnection"));

    #endregion
}