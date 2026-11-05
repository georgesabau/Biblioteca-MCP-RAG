namespace Lab1_MVC.Models;

public class AuthorDetailsViewModel
{
    public Author Author { get; init; } = new();
    public IReadOnlyList<BookListItem> Books { get; init; } = [];
    public int TotalOrders { get; init; }
    public int DistinctGenres { get; init; }
}
