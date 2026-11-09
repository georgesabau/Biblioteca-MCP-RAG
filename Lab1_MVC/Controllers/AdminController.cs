using Lab1_MVC.Data;
using Lab1_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
namespace Lab1_MVC.Controllers;
public class AdminController(LibraryContext db, Lab1_MVC.Services.RecommenderTrainer trainer) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Retrain()
    {
        try { var (rmse,count) = await trainer.TrainAsync(); TempData["Msg"] = $"Model reantrenat pe {count} evaluări. RMSE = {rmse:0.0000}"; }
        catch (InvalidOperationException e) { TempData["Msg"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Index()
    {
        ViewBag.Ratings = await db.Rating.CountAsync();
        return View();
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> ImportRatings(IFormFile? file)
    {
        if (file is null || file.Length == 0) { TempData["Msg"] = "Alegeți un fișier CSV."; return RedirectToAction(nameof(Index)); }
        var books = (await db.Book.Select(b => b.ID).ToListAsync()).ToHashSet();
        var existing = (await db.Rating.Select(r => new {r.ReaderId, r.BookID}).ToListAsync()).Select(r => (r.ReaderId,r.BookID)).ToHashSet();
        var rows = new List<Rating>(); int invalid = 0, duplicates = 0;
        using var parser = new TextFieldParser(file.OpenReadStream());
        parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
        var header = parser.ReadFields();
        if (header is null || !header.SequenceEqual(new[] { "UserId", "BookId", "Rating" }))
        { TempData["Msg"] = "Antet invalid: se așteaptă UserId,BookId,Rating."; return RedirectToAction(nameof(Index)); }
        while (!parser.EndOfData)
        {
            string[]? p;
            try { p = parser.ReadFields(); } catch (MalformedLineException) { invalid++; continue; }
            if (p is null || p.Length != 3 || !int.TryParse(p[0], out var u) || u <= 0 || !int.TryParse(p[1], out var b) || !books.Contains(b) || !int.TryParse(p[2], out var stars) || stars < 1 || stars > 5) { invalid++; continue; }
            if (!existing.Add((u,b))) { duplicates++; continue; }
            rows.Add(new Rating { ReaderId=u, BookID=b, Stars=stars, CreatedAt=DateTime.UtcNow });
        }
        db.Rating.AddRange(rows); await db.SaveChangesAsync();
        TempData["Msg"] = $"Au fost importate {rows.Count} evaluări; {invalid} rânduri invalide, {duplicates} duplicate ignorate.";
        return RedirectToAction(nameof(Index));
    }
}
