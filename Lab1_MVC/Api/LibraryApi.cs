using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Lab1_MVC.Data;
using Lab1_MVC.Models;

namespace Lab1_MVC.Api;

public record BookDto(int Id, string Title, string? Author, string? Genre, decimal Price);
public record NewBookDto([Required, StringLength(200)] string Title, [Range(1.0, 10000.0)] decimal Price, int? AuthorID, int? GenreID);
public record PriceDto([Range(1.0, 10000.0)] decimal Price);

public static class LibraryApi
{
    public static void MapLibraryApi(this WebApplication app)
    {
        var books = app.MapGroup("/api/v2/books").WithTags("Books · Minimal API");

        // Lista cartilor
        books.MapGet("/", async (string? genre, int? page, LibraryContext db) =>
        {
            const int pageSize = 5;
            var p = Math.Max(page ?? 1, 1);
            var list = await db.Book
                .Where(b => genre == null || b.Genre!.Name == genre)
                .OrderBy(b => b.ID)
                .Skip((p - 1) * pageSize).Take(pageSize)
                .Select(b => new BookDto(b.ID, b.Title,
                    b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                    b.Genre != null ? b.Genre.Name : null, b.Price))
                .ToListAsync();
            return Results.Ok(list);
        });

        // Detaliile unei cărți
       
        books.MapGet("/{id:int}", async Task<Results<Ok<BookDto>, NotFound>> (int id, LibraryContext db) =>
        {
            var dto = await db.Book.Where(b => b.ID == id)
                .Select(b => new BookDto(b.ID, b.Title,
                    b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                    b.Genre != null ? b.Genre.Name : null, b.Price))
                .FirstOrDefaultAsync();
            return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
        });

        // Adaugă o carte 
        books.MapPost("/", async (NewBookDto input, LibraryContext db) =>
        {
            var book = new Book { Title = input.Title, Price = input.Price,
                                  AuthorID = input.AuthorID, GenreID = input.GenreID };
            db.Book.Add(book);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v2/books/{book.ID}",
                new BookDto(book.ID, book.Title, null, null, book.Price));
        });

        // Modifică prețul
        books.MapPatch("/{id:int}", async (int id, PriceDto input, LibraryContext db) =>
        {
            var book = await db.Book.FindAsync(id);
            if (book is null) return Results.NotFound();
            book.Price = input.Price;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Șterge o carte
        books.MapDelete("/{id:int}", async (int id, LibraryContext db) =>
        {
            var book = await db.Book.FindAsync(id);
            if (book is null) return Results.NotFound();
            db.Book.Remove(book);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
