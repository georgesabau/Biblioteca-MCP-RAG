using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lab1_MVC.Data;
using Microsoft.Extensions.ML;
using Lab1_MVC.Models;
using Lab1_MVC.Api;
using Scalar.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

using Lab1_MVC.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LibraryContext") ?? throw new InvalidOperationException("Connection string 'LibraryContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
    .AddPredictionEnginePool<BookRating, BookRatingPrediction>()
    .FromFile(modelName: "BookRecommender",
        filePath: "MLModels/BookRecommender.zip",
        watchForChanges: true);

builder.Services.AddScoped<RecommenderTrainer>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

IChatClient chat = new OpenAIClient(
    new ApiKeyCredential("ollama"),
    new OpenAIClientOptions
    {
        Endpoint = new Uri("http://localhost:11434/v1")
    })
    .GetChatClient("llama3.2")
    .AsIChatClient();

builder.Services.AddChatClient(chat);

var app = builder.Build();
app.MapLibraryApi();
app.MapGet("/api/v2/readers/{id:int}/recommendations", async (int id, int? top, LibraryContext db, PredictionEnginePool<BookRating, BookRatingPrediction> pool) =>
{
    var n = Math.Clamp(top ?? 5, 1, 20);
    var rated = await db.Rating.Where(r => r.ReaderId == id).Select(r => r.BookID).ToListAsync();
    var recs = new List<RecommendationDto>();
    if (rated.Count > 0)
    {
        var candidates = await db.Book.AsNoTracking().Where(b => !rated.Contains(b.ID)).Select(b => new {b.ID,b.Title}).ToListAsync();
        recs = candidates.Select(b => new RecommendationDto(b.ID,b.Title,pool.Predict("BookRecommender", new BookRating {UserId=id,BookId=b.ID}).Score,false))
            .Where(r => float.IsFinite(r.Score)).OrderByDescending(r => r.Score).ThenBy(r => r.BookId).Take(n).ToList();
    }
    if (recs.Count == 0)
    {
        var popular = await db.Rating.Where(r => !rated.Contains(r.BookID)).GroupBy(r => new {r.BookID,r.Book.Title})
            .Where(g => g.Count() >= 12).Select(g => new {g.Key.BookID,g.Key.Title,Score=g.Average(r => r.Stars)})
            .OrderByDescending(r => r.Score).ThenBy(r => r.BookID).Take(n).ToListAsync();
        recs = popular.Select(r => new RecommendationDto(r.BookID,r.Title,(float)r.Score,true)).ToList();
    }
    return TypedResults.Ok(recs);
}).WithTags("Recomandări");

app.MapPost("/api/v2/books/{id:int}/pitch", async (int id, LibraryContext db, IChatClient ai) =>
{
    var b = await db.Book.Include(x => x.Author).Include(x => x.Genre)
        .FirstOrDefaultAsync(x => x.ID == id);
    if (b is null) return Results.NotFound();

    var r = await ai.GetResponseAsync(
    [
        new ChatMessage(ChatRole.System, "Ești bibliotecar. Răspunde în română, în cel mult 2 propoziții."),
        new ChatMessage(ChatRole.User, $"Recomandă „{b.Title}” de {b.Author?.FullName} ({b.Genre?.Name}). Descriere: {b.Description}")
    ]);
    return Results.Ok(new { b.Title, Pitch = r.Text });
});

app.MapGet("/api/v2/ask", async (string q, IChatClient ai) => (await ai.GetResponseAsync(q)).Text);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.Migrate();
    LibrarySeed.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "start",
    pattern: "start",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();