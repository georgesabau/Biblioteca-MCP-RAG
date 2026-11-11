// Agentul din I7 il descopera singur (tools/list), fara modificari de cod.
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class DocumentTools
{
    [McpServerTool(Name = "search_documents", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Caută după sens în regulamentul Bibliotecii și în descrierile cărților. " +
                 "Folosește-l pentru întrebări despre reguli (împrumut, termene, penalități, program, sala de lectură) " +
                 "și când cititorul descrie subiectul unei cărți, nu titlul ei. " +
                 "Întoarce fragmentele cele mai apropiate de întrebare, cu sursa, titlul și scorul de similaritate (0–1).")]
    public static Task<List<DocumentHit>> SearchDocuments(
        DocumentIndex index,                                   
        [Description("Intrebarea sau descrierea, in cuvintele cititorului.")] string query,
        [Description("Unde caută: 'regulament', 'carte' sau gol pentru ambele.")] string? source = null,
        [Description("Cate fragmente (1–8). Implicit 3.")] int top = 3,
        CancellationToken ct = default)
    {
        var src = string.IsNullOrWhiteSpace(source) ? null : source.Trim().ToLowerInvariant();
        return index.SearchAsync(query, Math.Clamp(top, 1, 8), src, ct);
    }
}
