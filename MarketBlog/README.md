# Ink & Idea

A single-writer blogging app built with C# and ASP.NET Core 8.

## Open and run

1. Open `MarketBlog.sln` in Visual Studio.
2. Set **MarketBlog** as the startup project.
3. Press **F5**. The app opens at `http://localhost:52775` over HTTP, so it does not need the ASP.NET Core HTTPS certificate.

Or run `dotnet run --project MarketBlog/MarketBlog.csproj` from this folder.

Posts are saved in `MarketBlog/App_Data/posts.json`.

## Write and publish

- Create, edit, save as draft, publish, and delete stories.
- Share published stories using their public URLs and copy-link buttons.
- Required fields: title (120 characters), story (up to 50,000 characters), and author name (60 characters).
- Optional fields: topic (40 characters) and short intro (220 characters). A preview is generated from the story when the intro is blank.
- Server-side validation displays field-specific errors and keeps submitted values in the form.

## Build and test

From this folder:

```powershell
dotnet restore MarketBlog.sln -m:1
dotnet build MarketBlog.sln --configuration Release --no-restore -m:1
dotnet test MarketBlog.Tests/MarketBlog.Tests.csproj --configuration Release --no-build --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

The NUnit cases use Arrange, Act, Assert sections. Coverage settings are scoped to `MarketBlog.Core`, which contains the app's validation logic.

## CI/CD

GitHub Actions is configured in `.github/workflows/ci.yml`. It builds, runs the tests, collects coverage, and uploads a deployable app artifact on successful runs. See [docs/CI-CD.md](docs/CI-CD.md) for GitHub setup and deployment notes.

This starter is designed for a single writer on a trusted local deployment. Before hosting it publicly, add authentication and authorization for writing and deleting posts, and use durable database storage such as SQLite or SQL Server.

