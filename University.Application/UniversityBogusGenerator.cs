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

        // 2. Generowanie Wydziałów (Wydział 1..3)
        var wydzialy = new List<Wydzial>();
        for (int i = 1; i <= 3; i++)
        {
            wydzialy.Add(await _universityService.DodajWydzialAsync($"Wydział {i}"));
        }

        // 3. Generowanie Profesorów z Gabinetami (ulice, miasta i kody pocztowe z Fakera)
        var tytuly = new[] { "dr inż.", "dr hab. inż.", "prof. dr hab. inż." };
        var profesorowie = new List<Profesor>();

        for (int i = 1; i <= liczbaProfesorow; i++)
        {
            var adres = new Adres(
                faker.Address.StreetAddress(),
                faker.Address.City(),
                faker.Address.ZipCode("##-###"));

            var profesor = await _universityService.DodajProfesoraAsync(
                $"ProfImie{i}",
                $"Nazw{i}",
                faker.PickRandom(tytuly),
                adres,
                $"Sala {i}",
                $"Budynek {faker.Random.Int(1, 3)}");

            profesorowie.Add(profesor);
        }

        // 4. Generowanie Studentów Inżynierskich (Student 1..10 -> indeksy S1001..S1010)
        var wszyscyStudenci = new List<Student>();

        for (int i = 1; i <= liczbaStudentowInzynierskich; i++)
        {
            var adres = new Adres(
                faker.Address.StreetAddress(),
                faker.Address.City(),
                faker.Address.ZipCode("##-###"));

            var student = await _universityService.DodajStudentaAsync(
                $"Imie{i}",
                $"Nazw{i}",
                faker.Random.Int(1, 3),
                adres);

            wszyscyStudenci.Add(student);
        }

        // 5. Generowanie Studentów Magisterskich z Promotorami (Student 11..15 -> indeksy S1011..S1015)
        for (int i = 1; i <= liczbaStudentowMagisterskich; i++)
        {
            int numerStudenta = liczbaStudentowInzynierskich + i;
            var adres = new Adres(
                faker.Address.StreetAddress(),
                faker.Address.City(),
                faker.Address.ZipCode("##-###"));

            var promotor = faker.PickRandom(profesorowie);

            var magistrant = await _universityService.DodajStudentaMagisterskiegoAsync(
                "Student",
                $"{numerStudenta}",
                faker.Random.Int(4, 5),
                adres,
                $"Temat pracy dyplomowej {i}",
                promotor.Id);

            wszyscyStudenci.Add(magistrant);
        }

        // 6. Generowanie Kursów (Kurs 1..9) oraz Wymagań Wstępnych (Self-Referencing N:M)
        var kursy = new List<Kurs>();
        for (int i = 1; i <= 9; i++)
        {
            var kurs = new Kurs
            {
                KodKursu = $"KURS-0{i}",
                Nazwa = $"Kurs {i}",
                PunktyECTS = faker.Random.Int(4, 6),
                WydzialId = wydzialy[(i - 1) % wydzialy.Count].Id,
                ProwadzacyId = faker.PickRandom(profesorowie).Id
            };
            kursy.Add(kurs);
            _context.Kursy.Add(kurs);
        }
        await _context.SaveChangesAsync();

        // Konfiguracja wymagań wstępnych (w tym wspólnych prerekwizytów dla Zapytania 3):
        kursy[1].WymaganiaWstepne.Add(kursy[0]);
        kursy[1].WymaganiaWstepne.Add(kursy[7]);

        kursy[2].WymaganiaWstepne.Add(kursy[0]);

        kursy[3].WymaganiaWstepne.Add(kursy[1]);
        kursy[3].WymaganiaWstepne.Add(kursy[2]);

        kursy[4].WymaganiaWstepne.Add(kursy[2]);
        kursy[4].WymaganiaWstepne.Add(kursy[3]);

        kursy[6].WymaganiaWstepne.Add(kursy[5]);

        kursy[8].WymaganiaWstepne.Add(kursy[7]);

        await _context.SaveChangesAsync();

        // 7. Generowanie Zapisów na Kursy (Enrollment) z ocenami (w tym nullami)
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