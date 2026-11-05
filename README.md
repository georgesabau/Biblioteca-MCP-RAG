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

## Predare

Creează un repository privat personal `Szasz_Daria_Biblioteca`, adaugă colaboratorul `fc0308`, încarcă istoricul local în acel repository și trimite doar linkul pe Moodle. Remote-ul actual indică proiectul inițial `georgesabau/Biblioteca-MVC`.
