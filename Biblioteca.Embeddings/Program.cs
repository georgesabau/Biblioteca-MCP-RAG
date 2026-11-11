// Biblioteca.Embeddings 
using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

const string OllamaUri = "http://localhost:11434";
const string Model = "embeddinggemma";                  

IEmbeddingGenerator<string, Embedding<float>> gen = new OllamaApiClient(new Uri(OllamaUri), Model);

Console.Write($"Model: {Model}\n1 Vector · 2 Similarități · 3 Căutare în catalog > ");
switch (Console.ReadLine())
{
    case "1": await Vector(); break;
    case "2": await Similaritati(); break;
    default: await Cautare(); break;
}

//  1.cum arata un embedding 
async Task Vector()
{
    while (true)
    {
        Console.Write("\nText (Enter = ieșire): ");
        var text = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(text)) break;

        ReadOnlyMemory<float> v = await gen.GenerateVectorAsync(text);
        Console.WriteLine($"Dimensiune: {v.Length}");
        Console.WriteLine("Primele 8 valori: " +
            string.Join("  ", v.ToArray().Take(8).Select(x => x.ToString("F4"))));
    }
}

// 2 similaritatea cosinus intre propozitii
async Task Similaritati()
{
    string[] p =
    [
        "A: Pot împrumuta o carte pentru două săptămâni?",
        "B: Care este durata unui împrumut?",
        "C: Cât costă dacă întârzii cu returnarea?",
        "D: Ce penalitate primesc pentru o carte adusă mai târziu?",
        "E: Rețetă de sarmale cu smântână.",
        "F: How long can I borrow a book?"
    ];
    GeneratedEmbeddings<Embedding<float>> e = await gen.GenerateAsync(p);

    foreach (var s in p) Console.WriteLine(s);
    Console.Write("\n     ");
    for (int j = 0; j < p.Length; j++) Console.Write($"   {p[j][0]}  ");
    Console.WriteLine();
    for (int i = 0; i < p.Length; i++)
    {
        Console.Write($"  {p[i][0]}  ");
        for (int j = 0; j < p.Length; j++)
            Console.Write($" {Cosinus(e[i].Vector.Span, e[j].Vector.Span):F3}");
        Console.WriteLine();
    }
}

// similaritatea cosinus: produsul scalar impartit la produsul lungimilor (intre -1 și 1)
static double Cosinus(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
{
    double dot = 0, na = 0, nb = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        na += a[i] * a[i];
        nb += b[i] * b[i];
    }
    return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
}

// 3.cautare după sens in catalog 
async Task Cautare()
{
    var store = new InMemoryVectorStore(new() { EmbeddingGenerator = gen });
    VectorStoreCollection<int, CarteVector> carti = store.GetCollection<int, CarteVector>("carti");
    await carti.EnsureCollectionExistsAsync();

    foreach (var c in CitesteCatalog("books.csv"))
        await carti.UpsertAsync(c);                        // embedding-ul se calculează aici, automat
    Console.WriteLine("Catalogul a fost indexat (40 de cărți).");

    while (true)
    {
        Console.Write("\nCe fel de carte căutați? (Enter = ieșire): ");
        var q = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(q)) break;

        await foreach (VectorSearchResult<CarteVector> r in carti.SearchAsync(q, top: 3))
            Console.WriteLine($"[{r.Score:F3}] {r.Record.Titlu} · {r.Record.Descriere}");
    }
}

// citeste books.csv (BookId,Title,AuthorFirstName,AuthorLastName,Genre,Year,Price,Description)
static IEnumerable<CarteVector> CitesteCatalog(string path)
{
    foreach (var line in File.ReadLines(path).Skip(1))
    {
        var f = CsvLine(line);
        var descriere = $"{f[1]}, de {f[2]} {f[3]} ({f[4]}). {f[7]}";
        yield return new CarteVector { Id = int.Parse(f[0]), Titlu = f[1], Descriere = f[7], Text = descriere };
    }
}

// imparte o linie CSV, respectand campurile intre ghilimele
static List<string> CsvLine(string line)
{
    var fields = new List<string>();
    var cur = new System.Text.StringBuilder();
    bool quoted = false;
    for (int i = 0; i < line.Length; i++)
    {
        char ch = line[i];
        if (ch == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
        else if (ch == '"') quoted = !quoted;
        else if (ch == ',' && !quoted) { fields.Add(cur.ToString()); cur.Clear(); }
        else cur.Append(ch);
    }
    fields.Add(cur.ToString());
    return fields;
}

class CarteVector
{
    [VectorStoreKey]
    public int Id { get; set; }

    [VectorStoreData]
    public string Titlu { get; set; } = "";

    [VectorStoreData]
    public string Descriere { get; set; } = "";

    // textul din care se calculează vectorul (titlu, autor, gen, descriere)
    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public string Text { get; set; } = "";
}
