namespace University.Domain;

public class Gabinet
{
    public int Id { get; set; }
    public string NumerPokoju { get; set; } = null!;
    public string Budynek { get; set; } = null!;

    public int ProfesorId { get; set; }
    public Profesor Profesor { get; set; } = null!;
}