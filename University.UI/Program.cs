using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using University.Application;
using University.Domain;
using University.Infrastructure;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        // Wyciszenie logów technicznych SQL w konsoli, aby interfejs był czytelny
        logging.ClearProviders();
    })
    .ConfigureServices((_, services) =>
    {
        services.AddDbContext<UniversityDbContext>(options =>
            options.UseSqlServer(UniversityDbContextFactory.DefaultConnectionString));

        services.AddScoped<IUniversityDbContext>(sp => sp.GetRequiredService<UniversityDbContext>());
        services.AddScoped<UniversityService>();
        services.AddScoped<UniversityBogusGenerator>();
        services.AddScoped<UniversityQueryService>();
    })
    .Build();

// Automatyczna migracja przy starcie aplikacji (Wymóg Części 3 pkt 5 oraz Scenariusza 1)
await using (var initScope = host.Services.CreateAsyncScope())
{
    var db = initScope.ServiceProvider.GetRequiredService<UniversityDbContext>();
    await db.Database.MigrateAsync();
}

bool dziala = true;
while (dziala)
{
    WyswietlNaglowekMenu();
    Console.Write("Wybierz opcję: ");
    var wybor = Console.ReadLine()?.Trim();
    Console.WriteLine();

    await using var scope = host.Services.CreateAsyncScope();
    var universityService = scope.ServiceProvider.GetRequiredService<UniversityService>();
    var bogusGenerator = scope.ServiceProvider.GetRequiredService<UniversityBogusGenerator>();
    var queryService = scope.ServiceProvider.GetRequiredService<UniversityQueryService>();

    try
    {
        switch (wybor)
        {
            case "1":
                await ObsluzGeneratorBogusAsync(bogusGenerator, universityService);
                break;
            case "2":
                await WyswietlStudentowAsync(universityService);
                break;
            case "3":
                await WyswietlProfesorowAsync(universityService);
                break;
            case "4":
                await WyswietlKursyAsync(universityService);
                break;
            case "5":
                await ZarzadzajLicznikamiAsync(universityService);
                break;
            case "6":
                await DodajStudentaInteraktywnieAsync(universityService);
                break;
            case "7":
                await EdytujStudentaInteraktywnieAsync(universityService);
                break;
            case "8":
                await DodajLubEdytujProfesoraInteraktywnieAsync(universityService);
                break;
            case "9":
                await UsunStudentaInteraktywnieAsync(universityService);
                break;
            case "10":
                await UsunProfesoraInteraktywnieAsync(universityService);
                break;
            case "11":
                await UruchomZapytaniaAnalityczneAsync(queryService, universityService);
                break;
            case "0":
                dziala = false;
                continue;
            default:
                WypiszKolor("Nieznana opcja. Wybierz numer z menu.", ConsoleColor.Yellow);
                break;
        }
    }
    catch (ArgumentException ex)
    {
        WypiszKolor($"[BŁĄD WALIDACJI DOMENOWEJ]: {ex.Message}", ConsoleColor.Red);
    }
    catch (Exception ex)
    {
        WypiszKolor($"[BŁĄD OPERACJI]: {ex.Message}", ConsoleColor.Red);
    }

    if (dziala)
    {
        Console.WriteLine("\nNaciśnij dowolny klawisz, aby wrócić do menu...");
        Console.ReadKey(true);
    }
}

// =============================================================================
// FUNKCJE OBSŁUGI MENU UI
// =============================================================================

static void WyswietlNaglowekMenu()
{
    WypiszKolor("======================================================================", ConsoleColor.Cyan);
    WypiszKolor("   SYSTEM ZARZĄDZANIA UCZELNIĄ - UNIVERSITY CLEAN ARCH (EF CORE)      ", ConsoleColor.Cyan);
    WypiszKolor("======================================================================", ConsoleColor.Cyan);
    Console.WriteLine(" 1.  [Bogus] Wygeneruj świeży zestaw danych testowych");
    Console.WriteLine(" 2.  [Odczyt] Lista studentów (Adres VO + Shadow Property + Promotor)");
    Console.WriteLine(" 3.  [Odczyt] Lista profesorów (Gabinet + Kursy + Dyplomanci - SplitQuery)");
    Console.WriteLine(" 4.  [Odczyt] Lista kursów i ich wymagań wstępnych (Prerequisites)");
    Console.WriteLine(" 5.  [Liczniki] Tabela LicznikiIndeksow + dodanie nowego prefiksu (np. 'D')");
    Console.WriteLine(" 6.  [CRUD] Dodaj nowego studenta (test licznika 'S' i walidacji Adresu)");
    Console.WriteLine(" 7.  [CRUD] Edytuj studenta (test aktualizacji Shadow Property)");
    Console.WriteLine(" 8.  [CRUD] Dodaj lub edytuj profesora");
    Console.WriteLine(" 9.  [Usuwanie] Usuń studenta po ID/Indeksie (test cofania licznika 'S')");
    Console.WriteLine(" 10. [Usuwanie] Usuń profesora po ID/Indeksie (test SetNull na magistrancie)");
    Console.WriteLine(" 11. [LINQ] Uruchom 3 zaawansowane zapytania analityczne");
    Console.WriteLine(" 0.  Wyjście");
    Console.WriteLine("----------------------------------------------------------------------");
}

static async Task ObsluzGeneratorBogusAsync(UniversityBogusGenerator generator, UniversityService service)
{
    WypiszKolor("Trwa generowanie danych uczelnianych przez bibliotekę Bogus...", ConsoleColor.Yellow);
    await generator.WygenerujDaneTestoweAsync();
    WypiszKolor("Pomyślnie wygenerowano dane testowe z użyciem transakcyjnego serwisu numeracji!", ConsoleColor.Green);

    Console.WriteLine("\nAktualny stan liczników po wygenerowaniu danych:");
    var liczniki = await service.PobierzLicznikiAsync();
    foreach (var l in liczniki)
    {
        Console.WriteLine($" -> Prefiks '{l.Prefix}': AktualnaWartosc = {l.AktualnaWartosc}");
    }
}

static async Task WyswietlStudentowAsync(UniversityService service)
{
    var studenci = await service.PobierzStudentowZShadowPropertyAsync();
    if (!studenci.Any())
    {
        WypiszKolor("Brak studentów w bazie danych. Uruchom najpierw generator (Opcja 1).", ConsoleColor.Yellow);
        return;
    }

    WypiszKolor($"=== LISTA STUDENTÓW (Łącznie: {studenci.Count}) ===", ConsoleColor.Green);
    foreach (var s in studenci)
    {
        var typ = s.CzyMagistrant ? "MAGISTRANT" : "INŻYNIERSKI";
        Console.WriteLine($"[{s.IndeksUczelniany}] (ID: {s.Id}) {s.Imie} {s.Nazwisko} | Rok: {s.RokStudiow} | Typ: {typ}");
        Console.WriteLine($"    Adres: {s.Adres}");
        Console.WriteLine($"    OstatniaModyfikacja (Shadow Prop): {s.OstatniaModyfikacja:yyyy-MM-dd HH:mm:ss.fff} UTC");

        if (s.CzyMagistrant)
        {
            var promotor = s.PromotorOpis ?? "BRAK PROMOTORA (NULL)";
            WypiszKolor($"    Praca dyplomowa: \"{s.TematPracyDyplomowej}\" | Promotor: {promotor}", ConsoleColor.DarkCyan);
        }
    }
}

static async Task WyswietlProfesorowAsync(UniversityService service)
{
    var dane = await service.PobierzProfesorowZSzczegolamiAsync();
    if (!dane.Any())
    {
        WypiszKolor("Brak profesorów w bazie danych.", ConsoleColor.Yellow);
        return;
    }

    WypiszKolor($"=== LISTA PROFESORÓW (Łącznie: {dane.Count}) ===", ConsoleColor.Green);
    foreach (var (p, modyfikacja) in dane)
    {
        var gabinet = p.Gabinet != null ? $"{p.Gabinet.Budynek}, pok. {p.Gabinet.NumerPokoju}" : "Brak gabinetu";
        Console.WriteLine($"[{p.IndeksUczelniany}] (ID: {p.Id}) {p.TytulNaukowy} {p.Imie} {p.Nazwisko} | Gabinet: {gabinet}");
        Console.WriteLine($"    Adres: {p.AdresZamieszkania} | OstatniaModyfikacja: {modyfikacja:yyyy-MM-dd HH:mm:ss} UTC");

        var kursy = p.ProwadzoneKursy.Any()
            ? string.Join(", ", p.ProwadzoneKursy.Select(k => $"{k.KodKursu} ({k.Nazwa})"))
            : "Brak prowadzonych kursów";
        Console.WriteLine($"    Prowadzone kursy ({p.ProwadzoneKursy.Count}): {kursy}");

        var dyplomanci = p.Dyplomanci.Any()
            ? string.Join(", ", p.Dyplomanci.Select(d => $"{d.IndeksUczelniany} {d.Imie} {d.Nazwisko}"))
            : "Brak przypisanych dyplomantów";
        Console.WriteLine($"    Dyplomanci ({p.Dyplomanci.Count}): {dyplomanci}");
    }
}

static async Task WyswietlKursyAsync(UniversityService service)
{
    var kursy = await service.PobierzKursyZWymaganiamiAsync();
    if (!kursy.Any())
    {
        WypiszKolor("Brak kursów w bazie danych.", ConsoleColor.Yellow);
        return;
    }

    WypiszKolor($"=== SIATKA KURSÓW I WYMAGAŃ WSTĘPNYCH (Łącznie: {kursy.Count}) ===", ConsoleColor.Green);
    foreach (var k in kursy)
    {
        var prerekwizyty = k.WymaganiaWstepne.Any()
            ? string.Join(", ", k.WymaganiaWstepne.Select(w => $"{w.KodKursu} ({w.PunktyECTS} ECTS)"))
            : "Brak wymagań wstępnych";

        Console.WriteLine($"[{k.KodKursu}] {k.Nazwa} | {k.PunktyECTS} ECTS | Wydział: {k.Wydzial.Nazwa}");
        Console.WriteLine($"    Prowadzący: {k.Prowadzacy.TytulNaukowy} {k.Prowadzacy.Imie} {k.Prowadzacy.Nazwisko} ({k.Prowadzacy.IndeksUczelniany})");
        Console.WriteLine($"    Wymagania wstępne: {prerekwizyty}");
    }
}

static async Task ZarzadzajLicznikamiAsync(UniversityService service)
{
    var liczniki = await service.PobierzLicznikiAsync();
    WypiszKolor("=== TABELA LICZNIKI INDEKSÓW ===", ConsoleColor.Green);
    foreach (var l in liczniki)
    {
        Console.WriteLine($" -> Prefiks: '{l.Prefix}' | AktualnaWartosc: {l.AktualnaWartosc}");
    }

    Console.Write("\nCzy chcesz dodać nowy prefiks (np. 'D') lub zmienić wartość istniejącego? (T/N): ");
    if (!string.Equals(Console.ReadLine()?.Trim(), "T", StringComparison.OrdinalIgnoreCase))
        return;

    Console.Write("Podaj prefiks (np. D): ");
    var prefix = Console.ReadLine() ?? "";
    Console.Write("Podaj wartość początkową (np. 500): ");
    if (!int.TryParse(Console.ReadLine(), out var wartosc))
    {
        WypiszKolor("Niepoprawna liczba całkowita.", ConsoleColor.Red);
        return;
    }

    await service.DodajLubUstawLicznikAsync(prefix, wartosc);
    WypiszKolor($"Zapisano prefiks '{prefix.ToUpperInvariant()}' z wartością {wartosc}.", ConsoleColor.Green);
}

static async Task DodajStudentaInteraktywnieAsync(UniversityService service)
{
    WypiszKolor("=== DODAWANIE NOWEGO STUDENTA ===", ConsoleColor.Green);
    Console.Write("Imię: ");
    var imie = Console.ReadLine() ?? "";
    Console.Write("Nazwisko: ");
    var nazwisko = Console.ReadLine() ?? "";
    Console.Write("Rok studiów (1-5): ");
    int.TryParse(Console.ReadLine(), out var rok);
    if (rok <= 0) rok = 1;

    Console.Write("Ulica i numer: ");
    var ulica = Console.ReadLine() ?? "";
    Console.Write("Miasto: ");
    var miasto = Console.ReadLine() ?? "";
    Console.Write("Kod pocztowy (format XX-XXX, np. 80-233; wpisz np. '123' aby przetestować wyjątek walidacji): ");
    var kod = Console.ReadLine() ?? "";

    // Walidacja w konstruktorze Value Object Adres!
    var adres = new Adres(ulica, miasto, kod);

    Console.Write("Czy to student studiów magisterskich? (T/N): ");
    var czyMagister = string.Equals(Console.ReadLine()?.Trim(), "T", StringComparison.OrdinalIgnoreCase);

    if (czyMagister)
    {
        Console.Write("Temat pracy dyplomowej: ");
        var temat = Console.ReadLine() ?? "Analiza systemów informatycznych";
        Console.Write("ID promotora (opcjonalnie, wciśnij Enter aby pominąć): ");
        var promotorInput = Console.ReadLine();
        int? promotorId = int.TryParse(promotorInput, out var pid) ? pid : null;

        var m = await service.DodajStudentaMagisterskiegoAsync(imie, nazwisko, rok, adres, temat, promotorId);
        WypiszKolor($"Dodano magistranta! Nadany indeks z transakcji: {m.IndeksUczelniany} (ID: {m.Id})", ConsoleColor.Green);
    }
    else
    {
        var s = await service.DodajStudentaAsync(imie, nazwisko, rok, adres);
        WypiszKolor($"Dodano studenta! Nadany indeks z transakcji: {s.IndeksUczelniany} (ID: {s.Id})", ConsoleColor.Green);
    }
}

static async Task EdytujStudentaInteraktywnieAsync(UniversityService service)
{
    WypiszKolor("=== EDYCJA STUDENTA (AKTUALIZACJA SHADOW PROPERTY) ===", ConsoleColor.Green);
    Console.Write("Podaj ID studenta do edycji: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        WypiszKolor("Niepoprawne ID.", ConsoleColor.Red);
        return;
    }

    Console.Write("Nowe imię: ");
    var imie = Console.ReadLine() ?? "";
    Console.Write("Nowe nazwisko: ");
    var nazwisko = Console.ReadLine() ?? "";
    Console.Write("Nowy rok studiów: ");
    int.TryParse(Console.ReadLine(), out var rok);
    if (rok <= 0) rok = 1;

    Console.Write("Nowa ulica: ");
    var ulica = Console.ReadLine() ?? "";
    Console.Write("Nowe miasto: ");
    var miasto = Console.ReadLine() ?? "";
    Console.Write("Nowy kod pocztowy (XX-XXX): ");
    var kod = Console.ReadLine() ?? "";

    var nowyAdres = new Adres(ulica, miasto, kod);
    await service.AktualizujStudentaAsync(id, imie, nazwisko, rok, nowyAdres);

    WypiszKolor($"Zaktualizowano dane studenta ID={id} oraz znacznik czasu 'OstatniaModyfikacja'!", ConsoleColor.Green);
}

static async Task DodajLubEdytujProfesoraInteraktywnieAsync(UniversityService service)
{
    Console.Write("Wybierz akcję: [D]odaj nowego profesora / [E]dytuj istniejącego: ");
    var akcja = Console.ReadLine()?.Trim().ToUpperInvariant();

    if (akcja == "E")
    {
        Console.Write("Podaj ID profesora do edycji: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) return;

        Console.Write("Nowe imię: ");
        var imie = Console.ReadLine() ?? "";
        Console.Write("Nowe nazwisko: ");
        var nazwisko = Console.ReadLine() ?? "";
        Console.Write("Nowy tytuł naukowy (np. prof. dr hab.): ");
        var tytul = Console.ReadLine() ?? "";
        Console.Write("Nowa ulica: ");
        var ulica = Console.ReadLine() ?? "";
        Console.Write("Nowe miasto: ");
        var miasto = Console.ReadLine() ?? "";
        Console.Write("Nowy kod pocztowy (XX-XXX): ");
        var kod = Console.ReadLine() ?? "";

        await service.AktualizujProfesoraAsync(id, imie, nazwisko, tytul, new Adres(ulica, miasto, kod));
        WypiszKolor($"Zaktualizowano profesora ID={id} oraz jego Shadow Property 'OstatniaModyfikacja'.", ConsoleColor.Green);
    }
    else
    {
        Console.Write("Imię: ");
        var imie = Console.ReadLine() ?? "";
        Console.Write("Nazwisko: ");
        var nazwisko = Console.ReadLine() ?? "";
        Console.Write("Tytuł naukowy (np. dr inż.): ");
        var tytul = Console.ReadLine() ?? "dr inż.";
        Console.Write("Ulica: ");
        var ulica = Console.ReadLine() ?? "";
        Console.Write("Miasto: ");
        var miasto = Console.ReadLine() ?? "";
        Console.Write("Kod pocztowy (XX-XXX): ");
        var kod = Console.ReadLine() ?? "";
        Console.Write("Numer pokoju gabinetu (np. EA-312): ");
        var pokoj = Console.ReadLine();
        Console.Write("Budynek (np. WETI A): ");
        var budynek = Console.ReadLine();

        var p = await service.DodajProfesoraAsync(imie, nazwisko, tytul, new Adres(ulica, miasto, kod), pokoj, budynek);
        WypiszKolor($"Dodano profesora! Nadany indeks: {p.IndeksUczelniany} (ID: {p.Id})", ConsoleColor.Green);
    }
}

static async Task UsunStudentaInteraktywnieAsync(UniversityService service)
{
    WypiszKolor("=== USUWANIE STUDENTA (TEST COFANIA LICZNIKA 'S') ===", ConsoleColor.Green);
    Console.Write("Podaj ID lub IndeksUczelniany studenta (np. S1015 lub S1005): ");
    var wejscie = Console.ReadLine() ?? "";

    var (sukces, komunikat) = await service.UsunStudentaAsync(wejscie);
    WypiszKolor(komunikat, sukces ? ConsoleColor.Green : ConsoleColor.Red);
}

static async Task UsunProfesoraInteraktywnieAsync(UniversityService service)
{
    WypiszKolor("=== USUWANIE PROFESORA (TEST COFANIA LICZNIKA 'P' ORAZ SET NULL) ===", ConsoleColor.Green);
    Console.Write("Podaj ID lub IndeksUczelniany profesora (np. P106 lub P101): ");
    var wejscie = Console.ReadLine() ?? "";

    var (sukces, komunikat) = await service.UsunProfesoraAsync(wejscie);
    WypiszKolor(komunikat, sukces ? ConsoleColor.Green : ConsoleColor.Red);
}

static async Task UruchomZapytaniaAnalityczneAsync(UniversityQueryService queryService, UniversityService universityService)
{
    WypiszKolor("======================================================================", ConsoleColor.Cyan);
    WypiszKolor("   RAPORTY ANALITYCZNE LINQ (AsNoTracking + Projekcja DTO)            ", ConsoleColor.Cyan);
    WypiszKolor("======================================================================", ConsoleColor.Cyan);

    // ZAPYTANIE 1
    WypiszKolor("\n[Zapytanie 1] Profesor z największą łączną liczbą zapisanych studentów:", ConsoleColor.Green);
    var topProf = await queryService.PobierzProfesoraZNajwiekszaLiczbaStudentowAsync();
    if (topProf != null)
    {
        Console.WriteLine($" -> {topProf.TytulNaukowy} {topProf.Imie} {topProf.Nazwisko} ({topProf.IndeksUczelniany})");
        Console.WriteLine($" -> Liczba prowadzonych kursów: {topProf.LiczbaProwadzonychKursow}");
        Console.WriteLine($" -> Łączna liczba zapisanych studentów: {topProf.LacznaLiczbaZapisanychStudentow}");
    }
    else
    {
        Console.WriteLine(" -> Brak danych.");
    }

    // ZAPYTANIE 2
    WypiszKolor("\n[Zapytanie 2] Średnia ocen (GPA) dla kursów na wybranym wydziale:", ConsoleColor.Green);
    var wydzialy = await universityService.PobierzWydzialyAsync();
    if (wydzialy.Any())
    {
        foreach (var w in wydzialy)
        {
            Console.WriteLine($"   [{w.Id}] {w.Nazwa}");
        }
        Console.Write("Wybierz ID wydziału z powyższej listy (domyślnie pierwszy): ");
        var wpisaneId = Console.ReadLine();
        int wydzialId = int.TryParse(wpisaneId, out var wid) && wydzialy.Any(w => w.Id == wid)
            ? wid
            : wydzialy.First().Id;

        var kursyGpa = await queryService.PobierzSredniaOcenDlaWydzialuAsync(wydzialId);
        Console.WriteLine($"\nWyniki GPA dla wydziału ID={wydzialId}:");
        foreach (var k in kursyGpa)
        {
            Console.WriteLine($" -> [{k.KodKursu}] {k.NazwaKursu,-42} | GPA: {k.SredniaOcenGpa:F2} | Ocenionych studentów: {k.LiczbaOcenionychStudentow}");
        }
    }

    // ZAPYTANIE 3
    WypiszKolor("\n[Zapytanie 3] Ranking studentów z najtrudniejszym planem zajęć (Unikalne ECTS z prerekwizytami):", ConsoleColor.Green);
    var ranking = await queryService.PobierzRankingNajtrudniejszychPlanowAsync(5);
    int miejsce = 1;
    foreach (var st in ranking)
    {
        Console.WriteLine($" #{miejsce++}. [{st.IndeksUczelniany}] {st.Imie} {st.Nazwisko} (Rok {st.RokStudiow}) -> " +
                          $"Łączne unikalne ECTS: {st.LaczneUnikalnePunktyEcts} pkt " +
                          $"(Zapisane kursy: {st.LiczbaZapisanychKursow}, Razem z unikalnymi prerekwizytami: {st.LiczbaUnikalnychKursowZPrerekwizytami})");
    }
}

static void WypiszKolor(string tekst, ConsoleColor kolor)
{
    var poprzedni = Console.ForegroundColor;
    Console.ForegroundColor = kolor;
    Console.WriteLine(tekst);
    Console.ForegroundColor = poprzedni;
}