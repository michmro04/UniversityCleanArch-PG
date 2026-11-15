using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using University.Domain;

namespace University.Application;

public interface IUniversityDbContext
{
    DbSet<Student> Studenci { get; }
    DbSet<StudentStudiowMagisterskich> StudenciMagisterscy { get; }
    DbSet<Profesor> Profesorowie { get; }
    DbSet<Gabinet> Gabinety { get; }
    DbSet<Wydzial> Wydzialy { get; }
    DbSet<Kurs> Kursy { get; }
    DbSet<Enrollment> Zapisy { get; }
    DbSet<LicznikIndeksow> LicznikiIndeksow { get; }

    DatabaseFacade Database { get; }
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}