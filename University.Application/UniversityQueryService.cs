using Microsoft.EntityFrameworkCore;

namespace University.Application;

public record ProfesorTopStudenciDto(
    string IndeksUczelniany,
    string TytulNaukowy,
    string Imie,
    string Nazwisko,
    int LiczbaProwadzonychKursow,
    int LacznaLiczbaZapisanychStudentow);

public record KursGpaDto(
    string KodKursu,
    string NazwaKursu,
    string NazwaWydzialu,
    double SredniaOcenGpa,
    int LiczbaOcenionychStudentow);

public record StudentTrudnyPlanDto(
    string IndeksUczelniany,
    string Imie,
    string Nazwisko,
    int RokStudiow,
    int LiczbaZapisanychKursow,
    int LiczbaUnikalnychKursowZPrerekwizytami,
    int LaczneUnikalnePunktyEcts);

public class UniversityQueryService
{
    private readonly IUniversityDbContext _context;

    public UniversityQueryService(IUniversityDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Zapytanie 1 [5 pkt]: Profesor z największą łączną liczbą zapisanych studentów
    /// ze wszystkich prowadzonych przez niego kursów.
    /// </summary>
    public async Task<ProfesorTopStudenciDto?> PobierzProfesoraZNajwiekszaLiczbaStudentowAsync()
    {
        return await _context.Profesorowie
            .AsNoTracking()
            .Select(p => new
            {
                p.IndeksUczelniany,
                p.TytulNaukowy,
                p.Imie,
                p.Nazwisko,
                LiczbaProwadzonychKursow = p.ProwadzoneKursy.Count(),
                LacznaLiczbaZapisanychStudentow = p.ProwadzoneKursy.SelectMany(k => k.Zapisy).Count()
            })
            .OrderByDescending(x => x.LacznaLiczbaZapisanychStudentow)
            .ThenByDescending(x => x.LiczbaProwadzonychKursow)
            .Select(x => new ProfesorTopStudenciDto(
                x.IndeksUczelniany,
                x.TytulNaukowy,
                x.Imie,
                x.Nazwisko,
                x.LiczbaProwadzonychKursow,
                x.LacznaLiczbaZapisanychStudentow
            ))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Zapytanie 2 [5 pkt]: Średnia ocen (GPA) oraz liczba ocenionych studentów
    /// dla wszystkich kursów na wybranym wydziale (tylko zapisy z Ocena != null).
    /// </summary>
    public async Task<List<KursGpaDto>> PobierzSredniaOcenDlaWydzialuAsync(int wydzialId)
    {
        return await _context.Kursy
            .AsNoTracking()
            .Where(k => k.WydzialId == wydzialId)
            .Select(k => new
            {
                k.KodKursu,
                NazwaKursu = k.Nazwa,
                NazwaWydzialu = k.Wydzial.Nazwa,
                SredniaOcenGpa = k.Zapisy
                    .Where(z => z.Ocena != null)
                    .Average(z => z.Ocena) ?? 0.0,
                LiczbaOcenionychStudentow = k.Zapisy.Count(z => z.Ocena != null)
            })
            .OrderByDescending(x => x.SredniaOcenGpa)
            .ThenBy(x => x.KodKursu)
            .Select(x => new KursGpaDto(
                x.KodKursu,
                x.NazwaKursu,
                x.NazwaWydzialu,
                x.SredniaOcenGpa,
                x.LiczbaOcenionychStudentow
            ))
            .ToListAsync();
    }

    /// <summary>
    /// Zapytanie 3 [10 pkt]: Student z najtrudniejszym planem zajęć.
    /// Suma punktów ECTS kursów, na które student jest zapisany ORAZ ich bezpośrednich
    /// wymagań wstępnych (z pełną deduplikacją po stronie bazy danych).
    /// </summary>
    public async Task<List<StudentTrudnyPlanDto>> PobierzRankingNajtrudniejszychPlanowAsync(int topN = 5)
    {
        return await _context.Studenci
            .AsNoTracking()
            .Select(s => new
            {
                s.IndeksUczelniany,
                s.Imie,
                s.Nazwisko,
                s.RokStudiow,
                LiczbaZapisanychKursow = s.Zapisy.Count(),
                // Liczba unikalnych kursów (zapisane + ich bezpośrednie prerekwizyty bez duplikatów)
                LiczbaUnikalnychKursowZPrerekwizytami = _context.Kursy.Count(k =>
                    s.Zapisy.Any(z => z.KursId == k.Id) ||
                    s.Zapisy.SelectMany(z => z.Kurs.WymaganiaWstepne).Any(w => w.Id == k.Id)),
                // Suma unikalnych punktów ECTS (każdy kurs z tabeli Kursy liczony maksymalnie raz)
                LaczneUnikalnePunktyEcts = _context.Kursy
                    .Where(k =>
                        s.Zapisy.Any(z => z.KursId == k.Id) ||
                        s.Zapisy.SelectMany(z => z.Kurs.WymaganiaWstepne).Any(w => w.Id == k.Id))
                    .Sum(k => (int?)k.PunktyECTS) ?? 0
            })
            .OrderByDescending(x => x.LaczneUnikalnePunktyEcts)
            .ThenByDescending(x => x.LiczbaUnikalnychKursowZPrerekwizytami)
            .Take(topN)
            .Select(x => new StudentTrudnyPlanDto(
                x.IndeksUczelniany,
                x.Imie,
                x.Nazwisko,
                x.RokStudiow,
                x.LiczbaZapisanychKursow,
                x.LiczbaUnikalnychKursowZPrerekwizytami,
                x.LaczneUnikalnePunktyEcts
            ))
            .ToListAsync();
    }
}