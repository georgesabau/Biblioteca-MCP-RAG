using Lab1_MVC.Data;
using Lab1_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;

namespace Lab1_MVC.Services;

public class RecommendationService(LibraryContext db, PredictionEnginePool<BookRating, BookRatingPrediction> pool)
{
    public async Task<List<RecommendationDto>> RecommendAsync(int readerId, int top = 5, CancellationToken ct = default)
    {
        var count = Math.Clamp(top, 1, 10);
        var rated = await db.Rating.Where(r => r.ReaderId == readerId).Select(r => r.BookID).ToListAsync(ct);
        var recommendations = new List<RecommendationDto>();
        if (rated.Count > 0)
        {
            var candidates = await db.Book.AsNoTracking().Where(b => !rated.Contains(b.ID))
                .Select(b => new { b.ID, b.Title }).ToListAsync(ct);
            recommendations = candidates.Select(b => new RecommendationDto(b.ID, b.Title,
                    pool.Predict("BookRecommender", new BookRating { UserId = readerId, BookId = b.ID }).Score, false))
                .Where(r => float.IsFinite(r.Score)).OrderByDescending(r => r.Score).ThenBy(r => r.BookId).Take(count).ToList();
        }
        if (recommendations.Count == 0)
        {
            var popular = await db.Rating.Where(r => !rated.Contains(r.BookID))
                .GroupBy(r => new { r.BookID, r.Book.Title }).Where(g => g.Count() >= 12)
                .Select(g => new { g.Key.BookID, g.Key.Title, Score = g.Average(r => r.Stars) })
                .OrderByDescending(r => r.Score).ThenBy(r => r.BookID).Take(count).ToListAsync(ct);
            recommendations = popular.Select(r => new RecommendationDto(r.BookID, r.Title, (float)r.Score, true)).ToList();
        }
        return recommendations;
    }
}

