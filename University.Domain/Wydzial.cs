namespace University.Domain;

public class Wydzial
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = null!;

    public ICollection<Kurs> Kursy { get; set; } = new List<Kurs>();
}