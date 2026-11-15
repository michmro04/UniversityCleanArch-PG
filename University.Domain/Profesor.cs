namespace University.Domain;

public class Profesor
{
    public int Id { get; set; }
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string IndeksUczelniany { get; set; } = null!;
    public string TytulNaukowy { get; set; } = null!;
    public Adres AdresZamieszkania { get; set; } = null!;

    public Gabinet? Gabinet { get; set; }
    public ICollection<Kurs> ProwadzoneKursy { get; set; } = new List<Kurs>();
    public ICollection<StudentStudiowMagisterskich> Dyplomanci { get; set; } = new List<StudentStudiowMagisterskich>();
}