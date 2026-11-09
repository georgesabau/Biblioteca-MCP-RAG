# Biblioteca MVC · Laborator Î4

Szasz Daria · 121200073 · varianta **A3 / B3**.

Catalog cu 8 cărți/pagină, sortare implicită titlu Z–A și filtru după preț minim–maxim. Gestiune autori, cititori, genuri și comenzi. Detaliile autorului includ numărul genurilor distincte și totalul comenzilor.

## Pornire

Este necesar SDK .NET 10. Din rădăcina soluției:

```powershell
dotnet restore Lab1_MVC.sln
dotnet build Lab1_MVC.sln
dotnet run --project Lab1_MVC --no-launch-profile --urls http://localhost:5184
```

Deschide `http://localhost:5184/Books`. Celelalte pagini sunt accesibile din meniu. Baza SQLite este creată/migrată automat, iar LibrarySeed o inițializează cu 40 de cărți dacă este goală. Funcțiile acestui laborator nu necesită Ollama; acesta este folosit numai de endpoint-urile AI existente.

## Documentație

- [Jurnal: variantă, SQL, explicații și rezultate](JURNAL.md)
- [Catalog: filtru și pagina a doua](docs/verificari/catalog-filter-page2.png)
- [Detalii autor](docs/verificari/author-details.png)
- [Cele două comenzi create prin interfață](docs/verificari/orders.png)
- [Raportul verificărilor automate](docs/verificari/ui-test-results.json)

Baza SQLite era deja urmărită de Git în proiectul inițial. Cele două comenzi create prin interfață sunt incluse în baza versionată, care conține acum 302 comenzi; capturile documentează verificarea lor.

## Laborator Î5 · A3 / B3

Branch: `laborator-5`, fără merge în main. Din meniul **Administrare**:

1. Importă `Lab1_MVC/Data/book-ratings.csv` (6.599 evaluări, 300 cititori). Reimportul ignoră duplicatele.
2. Apasă **Reantrenează modelul de recomandare**; noul model este preluat fără repornire.
3. Deschide jurnalul recomandărilor și dashboard-ul (media pe editură, B3).

API: `/api/v2/readers/5/recommendations?top=3`. Cititorii necunoscuți folosesc fallback cu minimum 12 evaluări/carte (A3). Fiecare apel este jurnalizat.

În `/Books/Create`, completează descrierea și apasă **Sugerează genul**. Modelele sunt incluse în repository; funcțiile Î5 nu necesită Ollama.

Reantrenarea clasificatorului din rădăcină:

```powershell
dotnet run --project Biblioteca.Genre.Training -- Biblioteca.Genre.Training/book-descriptions.csv Lab1_MVC/MLModels/GenreClassifier.zip
```

Rezultate și explicații: [JURNAL.md](JURNAL.md), [teste HTTP/SQL](docs/verificari/lab5-results.json), [clasificare](docs/verificari/lab5-genre-training.txt), [dashboard](docs/verificari/lab5-dashboard.png).

Pentru predare folosește linkul ultimului commit de pe `laborator-5` din repository-ul personal. Încarcă acel link pe Moodle. Baza SQLite locală, modificată anterior lucrului la Î5, rămâne necomisă; pe o clonă importă evaluările din pagina Administrare.
