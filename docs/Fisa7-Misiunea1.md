# Fișa 7 — Misiunea 1

Aplicația expune trei tool-uri MCP la `/mcp`, folosind ModelContextProtocol.AspNetCore 2.4.0. Implementarea din `Lab1_MVC/Mcp/LibraryTools.cs` folosește fișierul Moodle furnizat, adaptat la namespace-urile și tipul RecommendationDto din proiect. RecommendationService reutilizează logica ML și fallback din aplicație. Clientul folosește Program.cs furnizat, cu portul 7105 și suport pentru MCP_ENDPOINT; modul 2 rămâne disponibil pentru misiunea următoare. Nu este necesar Ollama pentru această misiune.

## Pornire

În primul terminal:

```powershell
dotnet run --project Lab1_MVC --launch-profile https
```

Port HTTPS: **7105**. Endpoint: `https://localhost:7105/mcp`.

În al doilea terminal:

```powershell
dotnet run --project Biblioteca.Agent
```

Alege `1`, apoi introdu `Rebreanu`. Repornește inspectorul pentru căutarea `xyz`.

În Visual Studio: Solution → Configure Startup Projects → Multiple startup projects → Start pentru Lab1_MVC și Biblioteca.Agent; selectează profilul https pentru aplicația web. Dacă inspectorul pornește înaintea serverului, repornește inspectorul după ce serverul ascultă.

Pentru HTTP, pornește serverul cu `--launch-profile http`, apoi în terminalul inspectorului:

```powershell
$env:MCP_ENDPOINT = 'http://localhost:5114/mcp'
dotnet run --project Biblioteca.Agent
```

## Partea C — Răspunsuri

| Tool | Parametri din schema JSON | Originea descrierii |
| --- | --- | --- |
| `search_books` | `query: string`, obligatoriu | Atributul `[Description]` al metodei; descrierea parametrului vine din `[Description]` de pe `query`. |
| `get_book_details` | `bookId: integer`, obligatoriu | Atributul `[Description]` al metodei și al parametrului. |
| `recommend_books` | `readerId: integer`, obligatoriu; `top: integer`, opțional, implicit 5 | Atributul `[Description]` al metodei și al parametrilor. |

**LibraryContext db apare în schema JSON?** Nu. Este un serviciu înregistrat în DI, injectat de server. Clientul nu îl trimite. Nici RecommendationService, nici CancellationToken nu apar în scheme; sunt furnizate de server.

**Ce întoarce search_books pentru Rebreanu? În ce format?** Un bloc de conținut text MCP care conține următorul tablou JSON (diacriticele pot apărea escapate cu `\u`):

```json
[
  { "id": 4, "title": "Ion", "author": "Liviu Rebreanu", "genre": "Roman românesc", "year": 1920, "price": 20.0 },
  { "id": 7, "title": "Pădurea spânzuraților", "author": "Liviu Rebreanu", "genre": "Roman românesc", "year": 1922, "price": 39.0 }
]
```

**Ce întoarce xyz? Este eroare?** `[]`, cu `IsError: False`. Apelul a reușit; catalogul nu conține potriviri.

**Ce informații despre cod are clientul? Vede LINQ?** Clientul primește numele tool-urilor, descrierile, schemele JSON și rezultatele apelurilor. Nu primește codul sursă, interogările LINQ, conexiunea bazei de date sau serviciile injectate.

## Verificări efectuate

- `dotnet build Lab1_MVC.sln --no-restore`: succes, 0 erori. Există avertismente anterioare despre pachete și vulnerabilități în dependențele proiectului.
- Descoperire prin clientul MCP real: exact cele trei tool-uri de mai sus; schemele nu includ serviciile DI.
- Căutări `Rebreanu` și `xyz`: rezultatele prezentate mai sus.
- Verificare inițială, înainte de alinierea la fișierele Moodle: `get_book_details`, cartea 4: Ion, Liviu Rebreanu, 1920, Roman românesc, 20 lei și descrierea din catalog.
- Verificare inițială, înainte de alinierea la fișierele Moodle: `recommend_books`, cititorul 5: cinci rezultate ML, `Fallback: false`.
- Verificare inițială, înainte de alinierea la fișierele Moodle: `recommend_books`, cititorul 999: cinci rezultate populare, `Fallback: true`.

Apelurile au fost verificate pe HTTP, port 5114. HTTPS nu a putut fi pornit în contul izolat de verificare; certificatul de dezvoltare și accesul Data Protection depind de contul Windows. Endpointul implicit al inspectorului rămâne HTTPS, conform fișei.

Documentația SDK folosită: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md și https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/transports/transports.md.

