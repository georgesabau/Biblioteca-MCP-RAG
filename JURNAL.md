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