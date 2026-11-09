using Microsoft.ML;
using Lab1_MVC.Models;
var ml = new MLContext(seed:1);
var source = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory,"book-descriptions.csv");
var data = ml.Data.LoadFromTextFile<BookText>(source,hasHeader:true,separatorChar:',',allowQuoting:true);
var split = ml.Data.TrainTestSplit(data,testFraction:0.2);
var pipeline = ml.Transforms.Conversion.MapValueToKey("Label",nameof(BookText.Genre))
    .Append(ml.Transforms.Text.FeaturizeText("Features",nameof(BookText.Description)))
    .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label","Features"))
    .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
var model = pipeline.Fit(split.TrainSet);
var m = ml.MulticlassClassification.Evaluate(model.Transform(split.TestSet));
Console.WriteLine($"MicroAccuracy: {m.MicroAccuracy:P4}");
Console.WriteLine($"MacroAccuracy: {m.MacroAccuracy:P4}");
Console.WriteLine(m.ConfusionMatrix.GetFormattedConfusionTable());
var output = args.Length > 1 ? args[1] : "GenreClassifier.zip";
ml.Model.Save(model,data.Schema,output);
var engine = ml.Model.CreatePredictionEngine<BookText,GenrePrediction>(model);
foreach (var text in new[] {"Pe planeta deșertică Arrakis, Paul Atreides luptă pentru controlul mirodeniei și destinul omenirii.","un detectiv pe o stație spațială"})
{
    var p = engine.Predict(new BookText {Description=text});
    Console.WriteLine($"{text} => {p.Genre}: {p.Score.Max():P4}");
}
