namespace University.Domain;

public class StudentStudiowMagisterskich : Student
{
    public string TematPracyDyplomowej { get; set; } = null!;

    public int? PromotorId { get; set; }
    public Profesor? Promotor { get; set; }
}