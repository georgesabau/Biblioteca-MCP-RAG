# Fișa 7 — Misiunea 2

Experiment din 10 noiembrie 2026, cu Ollama și modelul local `llama3.2:latest` (3.2B, Q4_K_M). Au fost disponibile numai tool-urile `search_books`, `get_book_details` și `recommend_books`. Tool-ul de prețuri pentru misiunea 3 a rămas comentat.

Clientul este cel furnizat pe Moodle, adaptat la portul aplicației. `UseFunctionInvocation()` execută apelurile cerute de model; istoricul conversației este păstrat între întrebări. Urma `[tool]` este afișată după încheierea răspunsului, din mesajele întoarse de clientul AI.

Cele șase întrebări au fost trimise o singură dată fiecare, în ordinea fișei și în aceeași conversație. Transcrierea integrală se află în `Fisa7-Misiunea2-transcript.txt`. Apelurile directe folosite separat pentru verificare nu reprezintă repetări ale întrebărilor către model.

## Tabel pentru fișă

| # | Întrebarea | Tool-uri apelate, copiate din consolă | Răspunsul este corect? |
| --- | --- | --- | --- |
| 1 | Ce cărți de Rebreanu are biblioteca? | `[tool] search_books(query=Rebreanu)` | Parțial. Identifică Ion și a doua carte, cu anii corecți, dar scrie „Pădurea spânzurătorilor” în loc de „Pădurea spânzuraților”. |
| 2 | Spune-mi mai multe despre romanul Ion: anul, genul și prețul. | `[tool] search_books(query=Ion)` | Nu. Susține că nu a găsit romanul lui Rebreanu și îl atribuie lui Ion Creangă. Nu apelează get_book_details și nu oferă anul, genul și prețul cerute. |
| 3 | Ce cărți îi recomanzi cititorului 5? | `[tool] search_books(query=Rebreanu)`; `[tool] recommend_books(readerId=5, top=5)` | Parțial. Apelează recomandările pentru cititorul corect, dar deformează Moromeții în „Moromii” și Numele vântului în „Numele vânătului”; inventează descrieri și prezintă scorurile ML ca note. Căutarea Rebreanu este inutilă aici. |
| 4 | Ce cărți îi recomanzi unui cititor nou, cu ID-ul 999? | `[tool] recommend_books(readerId=999, top=5)` | Parțial. Folosește cititorul corect și rezultatele fallback, dar deformează Câinele din Baskerville în „Câiniii din Baskerville” și adaugă descrieri care nu sunt furnizate de tool. Nu explică mecanismul fallback. |
| 5 | Care este cea mai scumpă carte din catalog? | `[tool] search_books(query=preȥ)` | Nu identifică răspunsul. Spune că nu a găsit informații despre prețuri. Nu inventează o carte, dar caută un text nepotrivit; search_books nu ordonează întregul catalog după preț. Răspunsul real este Domain-Driven Design, 220 lei. |
| 6 | Care este capitala Franței? | `[tool] search_books(query=Fran\ae)` | Nu. Caută în catalog și declară că nu a găsit informația. Capitala este Paris; tool-urile bibliotecii nu sunt potrivite pentru această întrebare de cultură generală. |

Rezultatul arată că accesul MCP la date reale nu garantează fidelitatea textului generat. Modelul poate alege un tool nepotrivit, poate omite un apel necesar sau poate inventa explicații după ce primește rezultatul corect. Lipsa unui tool care ordonează catalogul după preț este subiectul misiunii 3; acel tool nu a fost activat în acest experiment.

## Date de referință verificate

Catalogul real a fost citit prin `/api/books`, iar detaliile și recomandările prin apeluri MCP directe.

- `/Books/Details/4`: **Ion**, Liviu Rebreanu, **1920**, **Roman românesc**, **20 lei**, 6 comenzi.
- `/Books/Details/7`: **Pădurea spânzuraților**, Liviu Rebreanu, **1922**, **39 lei**.
- Cea mai scumpă carte din întregul catalog: `/Books/Details/32`, **Domain-Driven Design**, Eric Evans, **220 lei**. Urmează Designing Data-Intensive Applications, 210 lei, și C# in Depth, 195 lei.

Recomandările reale pentru cititorul **5**, cu `fallback: false`:

| ID | Titlu | Scor ML |
| --- | --- | --- |
| 30 | Designing Data-Intensive Applications | 4.0801725 |
| 27 | Fata cu un tatuaj de dragon | 3.8340535 |
| 5 | Moromeții | 2.942654 |
| 21 | Numele vântului | 2.935462 |
| 13 | Plumb | 2.6382828 |

Recomandările reale pentru cititorul nou **999**, cu `fallback: true`:

| ID | Titlu | Media evaluărilor |
| --- | --- | --- |
| 25 | Câinele din Baskerville | 4.1396646 |
| 32 | Domain-Driven Design | 3.8117647 |
| 27 | Fata cu un tatuaj de dragon | 3.8060606 |
| 16 | Dune | 3.74375 |
| 35 | Istoria critică a românilor | 3.724359 |

Scorurile ML pentru cititorul 5 sunt predicții, nu note acordate de cititor. Pentru cititorul 999 se folosesc mediile cărților cu minimum 12 evaluări. Tool-ul nu furnizează descrieri literare, astfel că explicațiile inventate de model nu sunt susținute de rezultatul lui.

## Rulare

Pornește aplicația web și apoi clientul:

```powershell
dotnet run --project Lab1_MVC --launch-profile https
```

În alt terminal:

```powershell
dotnet run --project Biblioteca.Agent
```

Alege **2**, apoi introdu întrebările pe rând. Enter fără text încheie conversația. Ollama trebuie să ruleze la `http://localhost:11434`, cu `llama3.2` instalat.

Verificarea din mediul izolat a folosit profilul HTTP al serverului și `$env:MCP_ENDPOINT = 'http://localhost:5114/mcp'` pentru client, deoarece HTTPS nu a putut fi pornit în acest cont.
