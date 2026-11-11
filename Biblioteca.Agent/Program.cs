// Biblioteca.Agent (actualizat pentru I8)
// 1 = inspector (fara model de limbaj): ce vede clientul MCP
// 2 = agentul: Ollama + tool-urile MCP ale Bibliotecii
// 3 = NOU:cautare în documente (search_documents), fara model: fragmentele si scorurile
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using System.ClientModel;

// Portul https al aplicatiei Biblioteca (launchSettings.json)
const string McpEndpoint = "https://localhost:7105/mcp";
const string OllamaEndpoint = "http://localhost:11434/v1";
const string Model = "llama3.2";

var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri(McpEndpoint)
});
await using McpClient mcp = await McpClient.CreateAsync(transport);
IList<McpClientTool> tools = await mcp.ListToolsAsync();

Console.Write("1 Inspector; 2 Agent; 3 Cautare in documente > ");
switch (Console.ReadLine())
{
    case "1": await Inspector(); break;
    case "3": await CautareDocumente(); break;
    default: await Agent(); break;
}

// I8 
async Task CautareDocumente()
{
    while (true)
    {
        Console.Write("\nÎntrebare (Enter = ieșire): ");
        var query = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(query)) break;
        Console.Write("Sursa (regulament / carte / Enter = ambele): ");
        var source = Console.ReadLine();
        Console.Write("Câte fragmente (1–8, Enter = 3): ");
        int top = int.TryParse(Console.ReadLine(), out var k) ? k : 3;

        CallToolResult result = await mcp.CallToolAsync("search_documents",
            new Dictionary<string, object?> { ["query"] = query, ["source"] = source, ["top"] = top });

        if (result.IsError == true)
        {
            foreach (var b in result.Content.OfType<TextContentBlock>()) Console.WriteLine($"Eroare: {b.Text}");
            continue;
        }
        foreach (var block in result.Content.OfType<TextContentBlock>())
        {
            using var doc = JsonDocument.Parse(block.Text);
            int i = 1;
            foreach (var hit in doc.RootElement.EnumerateArray())
            {
                Console.WriteLine($"\n{i++}. [{Camp(hit, "score").GetDouble():F3}] " +
                                  $"{Camp(hit, "source")} · {Camp(hit, "title")}");
                Console.WriteLine($"   {Camp(hit, "text")}");
            }
        }
    }
}

// campul unui obiect JSON, indiferent de litere mari/mici (Score sau score)
static JsonElement Camp(JsonElement e, string name) =>
    e.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

// M1
async Task Inspector()
{
    foreach (var t in tools)
    {
        Console.WriteLine($"\n== {t.Name}\n{t.Description}");
        Console.WriteLine(JsonSerializer.Serialize(t.JsonSchema,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    Console.Write("\nCăutați în catalog (search_books): ");
    var query = Console.ReadLine() ?? "";
    CallToolResult result = await mcp.CallToolAsync("search_books",
        new Dictionary<string, object?> { ["query"] = query });

    Console.WriteLine($"IsError: {result.IsError}");
    foreach (var block in result.Content.OfType<TextContentBlock>())
        Console.WriteLine(block.Text);
}

// M2
async Task Agent()
{
    IChatClient chat = new ChatClientBuilder(
            new OpenAIClient(new ApiKeyCredential("ollama"),
                             new OpenAIClientOptions { Endpoint = new Uri(OllamaEndpoint) })
                .GetChatClient(Model)
                .AsIChatClient())
        .UseFunctionInvocation()        // execută automat tool-urile cerute de model
        .Build();

    List<ChatMessage> messages =
    [
        new(ChatRole.System,
            "Ești asistentul Bibliotecii FSEGA. Răspunzi în limba română. " +
            "Pentru orice informație despre cărți, cititori sau recomandări folosește tool-urile. " +
            "Nu inventa titluri, prețuri sau autori: dacă tool-urile nu întorc informația, spune că nu o ai. " +
            // I8: regulile vin din regulament, prin search_documents
            "Pentru reguli ale bibliotecii (împrumut, termene, penalități, program) folosește search_documents " +
            "și răspunde doar din fragmentele găsite, menționând secțiunea din regulament. " +
            "Dacă fragmentele nu conțin răspunsul, spune că regulamentul nu precizează.")
    ];
    var options = new ChatOptions { Tools = [.. tools] };

    Console.WriteLine($"Tool-uri disponibile: {string.Join(", ", tools.Select(t => t.Name))}");
    while (true)
    {
        Console.Write("\nTu: ");
        var question = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(question)) break;

        messages.Add(new(ChatRole.User, question));
        ChatResponse response = await chat.GetResponseAsync(messages, options);
        messages.AddMessages(response);

        // urma agentului: ce tool-uri a apelat si cu ce argumente
        foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        {
            var args = string.Join(", ", (call.Arguments ?? new Dictionary<string, object?>())
                .Select(a => $"{a.Key}={a.Value}"));
            Console.WriteLine($"  [tool] {call.Name}({args})");
        }
        Console.WriteLine($"Asistent: {response.Text}");
    }
}
