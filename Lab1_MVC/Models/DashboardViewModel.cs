namespace Lab1_MVC.Models;
public record PublisherRating(string Name, double? Average);
public record DashboardViewModel(int Ratings, int Readers, double Average, int[] StarCounts, List<PublisherRating> Publishers);
