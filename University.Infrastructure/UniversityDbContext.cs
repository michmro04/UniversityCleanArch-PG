using Microsoft.EntityFrameworkCore;
using University.Application;
using University.Domain;

namespace University.Infrastructure;

public class UniversityDbContext : DbContext, IUniversityDbContext
{
    public UniversityDbContext(DbContextOptions<UniversityDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Studenci => Set<Student>();
    public DbSet<StudentStudiowMagisterskich> StudenciMagisterscy => Set<StudentStudiowMagisterskich>();
    public DbSet<Profesor> Profesorowie => Set<Profesor>();
    public DbSet<Gabinet> Gabinety => Set<Gabinet>();
    public DbSet<Wydzial> Wydzialy => Set<Wydzial>();
    public DbSet<Kurs> Kursy => Set<Kurs>();
    public DbSet<Enrollment> Zapisy => Set<Enrollment>();
    public DbSet<LicznikIndeksow> LicznikiIndeksow => Set<LicznikIndeksow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LicznikIndeksow>(builder =>
        {
            builder.HasKey(l => l.Prefix);
            builder.Property(l => l.Prefix).HasMaxLength(10);

            builder.HasData(
                new LicznikIndeksow { Prefix = "S", AktualnaWartosc = 1000 },
                new LicznikIndeksow { Prefix = "P", AktualnaWartosc = 100 }
            );
        });

        //konfiguracja encji Student
        modelBuilder.Entity<Student>(builder =>
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Imie).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Nazwisko).IsRequired().HasMaxLength(100);
            builder.Property(s => s.IndeksUczelniany).IsRequired().HasMaxLength(20);

            builder.HasIndex(s => s.IndeksUczelniany).IsUnique();

            builder.Property<DateTime>("OstatniaModyfikacja");

            builder.OwnsOne(s => s.AdresZamieszkania, adres =>
            {
                adres.Property(a => a.Ulica)
                    .HasColumnName("Adres_Ulica")
                    .HasMaxLength(150)
                    .IsRequired();

                adres.Property(a => a.Miasto)
                    .HasColumnName("Adres_Miasto")
                    .HasMaxLength(100)
                    .IsRequired();

                adres.Property(a => a.KodPocztowy)
                    .HasColumnName("Adres_KodPocztowy")
                    .HasMaxLength(10)
                    .IsRequired();
            });
        });

        //konfiguracja encji StudentStudiowMagisterskich
        modelBuilder.Entity<StudentStudiowMagisterskich>(builder =>
        {
            builder.Property(m => m.TematPracyDyplomowej).IsRequired().HasMaxLength(300);

            builder.HasOne(m => m.Promotor)
                .WithMany(p => p.Dyplomanci)
                .HasForeignKey(m => m.PromotorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        //konfiguracja encji Profesor
        modelBuilder.Entity<Profesor>(builder =>
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Imie).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Nazwisko).IsRequired().HasMaxLength(100);
            builder.Property(p => p.IndeksUczelniany).IsRequired().HasMaxLength(20);
            builder.Property(p => p.TytulNaukowy).IsRequired().HasMaxLength(50);

            builder.HasIndex(p => p.IndeksUczelniany).IsUnique();

            builder.Property<DateTime>("OstatniaModyfikacja");

            builder.OwnsOne(p => p.AdresZamieszkania, adres =>
            {
                adres.Property(a => a.Ulica)
                    .HasColumnName("Adres_Ulica")
                    .HasMaxLength(150)
                    .IsRequired();

                adres.Property(a => a.Miasto)
                    .HasColumnName("Adres_Miasto")
                    .HasMaxLength(100)
                    .IsRequired();

                adres.Property(a => a.KodPocztowy)
                    .HasColumnName("Adres_KodPocztowy")
                    .HasMaxLength(10)
                    .IsRequired();
            });

            builder.HasOne(p => p.Gabinet)
                .WithOne(g => g.Profesor)
                .HasForeignKey<Gabinet>(g => g.ProfesorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        //konfiguracja encji Gabinet
        modelBuilder.Entity<Gabinet>(builder =>
        {
            builder.HasKey(g => g.Id);
            builder.Property(g => g.NumerPokoju).IsRequired().HasMaxLength(30);
            builder.Property(g => g.Budynek).IsRequired().HasMaxLength(100);
        });

        //konfiguracja encji Wydzial
        modelBuilder.Entity<Wydzial>(builder =>
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Nazwa).IsRequired().HasMaxLength(200);
        });

        //konfiguracja encji Kurs
        modelBuilder.Entity<Kurs>(builder =>
        {
            builder.HasKey(k => k.Id);
            builder.Property(k => k.Nazwa).IsRequired().HasMaxLength(200);
            builder.Property(k => k.KodKursu).IsRequired().HasMaxLength(30);

            builder.HasOne(k => k.Wydzial)
                .WithMany(w => w.Kursy)
                .HasForeignKey(k => k.WydzialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(k => k.Prowadzacy)
                .WithMany(p => p.ProwadzoneKursy)
                .HasForeignKey(k => k.ProwadzacyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(k => k.WymaganiaWstepne)
                .WithMany(k => k.KursyZalezne)
                .UsingEntity<Dictionary<string, object>>(
                    "KursWymaganiaWstepne",
                    j => j.HasOne<Kurs>()
                          .WithMany()
                          .HasForeignKey("WymaganyKursId")
                          .OnDelete(DeleteBehavior.ClientCascade),
                    j => j.HasOne<Kurs>()
                          .WithMany()
                          .HasForeignKey("KursId")
                          .OnDelete(DeleteBehavior.Cascade));
        });

        //konfiguracja encji Enrollment
        modelBuilder.Entity<Enrollment>(builder =>
        {
            builder.HasKey(e => new { e.StudentId, e.KursId });

            builder.Property(e => e.Semestr).IsRequired().HasMaxLength(20);

            builder.HasOne(e => e.Student)
                .WithMany(s => s.Zapisy)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Kurs)
                .WithMany(k => k.Zapisy)
                .HasForeignKey(e => e.KursId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}