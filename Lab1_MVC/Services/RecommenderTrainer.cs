using Lab1_MVC.Data;
using Lab1_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Trainers;
namespace Lab1_MVC.Services;
public class RecommenderTrainer(LibraryContext db, IWebHostEnvironment env)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public async Task<(double Rmse, int Count)> TrainAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var ratings = await db.Rating.AsNoTracking().Select(r => new BookRating {UserId=r.ReaderId, BookId=r.BookID, Rating=r.Stars}).ToListAsync();
            if (ratings.Count < 10) throw new InvalidOperationException("Importați cel puțin 10 evaluări înainte de antrenare.");
            var ml = new MLContext(seed:1);
            var data = ml.Data.LoadFromEnumerable(ratings);
            var split = ml.Data.TrainTestSplit(data, testFraction:0.2);
            var pipeline = ml.Transforms.Conversion.MapValueToKey("UserKey", "UserId")
                .Append(ml.Transforms.Conversion.MapValueToKey("BookKey", "BookId"))
                .Append(ml.Recommendation().Trainers.MatrixFactorization(new MatrixFactorizationTrainer.Options {
                    MatrixColumnIndexColumnName="UserKey", MatrixRowIndexColumnName="BookKey", LabelColumnName="Label", NumberOfIterations=20, ApproximationRank=16 }));
            var model = pipeline.Fit(split.TrainSet);
            var scored = ml.Data.CreateEnumerable<BookRatingPrediction>(model.Transform(split.TestSet), false).ToList();
            if (!scored.Any(p => float.IsFinite(p.Score))) throw new InvalidOperationException("Date insuficiente pentru evaluarea modelului.");
            // Unseen reader/book keys have NaN scores and cannot contribute to RMSE.
            var predictions = model.Transform(split.TestSet);
            var finite = ml.Data.FilterRowsByColumn(predictions, "Score", double.NegativeInfinity, double.PositiveInfinity);
            var metrics = ml.Regression.Evaluate(finite);
            var path = Path.Combine(env.ContentRootPath,"MLModels","BookRecommender.zip");
            var temp = path + ".tmp";
            ml.Model.Save(model,data.Schema,temp);
            System.IO.File.Move(temp,path,true);
            return (metrics.RootMeanSquaredError, ratings.Count);
        }
        finally { Gate.Release(); }
    }
}
