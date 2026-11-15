using Microsoft.EntityFrameworkCore;
using University.Domain;

namespace University.Application;

public record StudentWidokDto(
    int Id,
    string IndeksUczelniany,
    string Imie,
    string Nazwisko,
    int RokStudiow,
    string Adres,
    DateTime OstatniaModyfikacja,
    bool CzyMagistrant,
    string? TematPracyDyplomowej,
    string? PromotorOpis);

public class UniversityService
{
    private readonly IUniversityDbContext _context;

    public UniversityService(IUniversityDbContext context)
    {
        _context = context;
    }

    // =========================================================================
    // 1. ZARZĄDZANIE LICZNIKAMI INDEKSÓW (LicznikIndeksow)
    // =========================================================================

    public async Task<List<LicznikIndeksow>> PobierzLicznikiAsync()
    {
        return await _context.LicznikiIndeksow
            .AsNoTracking()
            .OrderBy(l => l.Prefix)
            .ToListAsync();
    }

    public async Task DodajLubUstawLicznikAsync(string prefix, int wartoscPoczatkowa)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefiks nie może być pusty.", nameof(prefix));

        var znormalizowanyPrefix = prefix.Trim().ToUpperInvariant();
        var istniejacy = await _context.LicznikiIndeksow
            .FirstOrDefaultAsync(l => l.Prefix == znormalizowanyPrefix);

        if (istniejacy == null)
        {
            _context.LicznikiIndeksow.Add(new LicznikIndeksow
            {
                Prefix = znormalizowanyPrefix,
                AktualnaWartosc = wartoscPoczatkowa
            });
        }
        else
        {
            istniejacy.AktualnaWartosc = wartoscPoczatkowa;
        }

        await _context.SaveChangesAsync();
    }

    // =========================================================================
    // 2. TWORZENIE STUDENTÓW I PROFESORÓW (Transakcja + Licznik + Shadow Prop)
    // =========================================================================

    public async Task<Student> DodajStudentaAsync(
        string imie,
        string nazwisko,
        int rokStudiow,
        Adres adres)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var licznik = await PobierzLubUtworzLicznikWTransakcjiAsync("S", 1000);
            licznik.AktualnaWartosc++;

            var student = new Student
            {
                Imie = imie.Trim(),
                Nazwisko = nazwisko.Trim(),
                RokStudiow = rokStudiow,
                IndeksUczelniany = $"S{licznik.AktualnaWartosc}",
                AdresZamieszkania = adres
            };

            _context.Studenci.Add(student);

            // Ustawienie właściwości ukrytej (Shadow Property) przez ChangeTracker
            _context.Entry(student).Property("OstatniaModyfikacja").CurrentValue = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return student;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StudentStudiowMagisterskich> DodajStudentaMagisterskiegoAsync(
        string imie,
        string nazwisko,
        int rokStudiow,
        Adres adres,
        string tematPracy,
        int? promotorId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var licznik = await PobierzLubUtworzLicznikWTransakcjiAsync("S", 1000);
            licznik.AktualnaWartosc++;

            var magistrant = new StudentStudiowMagisterskich
            {
                Imie = imie.Trim(),
                Nazwisko = nazwisko.Trim(),
                RokStudiow = rokStudiow,
                IndeksUczelniany = $"S{licznik.AktualnaWartosc}",
                AdresZamieszkania = adres,
                TematPracyDyplomowej = tematPracy.Trim(),
                PromotorId = promotorId
            };

            _context.StudenciMagisterscy.Add(magistrant);

            // Ustawienie właściwości ukrytej (Shadow Property) przez ChangeTracker
            _context.Entry(magistrant).Property("OstatniaModyfikacja").CurrentValue = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return magistrant;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Profesor> DodajProfesoraAsync(
        string imie,
        string nazwisko,
        string tytulNaukowy,
        Adres adres,
        string? numerPokoju = null,
        string? budynek = null)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var licznik = await PobierzLubUtworzLicznikWTransakcjiAsync("P", 100);
            licznik.AktualnaWartosc++;

            var profesor = new Profesor
            {
                Imie = imie.Trim(),
                Nazwisko = nazwisko.Trim(),
                TytulNaukowy = tytulNaukowy.Trim(),
                IndeksUczelniany = $"P{licznik.AktualnaWartosc}",
                AdresZamieszkania = adres
            };

            if (!string.IsNullOrWhiteSpace(numerPokoju) && !string.IsNullOrWhiteSpace(budynek))
            {
                profesor.Gabinet = new Gabinet
                {
                    NumerPokoju = numerPokoju.Trim(),
                    Budynek = budynek.Trim()
                };
            }

            _context.Profesorowie.Add(profesor);

            // Ustawienie właściwości ukrytej (Shadow Property) przez ChangeTracker
            _context.Entry(profesor).Property("OstatniaModyfikacja").CurrentValue = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return profesor;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // 3. AKTUALIZACJA (Update + aktualizacja Shadow Property)
    // =========================================================================

    public async Task AktualizujStudentaAsync(
        int studentId,
        string noweImie,
        string noweNazwisko,
        int nowyRokStudiow,
        Adres nowyAdres)
    {
        var student = await _context.Studenci.FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new InvalidOperationException($"Nie znaleziono studenta o ID={studentId}.");

        student.Imie = noweImie.Trim();
        student.Nazwisko = noweNazwisko.Trim();
        student.RokStudiow = nowyRokStudiow;
        student.AdresZamieszkania = nowyAdres;

        _context.Entry(student).Property("OstatniaModyfikacja").CurrentValue = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task AktualizujProfesoraAsync(
        int profesorId,
        string noweImie,
        string noweNazwisko,
        string nowyTytul,
        Adres nowyAdres)
    {
        var profesor = await _context.Profesorowie.FirstOrDefaultAsync(p => p.Id == profesorId)
            ?? throw new InvalidOperationException($"Nie znaleziono profesora o ID={profesorId}.");

        profesor.Imie = noweImie.Trim();
        profesor.Nazwisko = noweNazwisko.Trim();
        profesor.TytulNaukowy = nowyTytul.Trim();
        profesor.AdresZamieszkania = nowyAdres;

        _context.Entry(profesor).Property("OstatniaModyfikacja").CurrentValue = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // =========================================================================
    // 4. USUWANIE W TRANSAKCJI (Warunkowe cofanie licznika + SetNull)
    // =========================================================================

    public async Task<(bool Sukces, string Komunikat)> UsunStudentaAsync(string idLubIndeks)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            Student? student = int.TryParse(idLubIndeks, out var id)
                ? await _context.Studenci.FirstOrDefaultAsync(s => s.Id == id)
                : await _context.Studenci.FirstOrDefaultAsync(s => s.IndeksUczelniany == idLubIndeks.Trim().ToUpperInvariant());

            if (student == null)
                return (false, $"Nie znaleziono studenta dla identyfikatora '{idLubIndeks}'.");

            var (prefix, numer) = RozbijIndeks(student.IndeksUczelniany);
            var licznik = await _context.LicznikiIndeksow.FirstOrDefaultAsync(l => l.Prefix == prefix);

            bool cofnietoLicznik = false;
            if (licznik != null && numer == licznik.AktualnaWartosc)
            {
                licznik.AktualnaWartosc--;
                cofnietoLicznik = true;
            }

            _context.Studenci.Remove(student);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var infoLicznik = cofnietoLicznik
                ? $"Cofnięto licznik '{prefix}' do wartości {licznik!.AktualnaWartosc}."
                : $"Licznik '{prefix}' pozostał bez zmian ({licznik?.AktualnaWartosc}).";

            return (true, $"Usunięto studenta {student.IndeksUczelniany} ({student.Imie} {student.Nazwisko}). {infoLicznik}");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool Sukces, string Komunikat)> UsunProfesoraAsync(string idLubIndeks)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            Profesor? profesor = int.TryParse(idLubIndeks, out var id)
                ? await _context.Profesorowie
                    .Include(p => p.ProwadzoneKursy)
                    .FirstOrDefaultAsync(p => p.Id == id)
                : await _context.Profesorowie
                    .Include(p => p.ProwadzoneKursy)
                    .FirstOrDefaultAsync(p => p.IndeksUczelniany == idLubIndeks.Trim().ToUpperInvariant());

            if (profesor == null)
                return (false, $"Nie znaleziono profesora dla identyfikatora '{idLubIndeks}'.");

            // Jeśli usuwany profesor prowadzi kursy, przepisujemy je innemu profesorowi (lub usuwamy, gdy brak innych),
            // aby umożliwić bezbłędne usunięcie profesora i przetestowanie DeleteBehavior.SetNull na magistrantach.
            if (profesor.ProwadzoneKursy.Any())
            {
                var zastepca = await _context.Profesorowie
                    .FirstOrDefaultAsync(p => p.Id != profesor.Id);

                if (zastepca != null)
                {
                    foreach (var kurs in profesor.ProwadzoneKursy)
                    {
                        kurs.ProwadzacyId = zastepca.Id;
                    }
                }
                else
                {
                    _context.Kursy.RemoveRange(profesor.ProwadzoneKursy);
                }
            }

            var (prefix, numer) = RozbijIndeks(profesor.IndeksUczelniany);
            var licznik = await _context.LicznikiIndeksow.FirstOrDefaultAsync(l => l.Prefix == prefix);

            bool cofnietoLicznik = false;
            if (licznik != null && numer == licznik.AktualnaWartosc)
            {
                licznik.AktualnaWartosc--;
                cofnietoLicznik = true;
            }

            _context.Profesorowie.Remove(profesor);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var infoLicznik = cofnietoLicznik
                ? $"Cofnięto licznik '{prefix}' do wartości {licznik!.AktualnaWartosc}."
                : $"Licznik '{prefix}' pozostał bez zmian ({licznik?.AktualnaWartosc}).";

            return (true, $"Usunięto profesora {profesor.IndeksUczelniany} ({profesor.TytulNaukowy} {profesor.Imie} {profesor.Nazwisko}). {infoLicznik}");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    // 5. ODCZYT DANYCH (w tym Shadow Property i AsSplitQuery) + POZOSTAŁY CRUD
    // =========================================================================

    public async Task<List<StudentWidokDto>> PobierzStudentowZShadowPropertyAsync()
    {
        return await _context.Studenci
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new StudentWidokDto(
                s.Id,
                s.IndeksUczelniany,
                s.Imie,
                s.Nazwisko,
                s.RokStudiow,
                s.AdresZamieszkania.ToString(),
                EF.Property<DateTime>(s, "OstatniaModyfikacja"),
                s is StudentStudiowMagisterskich,
                s is StudentStudiowMagisterskich
                    ? ((StudentStudiowMagisterskich)s).TematPracyDyplomowej
                    : null,
                s is StudentStudiowMagisterskich && ((StudentStudiowMagisterskich)s).Promotor != null
                    ? ((StudentStudiowMagisterskich)s).Promotor!.TytulNaukowy + " " +
                      ((StudentStudiowMagisterskich)s).Promotor!.Imie + " " +
                      ((StudentStudiowMagisterskich)s).Promotor!.Nazwisko + " (" +
                      ((StudentStudiowMagisterskich)s).Promotor!.IndeksUczelniany + ")"
                    : null
            ))
            .ToListAsync();
    }

    public async Task<List<(Profesor Profesor, DateTime OstatniaModyfikacja)>> PobierzProfesorowZSzczegolamiAsync()
    {
        // Użycie .AsSplitQuery() zapobiega eksplozji kartezjańskiej przy ładowaniu kolekcji ProwadzoneKursy i Dyplomanci
        var profesorowie = await _context.Profesorowie
            .AsNoTracking()
            .Include(p => p.Gabinet)
            .Include(p => p.ProwadzoneKursy)
            .Include(p => p.Dyplomanci)
            .AsSplitQuery()
            .OrderBy(p => p.Id)
            .ToListAsync();

        var modyfikacje = await _context.Profesorowie
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                OstatniaModyfikacja = EF.Property<DateTime>(p, "OstatniaModyfikacja")
            })
            .ToDictionaryAsync(x => x.Id, x => x.OstatniaModyfikacja);

        return profesorowie
            .Select(p => (p, modyfikacje.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<List<Kurs>> PobierzKursyZWymaganiamiAsync()
    {
        return await _context.Kursy
            .AsNoTracking()
            .Include(k => k.Wydzial)
            .Include(k => k.Prowadzacy)
            .Include(k => k.WymaganiaWstepne)
            .AsSplitQuery()
            .OrderBy(k => k.KodKursu)
            .ToListAsync();
    }

    public async Task<List<Wydzial>> PobierzWydzialyAsync()
    {
        return await _context.Wydzialy
            .AsNoTracking()
            .OrderBy(w => w.Id)
            .ToListAsync();
    }

    public async Task<Wydzial> DodajWydzialAsync(string nazwa)
    {
        var wydzial = new Wydzial { Nazwa = nazwa.Trim() };
        _context.Wydzialy.Add(wydzial);
        await _context.SaveChangesAsync();
        return wydzial;
    }

    public async Task<Kurs> DodajKursAsync(string nazwa, string kodKursu, int ects, int wydzialId, int prowadzacyId)
    {
        var kurs = new Kurs
        {
            Nazwa = nazwa.Trim(),
            KodKursu = kodKursu.Trim(),
            PunktyECTS = ects,
            WydzialId = wydzialId,
            ProwadzacyId = prowadzacyId
        };

        _context.Kursy.Add(kurs);
        await _context.SaveChangesAsync();
        return kurs;
    }

    // =========================================================================
    // METODY POMOCNICZE
    // =========================================================================

    private async Task<LicznikIndeksow> PobierzLubUtworzLicznikWTransakcjiAsync(string prefix, int domyslnaWartoscBazowa)
    {
        var licznik = await _context.LicznikiIndeksow
            .FirstOrDefaultAsync(l => l.Prefix == prefix);

        if (licznik == null)
        {
            licznik = new LicznikIndeksow
            {
                Prefix = prefix,
                AktualnaWartosc = domyslnaWartoscBazowa
            };
            _context.LicznikiIndeksow.Add(licznik);
        }

        return licznik;
    }

    private static (string Prefix, int Numer) RozbijIndeks(string indeks)
    {
        var prefix = new string(indeks.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
        var czescLiczbowa = new string(indeks.SkipWhile(char.IsLetter).ToArray());
        int.TryParse(czescLiczbowa, out var numer);
        return (prefix, numer);
    }
}