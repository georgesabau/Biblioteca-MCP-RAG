using Microsoft.ML.Data;
namespace Lab1_MVC.Models;
public class BookText
{
    [LoadColumn(0)] public string Description { get; set; } = "";
    [LoadColumn(1)] public string Genre { get; set; } = "";
}
public class GenrePrediction
{
    [ColumnName("PredictedLabel")] public string Genre { get; set; } = "";
    public float[] Score { get; set; } = [];
}
