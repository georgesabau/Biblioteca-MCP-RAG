// Mcp/LibraryTools.cs
using Lab1_MVC.Data;
using Lab1_MVC.Models;
using Lab1_MVC.Services;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace Lab1_MVC.Mcp;

public record BookInfo(int Id, string Title, string? Author, string? Genre, int Year, decimal Price);

public record BookDetails(int Id, string Title, string? Author, string? Genre, int Year,
                          decimal Price, string? Description, int Orders);

[McpServerToolType]
public class LibraryTools
{
    [McpServerTool(Name = "search_books", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Cauta carti in catalogul Bibliotecii dupa un cuvant din titlu sau din numele autorului. " +
                 "Intoarce cel mult 10 carti, cu ID, titlu, autor, gen, an si pret.")]
    public static async Task<List<BookInfo>> SearchBooks(
        LibraryContext db,                                    
        [Description("Cuvantul cautat, de exemplu 'Rebreanu' sau 'Ion'.")] string query,
        CancellationToken ct)
    {
        var q = query.Trim();
        return await db.Book.AsNoTracking()
            .Where(b => b.Title.Contains(q)
                     || (b.Author != null && (b.Author.LastName.Contains(q) || b.Author.FirstName.Contains(q))))
            .OrderBy(b => b.Title)
            .Take(10)
            .Select(b => new BookInfo(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name,
                b.Year, b.Price))
            .ToListAsync(ct);
    }

    [McpServerTool(Name = "get_book_details", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Intoarce detaliile unei carti dupa ID: autor, gen, an, pret, descriere si numarul de comenzi. " +
                 "Daca nu stii ID-ul, cauta intai cartea cu search_books.")]
    public static async Task<BookDetails?> GetBookDetails(
        LibraryContext db,
        [Description("ID-ul cartii din catalog.")] int bookId,
        CancellationToken ct)
    {
        return await db.Book.AsNoTracking()
            .Where(b => b.ID == bookId)
            .Select(b => new BookDetails(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name,
                b.Year, b.Price, b.Description, b.Orders.Count()))
            .FirstOrDefaultAsync(ct);
    }

    [McpServerTool(Name = "recommend_books", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Recomandă carti unui cititor, dupa evaluarile lui (modelul ML.NET al Bibliotecii). " +
                 "Pentru un cititor fara evaluari intoarce cele mai bine evaluate carti (Fallback = true).")]
    public static async Task<List<RecommendationDto>> RecommendBooks(
        RecommendationService recs,                           // serviciul din I6
        [Description("ID-ul cititorului (ReaderId).")] int readerId,
        [Description("Câte recomandări (1–10). Implicit 5.")] int top = 5,
        CancellationToken ct = default)
    {
        return await recs.RecommendAsync(readerId, Math.Clamp(top, 1, 10), ct);
    }

    // Misiunea 3
    [McpServerTool(Name = "top_books_by_price", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Intoarce cele mai scumpe sau cele mai ieftine carti din catalog.")]
    public static async Task<List<BookInfo>> TopBooksByPrice(
        LibraryContext db,
        [Description("true = cele mai scumpe; false = cele mai ieftine.")] bool mostExpensive,
        [Description("Câte cărți (1–10). Implicit 3.")] int top = 3,
        CancellationToken ct = default)
    {
        var books = db.Book.AsNoTracking();
        books = mostExpensive ? books.OrderByDescending(b => (double)b.Price).ThenBy(b => b.ID) : books.OrderBy(b => (double)b.Price).ThenBy(b => b.ID);
        return await books.Take(Math.Clamp(top, 1, 10))
            .Select(b => new BookInfo(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name, b.Year, b.Price))
            .ToListAsync(ct);
    }
    [McpServerTool(Name = "books_by_genre", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Întoarce cărțile unui gen din catalog, cu ID, titlu, autor, gen, an și preț. De exemplu: Programare sau Roman românesc.")]
    public static async Task<List<BookInfo>> BooksByGenre(
        LibraryContext db,
        [Description("Numele exact al genului, de exemplu Programare.")] string genre,
        CancellationToken ct = default)
    {
        var name = genre.Trim();
        return await db.Book.AsNoTracking()
            .Where(b => b.Genre != null && b.Genre.Name == name)
            .OrderBy(b => b.Title).ThenBy(b => b.ID)
            .Select(b => new BookInfo(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name, b.Year, b.Price))
            .ToListAsync(ct);
    }
}
