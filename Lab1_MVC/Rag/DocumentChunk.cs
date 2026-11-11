using Microsoft.Extensions.VectorData;

public class DocumentChunk
{
    [VectorStoreKey]
    public string Id { get; set; } = "";                 // de ex. "regulament-06" sau "carte-4"

    [VectorStoreData(IsIndexed = true)]
    public string Source { get; set; } = "";             // "regulament" sau "carte" 

    [VectorStoreData]
    public string Title { get; set; } = "";              // titlul sectiunii sau al cartii

    [VectorStoreData]
    public string Text { get; set; } = "";               // textul fragmentului (il pastram pentru raspuns)

   
    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public string Embedding { get; set; } = "";
}

// Ce intoarce tool-ul MCP: fragmentul, sursa si scorul de similaritate (mai mare = mai apropiat).
public record DocumentHit(string Source, string Title, string Text, double Score);
