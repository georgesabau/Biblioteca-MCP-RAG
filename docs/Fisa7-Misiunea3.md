# Fișa 7 — Misiunea 3

Experiment din 10 noiembrie 2026, cu Ollama `llama3.2:latest`, folosind același client și același mesaj de sistem ca la misiunea 2. Testele au folosit endpointul HTTP `http://localhost:5114/mcp` în mediul izolat.

## Partea A — Tool-uri noi

În `Lab1_MVC/Mcp/LibraryTools.cs` sunt active:

- `top_books_by_price(mostExpensive: boolean, top: integer = 3)`: ordonează întregul catalog după preț și întoarce între 1 și 10 cărți. `true` cere cele mai scumpe, iar `false` cele mai ieftine. Pentru SQLite se ordonează prin conversie numerică la `double`; prețul întors păstrează tipul `decimal`. ID-ul departajează prețurile egale.
- `books_by_genre(genre: string)`: întoarce cărțile genului indicat prin numele exact, ordonate după titlu. Pentru un gen inexistent întoarce `[]`. Parametrul are descriere, iar contextul bazei de date și CancellationToken sunt injectate de server.

Ambele au atributele MCP `ReadOnly = true`, `Destructive = false`, `OpenWorld = false` și întorc liste de `BookInfo` cu ID, titlu, autor, gen, an și preț.

| Întrebarea | Tool-uri apelate, copiate din consolă | Răspunsul este corect? |
| --- | --- | --- |
| Care este cea mai scumpă carte din catalog? | `[tool] top_books_by_price(mostExpensive=False, top=3)` | Nu. Modelul alege tool-ul nou, dar inversează sensul parametrului boolean, apoi numește Moara cu noroc, 26 lei, drept cea mai scumpă. Catalogul confirmă Domain-Driven Design, 220 lei. |
| Ce cărți din genul Programare are biblioteca? | `[tool] books_by_genre(genre=Programare)` | Da. Enumeră corect cele cinci cărți, autorii și prețurile lor. |

Răspunsul corect pentru genul **Programare**:

| Titlu | Autor | Preț în lei |
| --- | --- | --- |
| C# in Depth | Jon Skeet | 195 |
| Clean Code | Robert C. Martin | 189 |
| Designing Data-Intensive Applications | Martin Kleppmann | 210 |
| Domain-Driven Design | Eric Evans | 220 |
| The Pragmatic Programmer | David Thomas | 175 |

Apelurile MCP directe de verificare, separate de întrebările adresate modelului, confirmă:

- `top_books_by_price(true, 1)` → Domain-Driven Design, ID 32, 220 lei.
- `top_books_by_price(false, 1)` → Ion, ID 4, 20 lei.
- `books_by_genre("Programare")` → cele cinci titluri de mai sus.
- `books_by_genre("xyz")` → `[]`, fără eroare.

Rezultatele brute sunt în `Fisa7-Misiunea3-verificari.json`; dialogul este în `Fisa7-Misiunea3-A-transcript.txt`.

## Partea B — Descrierea contează

Pentru fiecare variantă se modifică numai elementul cerut al tool-ului de căutare, se compilează și se repornește serverul, apoi se pornește un client nou. Întrebarea este mereu: **Ce cărți de Rebreanu are biblioteca?** După fiecare variantă se restaurează codul original. Cele două tool-uri adăugate la partea A rămân disponibile.

Rezultatele sunt observații ale acestei rulări, nu predicții ale comportamentului modelului. Modificările experimentale nu sunt păstrate în codul final.

| # | Schimbarea | Ce a făcut agentul? |
| --- | --- | --- |
| B1 | Descrierea metodei devine „Funcția 1.” | Apelează `[tool] search_books(query=Rebreanu)`. Menționează Ion (1920), omite Pădurea spânzuraților și introduce afirmația „În patru zile nu a fost găsit în catalog”, deși acel titlu nu vine din tool. Răspuns incomplet și infidel rezultatului. |
| B2 | Se elimină `[Description]` de pe parametrul `query` | Apelează `[tool] search_books(query=Rebreanu)`. Întoarce Ion și titlul inventat „Pădurea spinoasă a vânzorilor”. Lipsa descrierii parametrului nu a împiedicat alegerea tool-ului în această rulare, dar răspunsul final nu este fidel. |
| B3 | Numele devine `f1`, descrierile rămân | Apelează `[tool] f1(query=Rebreanu)`. Oferă anii și prețurile corecte, dar deformează al doilea titlu în „Pădurea spânzurătoare”. Numele opac nu a împiedicat folosirea tool-ului cu descrierile păstrate. |
| B4 | Descrierea greșită „Întoarce lista de autori.” | Apelează `[tool] search_books(query=Rebreanu)`. Enumeră Ion (1920) și „Pădurea spăzuților” (1922). În această rulare numele tool-ului și descrierea parametrului au fost suficiente pentru a-l selecta, dar al doilea titlu este deformat. |

Transcrierile integrale sunt în fișierele `Fisa7-Misiunea3-B1-transcript.txt` până la `Fisa7-Misiunea3-B4-transcript.txt`. În toate variantele agentul a ales tool-ul de căutare. O singură rulare pe variantă nu demonstrează o relație cauzală între descriere și deformările titlurilor; asemenea deformări au apărut și cu descrierile originale la misiunea 2. Descrierile corecte rămân necesare pentru a exprima contractul tool-ului, dar nu garantează un răspuns fidel din partea acestui model.

## Starea finală

`search_books` are numele, descrierea metodei și descrierea parametrului originale. Nu a rămas activă nicio modificare B1–B4. Tool-urile de preț și gen rămân active. Compilarea finală a soluției a trecut cu 0 erori și 5 avertismente existente despre dependențe.

Pentru rulare normală: pornește `dotnet run --project Lab1_MVC --launch-profile https`, apoi în alt terminal `dotnet run --project Biblioteca.Agent` și alege `2`. Modelul local folosit este `llama3.2`.
