using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VideoUploader.Data.Database;

public class VideoUploaderContextFactory : IDesignTimeDbContextFactory<VideoUploaderContext>
{
    public VideoUploaderContext CreateDbContext(string[] args)
    {
        // Configuração do arquivo appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../VideoUploader.API"))
            .AddJsonFile("appsettings.json")
            .Build();

        // Configuração das opções do DbContext
        var optionsBuilder = new DbContextOptionsBuilder<VideoUploaderContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        return new VideoUploaderContext(configuration, optionsBuilder.Options);
    }
}