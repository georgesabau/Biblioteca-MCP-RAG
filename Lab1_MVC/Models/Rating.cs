using System.ComponentModel.DataAnnotations;
namespace Lab1_MVC.Models;
public class Rating
{
    public int ID { get; set; }
    [Range(1, int.MaxValue)] public int ReaderId { get; set; }
    public int BookID { get; set; }
    public Book Book { get; set; } = null!;
    [Range(1, 5)] public int Stars { get; set; }
    public DateTime CreatedAt { get; set; }
}
