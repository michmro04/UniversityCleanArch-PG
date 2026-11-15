namespace University.Domain;

public class Kurs
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = null!;
    public string KodKursu { get; set; } = null!;
    public int PunktyECTS { get; set; }

    public int WydzialId { get; set; }
    public Wydzial Wydzial { get; set; } = null!;

    public int ProwadzacyId { get; set; }
    public Profesor Prowadzacy { get; set; } = null!;

    public ICollection<Enrollment> Zapisy { get; set; } = new List<Enrollment>();

    public ICollection<Kurs> WymaganiaWstepne { get; set; } = new List<Kurs>();
    public ICollection<Kurs> KursyZalezne { get; set; } = new List<Kurs>();
}