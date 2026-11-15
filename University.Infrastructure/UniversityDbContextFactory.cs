using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace University.Infrastructure;

public class UniversityDbContextFactory : IDesignTimeDbContextFactory<UniversityDbContext>
{
    public const string DefaultConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=UniversityCleanArchDb;Trusted_Connection=True;MultipleActiveResultSets=true";

    public UniversityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UniversityDbContext>();
        optionsBuilder.UseSqlServer(DefaultConnectionString);

        return new UniversityDbContext(optionsBuilder.Options);
    }
}