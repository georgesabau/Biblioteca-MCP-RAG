# Laboratorul Î4 · Szasz Daria · nr. matricol 121200073 · varianta A3 · B3

| Parametru | Valoarea mea |
|---|---|
| A: ultima cifră 3 | A3 |
| B: penultima cifră 7 | B3 |
| V1 | 8 cărți pe pagină |
| V2 | titlu Z → A |
| V3 | preț minim–maxim |
| V4 | numărul de genuri distincte ale autorului |

## A1 · Pornire
Baza locală conține 40 de cărți. Compilarea inițială a reușit cu avertismente existente de pachete și nullable.

## A2–A5 · Implementare

Controller-ele și cele cinci view-uri pentru Author, Customer, Genre și Order au fost generate cu `dotnet-aspnet-codegenerator` 10.0.2 și adaptate cerințelor. Autorii au coloana FullName. Selectoarele de cărți afișează numele complet al autorului și genurile ordonate. Catalogul proiectează numai coloanele necesare în BookListItem. Sortarea, căutarea, filtrarea și Skip/Take sunt executate înainte de ToListAsync, în SQL. ID este criteriu secundar de sortare pentru o paginare stabilă.

Detaliile cărții încarcă autorul, genul și comenzile cu cititorii prin Include/ThenInclude și AsNoTracking. Paginile invalide sunt limitate la intervalul existent. Lista goală afișează 0 rezultate și pagina 1 din 1, cu navigarea dezactivată.

## S1 · Comenzile

- Protecția la overposting: **ViewModel**, `OrderEditViewModel`.
- La Create se construiește un Order nou numai din BookID, CustomerID și OrderDate; un OrderID postat este ignorat. La Edit se încarcă entitatea existentă și se copiază numai câmpurile permise, după verificarea ID-ului din rută.
- ViewModel-ul nu conține proprietățile de navigare Book/Customer. Astfel, datele lor nu pot fi suprascrise prin formular și nu sunt validate ca obiecte obligatorii la crearea unei comenzi.
- Se verifică în baza de date existența cărții și a cititorului; formularele au token antiforgery. Selectoarele sunt reconstruite și la validare eșuată.
- Index este ordonat după OrderDate descrescător, apoi OrderID descrescător. Filtrul de cititor se aplică în SQL. Selectoarele și lista folosesc numele cititorilor și titlurile cărților.
- Data implicită este DateTime.Today.

La 05.11.2026, Codex a completat și trimis în Chrome formularele `/Orders/Create` pentru Cititor 1:

| Comandă locală | Carte | Data |
|---|---|---|
| 301 | 1984 (ID 38) | 2026-11-05 |
| 302 | Mândrie și prejudecată (ID 39) | 2026-11-05 |

Comenzile sunt păstrate în baza SQLite locală, deja urmărită de Git în proiectul inițial și inclusă în commit-ul final. O clonă a acestui istoric primește 302 comenzi; o bază nouă, fără date, primește cele 300 de comenzi din LibrarySeed. Dovada formularelor finalizate este în `docs/verificari/orders.png`.

## S2 · Pagina unui autor și sortarea după autor

Indicatorul V4: **numărul de genuri distincte ale cărților autorului**, fără valoarea null. Autorul fără cărți are 0 genuri și 0 comenzi.

Interogarea efectivă observată în logul EF Core (pentru exemplu, @id = 8):

```sql
SELECT COUNT(*)
FROM (
    SELECT DISTINCT "b"."GenreID"
    FROM "Book" AS "b"
    WHERE "b"."AuthorID" = @id AND "b"."GenreID" IS NOT NULL
) AS "b0"
```

Numărul total de comenzi este calculat separat tot în SQL:

```sql
SELECT COUNT(*)
FROM "Order" AS "o"
INNER JOIN "Book" AS "b" ON "o"."BookID" = "b"."ID"
WHERE "b"."AuthorID" = @id
```

Verificare: Ioan Slavici, ID 8, are 2 cărți, 2 genuri distincte și 17 comenzi. Valorile din pagină au fost comparate cu interogări SQLite independente.

`OrderBy(b => b.Author.FullName)` nu poate fi tradus în SQL: FullName este un getter C# calculat, fără coloană mapată. EF Core nu poate transforma automat corpul getter-ului într-o expresie SQL de sortare. Soluția folosește expresia explicită `FirstName + " " + LastName`, cu tratarea autorului null, înainte de paginare:

```csharp
"author" => books.OrderBy(b => b.Author != null
    ? b.Author.FirstName + " " + b.Author.LastName : "").ThenBy(b => b.ID),
"author_desc" => books.OrderByDescending(b => b.Author != null
    ? b.Author.FirstName + " " + b.Author.LastName : "").ThenBy(b => b.ID),
```

Pentru SQLite, `OrderBy(b => b.Price)` pe decimal nu este suportat direct. Sortarea folosește `(double)b.Price`, tradus în `CAST("b"."Price" AS REAL)`. Compararea limitelor de preț rămâne decimal și este tradusă prin funcția SQLite `ef_compare` înregistrată de EF Core. Nu există sortare în memorie.

## S3 · Filtrul variantei mele

Filtru implementat: **preț minim–maxim**, cu limite inclusive și fiecare limită opțională. Se combină cu căutarea după titlu și sortarea. Formularul trimite o căutare nouă și resetează pagina; link-urile de sortare și paginare păstrează currentFilter, sortOrder, minPrice și maxPrice. Limitele negative, inversate sau invalide afișează erori și o listă goală.

Interogarea efectivă pentru pagina a doua a scenariului testat, copiată din logul EF Core (parametri: minPrice_Value = 20, maxPrice_Value = 100, searchString = 'a', p4 = 8, p = 8):

```sql
SELECT "b0"."ID", "b0"."Title", CASE
    WHEN "a"."ID" IS NOT NULL THEN COALESCE("a"."FirstName", '') || ' ' || COALESCE("a"."LastName", '')
    ELSE ''
END, CASE
    WHEN "g"."ID" IS NOT NULL THEN "g"."Name"
    ELSE ''
END, "b0"."Price", (
    SELECT COUNT(*)
    FROM "Order" AS "o"
    WHERE "b0"."ID" = "o"."BookID")
FROM (
    SELECT "b"."ID", "b"."AuthorID", "b"."GenreID", "b"."Price", "b"."Title", CAST("b"."Price" AS REAL) AS "c"
    FROM "Book" AS "b"
    WHERE ef_compare("b"."Price", @minPrice_Value) >= 0
        AND ef_compare("b"."Price", @maxPrice_Value) <= 0
        AND instr("b"."Title", @searchString) > 0
    ORDER BY CAST("b"."Price" AS REAL), "b"."ID"
    LIMIT @p4 OFFSET @p
) AS "b0"
LEFT JOIN "Author" AS "a" ON "b0"."AuthorID" = "a"."ID"
LEFT JOIN "Genre" AS "g" ON "b0"."GenreID" = "g"."ID"
ORDER BY "b0"."c", "b0"."ID"
```

Scenariu executat automat în Chrome pe baza locală cu 40 de cărți:

1. Căutare titlu `a`, preț minim `20`, maxim `100`, sortare preț crescător.
2. Rezultat: **25 de cărți, 4 pagini**, cu 8 cărți pe primele 3 pagini și 1 pe ultima.
3. Pe pagina 1, rândurile au fost comparate cu primele 8 rezultate ale unei interogări SQL independente.
4. Click pe „Înainte”: pagina 2 afișează următoarele 8 rezultate, identice cu SQL. URL-ul păstrează `sortOrder=price`, `currentFilter=a`, `minPrice=20`, `maxPrice=100`, `pageNumber=2`.
5. Click pe „Autor” păstrează căutarea și intervalul. O căutare nouă după „Dune” revine la pagina 1.

Captură: `docs/verificari/catalog-filter-page2.png`. Căutarea folosește Contains, tradus în instr de SQLite, deci distinge majusculele de minuscule.

## S4 · Folosirea AI

**Instrument:** ChatGPT. Folosit pentru înțelegerea cerințelor, ajutor la scrierea și verificarea unor fragmente de cod și identificarea unor erori.

**Exemplu de cerere:**

> Verifică dacă filtrarea și paginarea funcționează corect pentru varianta A3–B3.

**Exemplu de corecție:** Eroare întâlnită și corectarea ei: La modificarea unor fișiere, diacriticele nu se mai afișau corect în aplicație. Problema era cauzată de citirea fișierelor fără specificarea codificării UTF-8.

Cod inițial:

```
s = p.read_text()
p.write_text(s, encoding='utf-8')
```

Cod corectat:

```
s = p.read_text(encoding='utf-8-sig')
p.write_text(s, encoding='utf-8')
```

După această modificare, textele din interfață au fost afișate corect. Am înțeles că este important să verific și funcționarea aplicației, nu doar compilarea codului.

## Verificări efectuate

- `dotnet build Lab1_MVC.sln --no-restore`: compilare reușită, fără erori.
- Am verificat sortarea cărților după titlu, preț și autor, atât crescător, cât și descrescător.
- Am verificat paginarea cu 8 cărți pe pagină, păstrarea filtrelor la navigare și comportamentul aplicației atunci când nu există rezultate sau este introdus un număr de pagină invalid.
- Am verificat operațiile CRUD pentru autori, cititori, genuri și comenzi, prin crearea, modificarea și ștergerea unor înregistrări de test.
- Am testat validarea comenzilor prin introducerea unor date invalide, inclusiv selectarea unei cărți inexistente. Aplicația nu permite salvarea acestora.
- Am verificat protecția la overposting prin trimiterea unor câmpuri suplimentare în formular. Acestea nu au modificat proprietățile nepermise ale comenzii.
- Am verificat protecția antiforgery, iar o cerere trimisă fără tokenul necesar a fost respinsă cu HTTP 400.
- Am verificat pagina de detalii a unui autor, inclusiv numărul de comenzi pentru fiecare carte, totalul comenzilor și numărul de genuri distincte.
- Am verificat afișarea comenzilor asociate unei cărți și a numelor cititorilor.
- Am testat accesarea unor înregistrări inexistente, iar aplicația a returnat HTTP 404.
- Am creat o comandă nouă din interfață și am verificat că aceasta este salvată și afișată corect în lista comenzilor.

# Laboratorul Î5 · Szasz Daria · 121200073 · A3 / B3

## Varianta și predarea

Branch: `laborator-5`, creat din `main` la commit-ul final Î4 `bbfea42`. Main nu a fost modificat. Commit-uri distincte pentru A1, A2, A3, A4, S1 și S2, urmate de verificări și documentare. A3: **minimum 12 evaluări/carte pentru fallback**. B3: **media evaluărilor pe editură**.

## A1 · Evaluările în baza de date

Entitatea Rating are ReaderId (identificator din CSV, independent de Customer), BookID cu cheie externă, Stars și CreatedAt UTC. Migrarea `Ratings` este aplicată automat la pornire. Din `Lab1_MVC/Data/book-ratings.csv` s-au importat **6.599 de rânduri**, reprezentând **300 de cititori distincți**. Reimportarea aceluiași CSV a importat 0 rânduri și a ignorat 6.599 de perechi cititor/carte deja existente.

Formularul `/Admin` validează antetul, identificatorii pozitivi, existența cărții și nota 1–5. Un CSV cu 4 rânduri invalide a fost respins fără inserări. Importul folosește un parser CSV cu suport pentru ghilimele și antiforgery. Evaluările existente pentru aceeași pereche cititor/carte nu sunt suprascrise.

## A2 · Reantrenarea din aplicație

`RecommenderTrainer` încarcă evaluările din SQLite, folosește seed 1, împărțire 80/20, 20 de iterații și rang 16. Rezultatul măsurat: **RMSE = 0,6983**, pe 6.599 de evaluări. RMSE măsoară eroarea de predicție în unități de stele; mai mic înseamnă mai bine, pe aceeași împărțire de test.

Modelul este scris într-un fișier temporar și înlocuit după salvarea completă. Un semafor serializează reantrenările. Pool-ul urmărește schimbările fișierului. Am deschis `/Recommendations?userId=5` și am apelat API-ul după reantrenare, **fără repornirea aplicației**. Scorurile NaN pentru chei necunoscute nu sunt incluse în calculul RMSE. Cu mai puțin de 10 evaluări, formularul afișează un mesaj în loc să încerce antrenarea.

## A3 · API și cold start

Endpoint: `/api/v2/readers/{id}/recommendations?top=5`. Parametrul top este limitat la 1–20. Cărțile deja evaluate sunt excluse, inclusiv din fallback. Ordinea este descrescătoare după scor și apoi după ID, pentru rezultate stabile.

**Diferența dintre cititorii 5 și 999:** cititorul 5 are istoric și primește scoruri personalizate ML.NET, `fallback=false`: Designing Data-Intensive Applications (4,0802), Fata cu un tatuaj de dragon (3,8341), Moromeții (2,9427), Numele vântului (2,9355), Plumb (2,6383). Cu top=3, răspunsul conține exact primele 3.

Cititorul 999 nu există în evaluări: primește media stelelor, `fallback=true`, pentru cărțile cu minimum 12 evaluări: Câinele din Baskerville (4,1397), Domain-Driven Design (3,8118), Fata cu un tatuaj de dragon (3,8061), Dune (3,7438), Istoria critică a românilor (3,7244). Fallback-ul a fost comparat cu o interogare SQL independentă. Nu este necesară o înregistrare Customer pentru ReaderId 999.

## A4 · Clasificarea genului

Proiectul consolă `Biblioteca.Genre.Training` include CSV-ul furnizat, copiat la compilare, și antrenează clasificatorul FeaturizeText + SdcaMaximumEntropy cu seed 1 și split 80/20. Modelul `GenreClassifier.zip` este inclus în MLModels și copiat în output. Rezultatele efective sunt păstrate în `docs/verificari/lab5-genre-training.txt`.

- **MicroAccuracy: 74,7170%**; **MacroAccuracy: 74,4706%**.
- MicroAccuracy este proporția tuturor exemplelor corect clasificate; MacroAccuracy este media acurateților pe clase, fiecare clasă având aceeași pondere. Numărul diferit de exemple și performanțele diferite între clase explică diferența. Poezie și Nuvelă au recall mai mic (64%, respectiv 62,5%). Distribuția claselor din setul de test este relevantă, nu doar cea din întregul CSV.
- Cele mai frecvente confuzii direcționale sunt **Poezie → Nuvelă: 6** și **Clasic universal → Roman românesc: 6**. Perechea Poezie/Nuvelă are 11 confuzii în ambele sensuri. Este plauzibil: descrierile literare scurte au teme comune și nu identifică întotdeauna forma literară sau proveniența. Există și SF/Programare, favorizată de vocabularul tehnologic comun și de exemplele intenționat ambigue din CSV.
- Descrierea exactă a cărții **Dune din baza proiectului**: sugestie **SF**, **72,17%** încredere. Genul este corect.
- Descrierea **„un detectiv pe o stație spațială”**: sugestie **SF**, **77,83%** încredere. Cuvintele indică atât Polițist, cât și SF; scorul exprimă rezultatul modelului, nu o garanție de corectitudine sau o probabilitate calibrată.

Formularul Books/Create include Description și Year; acestea se și salvează. Butonul Sugerează genul trimite JSON cu token antiforgery, tratează descrierea goală și erorile de rețea, afișând genul și încrederea. Alegerea finală a genului rămâne bibliotecarului. Am verificat și persistența descrierii/anului prin crearea și ștergerea unei cărți temporare.

## S1 · Jurnalul recomandărilor

Fiecare apel reușit al API-ului salvează ReaderId, CreatedAt UTC și un tablou JSON cu ID-urile întoarse, în ordinea răspunsului, inclusiv lista goală. Migrarea `RecommendationLogs` creează tabelul. `/Admin/Log` refolosește PaginatedList cu 8 rânduri/pagină, filtru după cititor și date, sortare după dată ascendent/descendent cu ID secundar. Intervalul include întreaga zi finală, în UTC. Filtrele se păstrează la sortare și paginare; paginile în afara intervalului sunt limitate. Intervalul inversat afișează eroare și rezultate goale. Am verificat pagina a doua cu filtru și sortare, lista goală și o pagină prea mare.

## S2 · Dashboard B3

`/Admin/Dashboard` afișează 6.599 de evaluări, 300 de cititori distincți și media generală **3,246249** (afișată 3,25). Distribuția: 1 stea = 365, 2 = 1.179, 3 = 2.361, 4 = 1.854, 5 = 840. Graficul B3 este media pe editură: Art ≈ 3,16, Humanitas ≈ 3,56, Nemira ≈ 3,09, Polirom ≈ 3,14. Agregările sunt executate în SQL; media pe editură este media tuturor evaluărilor cărților asociate, nu media neponderată a mediilor cărților.

Chart.js este inclus local cu licența, deci graficele nu depind de CDN la utilizare. Sunt disponibile și tabelele cu valorile. O carte cu mai multe edituri contribuie la fiecare editură; una fără editură contribuie doar la statisticile generale. Editurile fără evaluări au medie null. Captura `docs/verificari/lab5-dashboard.png` a fost inspectată: ambele grafice sunt vizibile și lizibile.

## Verificări și abateri

- `dotnet build Lab1_MVC.sln --no-restore`: reușit, 0 erori; rămân avertismentele de pachete existente și avertismentul verificării online NuGet în mediul restricționat.
- Teste HTTP reale pentru import, reimport, reantrenare, recomandările 5/999, top=3, top=0 și top=99, sugestii SF, formulare și jurnal. Verificări SQL independente pentru fallback și dashboard. Cererile POST fără antiforgery au returnat HTTP 400.
- Rezultatele măsurate: `docs/verificari/lab5-results.json`. Modelul reantrenat este versionat; pentru o bază nouă, importul se face din Administrare înainte de reantrenare.
- Baza SQLite avea modificări locale înainte de Î5. Acestea au fost păstrate; baza modificată nu este inclusă în commit-urile Î5, pentru a nu amesteca modificările existente. Importul și verificările au fost executate în baza locală. Fișierul local preexistent `tatus` este păstrat și necomis.
- Nu există abateri funcționale de la A1–A4/S1–S2. Protecția antiforgery și validarea importului sunt completări față de exemplele minimale din enunț.
- AI: ChatGPT/Codex a asistat implementarea și testarea. Exemplu de corecție: cheia ReaderId necunoscută este verificată explicit înainte de predicție; fallback-ul exclude și el cărțile evaluate, evitând recomandarea repetată a acestora.
- Încărcarea linkului ultimului commit pe Moodle rămâne un pas de predare în contul studentului.
