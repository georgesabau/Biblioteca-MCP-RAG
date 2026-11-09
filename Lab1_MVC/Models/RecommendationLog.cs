namespace Lab1_MVC.Models;
public class RecommendationLog
{
    public int ID { get; set; }
    public int ReaderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string BookIds { get; set; } = "[]";
}
