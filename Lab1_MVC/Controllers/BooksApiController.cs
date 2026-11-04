using Lab1_MVC.Api;
using Lab1_MVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab1_MVC.Controllers;

[ApiController]
[Route("api/books")]
public class BooksApiController(LibraryContext db) : ControllerBase
{
    // GET /api/books?genre=SF
    [HttpGet]
    public async Task<IEnumerable<BookDto>> GetBooks(string? genre)
    {
        return await db.Book
            .Where(b => genre == null || b.Genre!.Name == genre)
            .OrderBy(b => b.ID)
            .Select(b => new BookDto(b.ID, b.Title,
                b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                b.Genre != null ? b.Genre.Name : null, b.Price))
            .ToListAsync();
    }

    // GET /api/books/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        var dto = await db.Book
            .Where(b => b.ID == id)
            .Select(b => new BookDto(b.ID, b.Title,
                b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                b.Genre != null ? b.Genre.Name : null, b.Price))
            .FirstOrDefaultAsync();

        if (dto is null) return NotFound();
        return dto;
    }
}
