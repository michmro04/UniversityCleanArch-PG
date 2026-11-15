using Bogus;
using Microsoft.EntityFrameworkCore;
using University.Domain;

namespace University.Application;

public class UniversityBogusGenerator
{
    private readonly IUniversityDbContext _context;
    private readonly UniversityService _universityService;

    public UniversityBogusGenerator(IUniversityDbContext context, UniversityService universityService)
    {
        _context = context;
        _universityService = universityService;
    }

    public async Task WygenerujDaneTestoweAsync(
        int liczbaProfesorow = 6,
        int liczbaStudentowInzynierskich = 10,
        int liczbaStudentowMagisterskich = 5)
    {
        // 1. Oczyszczenie istniejących danych i reset liczników przed nowym seedowaniem
        _context.Zapisy.RemoveRange(await _context.Zapisy.ToListAsync());

        var istniejaceKursy = await _context.Kursy
            .Include(k => k.WymaganiaWstepne)
            .ToListAsync();
        foreach (var k in istniejaceKursy)
        {
            k.WymaganiaWstepne.Clear();
        }
        await _context.SaveChangesAsync();

        _context.Kursy.RemoveRange(istniejaceKursy);
        _context.Gabinety.RemoveRange(await _context.Gabinety.ToListAsync());
        _context.Studenci.RemoveRange(await _context.Studenci.ToListAsync());
        _context.Profesorowie.RemoveRange(await _context.Profesorowie.ToListAsync());
        _context.Wydzialy.RemoveRange(await _context.Wydzialy.ToListAsync());
        await _context.SaveChangesAsync();

        await _universityService.DodajLubUstawLicznikAsync("S", 1000);
        await _universityService.DodajLubUstawLicznikAsync("P", 100);

        Randomizer.Seed = new Random(2026);
        var faker = new Faker("pl");

        // 2. Generowanie Wydziałów
        var nazwyWydzialow = new[]
        {
            "Wydział Elektroniki, Telekomunikacji i Informatyki",
            "Wydział Elektrotechniki i Automatyki",
            "Wydział Fizyki Technicznej i Matematyki Stosowanej"
        };

        var wydzialy = new List<Wydzial>();
        foreach (var nazwa in nazwyWydzialow)
        {
            wydzialy.Add(await _universityService.DodajWydzialAsync(nazwa));
        }

        // 3. Generowanie Profesorów z Gabinetami (przez UniversityService -> licznik "P" + konstruktor Adres)
        var tytuly = new[] { "dr inż.", "dr hab. inż.", "prof. dr hab. inż." };
        var budynki = new[] { "WETI A", "WETI B", "Gmach Główny" };
        var profesorowie = new List<Profesor>();

        for (int i = 0; i < liczbaProfesorow; i++)
        {
            var adres = new Adres(
                faker.Address.StreetAddress(),
                faker.Address.City(),
                faker.Address.ZipCode("##-###"));

            var numerPokoju = $"EA-{faker.Random.Int(101, 450)}";
            var budynek = faker.PickRandom(budynki);

            var profesor = await _universityService.DodajProfesoraAsync(
                faker.Name.FirstName(),
                faker.Name.LastName(),
                faker.PickRandom(tytuly),
                adres,
                numerPokoju,
                budynek);

            profesorowie.Add(profesor);
        }

        // 4. Generowanie Studentów Inżynierskich (przez UniversityService -> licznik "S" + konstruktor Adres)
        var wszyscyStudenci = new List<Student>();

        for (int i = 0; i < liczbaStudentowInzynierskich; i++)
        {
            var adres = new Adres(
                faker.Address.StreetAddress(),
                faker.Address.City(),
                faker.Address.ZipCode("##-###"));

            var student = await _universityService.DodajStudentaAsync(
                faker.Name.FirstName(),
                faker.Name.LastName(),
                faker.Random.Int(1, 3),
                adres);

            wszyscyStudenci.Add(student);
        }

        // 5. Generowanie Studentów Magisterskich z Promotorami (przez UniversityService -> licznik "S")
        var tematyPrac = new[]
        {
            "Optymalizacja zapytań w systemach rozproszonych z użyciem EF Core",
            "Analiza algorytmów przepływowych w sieciach wielotowarowych",
            "Zastosowanie głębokich sieci grafowych w systemach rekomendacyjnych",
            "Projekt i implementacja systemu czasu rzeczywistego dla pojazdów podwodnych",
            "Architektura mikroserwisowa w chmurze obliczeniowej o wysokiej dostępności"
        };

        for (int i = 0; i < liczbaStudentowMagisterskich; i++)
        {
            var adres = new Adres(
                faker.Address.StreetAddress(),
                faker.Address.City(),
                faker.Address.ZipCode("##-###"));

            var promotor = faker.PickRandom(profesorowie);
            var temat = tematyPrac[i % tematyPrac.Length];

            var magistrant = await _universityService.DodajStudentaMagisterskiegoAsync(
                faker.Name.FirstName(),
                faker.Name.LastName(),
                faker.Random.Int(4, 5),
                adres,
                temat,
                promotor.Id);

            wszyscyStudenci.Add(magistrant);
        }

        // 6. Generowanie Kursów oraz Wymagań Wstępnych (Self-Referencing N:M)
        var definicjeKursow = new (string Kod, string Nazwa, int Ects, int WydzialIdx)[]
        {
            ("ETI-01", " Podstawy Programowania", 6, 0),
            ("ETI-02", "Algorytmy i Struktury Danych", 6, 0),
            ("ETI-03", "Bazy Danych", 5, 0),
            ("ETI-04", "Programowanie Lokalnych Aplikacji (.NET)", 5, 0),
            ("ETI-05", "Architektura Systemów Korporacyjnych", 6, 0),
            ("WEIA-01", "Teoria Obwodów", 5, 1),
            ("WEIA-02", "Systemy Wbudowane", 6, 1),
            ("FTIMS-01", "Analiza Matematyczna", 6, 2),
            ("FTIMS-02", "Metody Probabilistyczne i Statystyka", 4, 2)
        };

        var kursy = new List<Kurs>();
        foreach (var def in definicjeKursow)
        {
            var kurs = new Kurs
            {
                KodKursu = def.Kod,
                Nazwa = def.Nazwa.Trim(),
                PunktyECTS = def.Ects,
                WydzialId = wydzialy[def.WydzialIdx].Id,
                ProwadzacyId = faker.PickRandom(profesorowie).Id
            };
            kursy.Add(kurs);
            _context.Kursy.Add(kurs);
        }
        await _context.SaveChangesAsync();

        // Konfiguracja wymagań wstępnych (w tym wspólnych prerekwizytów do przetestowania Zapytania 3!)
        // ETI-02 (AiSD) wymaga ETI-01 (Podstawy Programowania) oraz FTIMS-01 (Analiza Matematyczna)
        kursy[1].WymaganiaWstepne.Add(kursy[0]);
        kursy[1].WymaganiaWstepne.Add(kursy[7]);

        // ETI-03 (Bazy Danych) wymaga ETI-01 (Podstawy Programowania) -> wspólny prerekwizyt ETI-01!
        kursy[2].WymaganiaWstepne.Add(kursy[0]);

        // ETI-04 (PLANET) wymaga ETI-02 (AiSD) oraz ETI-03 (Bazy Danych)
        kursy[3].WymaganiaWstepne.Add(kursy[1]);
        kursy[3].WymaganiaWstepne.Add(kursy[2]);

        // ETI-05 (Architektura) wymaga ETI-03 (Bazy Danych) oraz ETI-04 (PLANET) -> wspólny prerekwizyt ETI-03!
        kursy[4].WymaganiaWstepne.Add(kursy[2]);
        kursy[4].WymaganiaWstepne.Add(kursy[3]);

        // WEIA-02 wymaga WEIA-01
        kursy[6].WymaganiaWstepne.Add(kursy[5]);

        // FTIMS-02 wymaga FTIMS-01
        kursy[8].WymaganiaWstepne.Add(kursy[7]);

        await _context.SaveChangesAsync();

        // 7. Generowanie Zapisów na Kursy (Enrollment) z ocenami (oraz nullami przed wystawieniem oceny)
        var dopuszczalneOceny = new double?[] { 3.0, 3.5, 4.0, 4.5, 5.0, null };
        var zapisy = new List<Enrollment>();

        foreach (var student in wszyscyStudenci)
        {
            var wylosowaneKursy = faker.PickRandom(kursy, faker.Random.Int(2, 5)).ToList();
            foreach (var kurs in wylosowaneKursy)
            {
                zapisy.Add(new Enrollment
                {
                    StudentId = student.Id,
                    KursId = kurs.Id,
                    Semestr = "2026/27-Z",
                    Ocena = faker.PickRandom(dopuszczalneOceny)
                });
            }
        }

        _context.Zapisy.AddRange(zapisy);
        await _context.SaveChangesAsync();
    }
}