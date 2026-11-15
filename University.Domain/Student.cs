namespace University.Domain;

public class Student
{
    public int Id { get; set; }
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string IndeksUczelniany { get; set; } = null!;
    public int RokStudiow { get; set; }
    public Adres AdresZamieszkania { get; set; } = null!;

    public ICollection<Enrollment> Zapisy { get; set; } = new List<Enrollment>();
}