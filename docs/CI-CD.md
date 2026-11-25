# CI/CD setup

## What the pipeline does

GitHub Actions runs `.github/workflows/ci.yml` on every push, pull request to `main` or `master`, and manual dispatch. It restores and builds the .NET 8 solution, runs the NUnit tests, collects Cobertura and OpenCover reports using `coverage.runsettings`, then publishes the web app. Each run uploads a test/coverage artifact; successful runs also upload a `MarketBlog-release` artifact that can be deployed.

## Enable it on GitHub

1. Create a GitHub repository.
2. Push the contents of this folder (the folder containing `MarketBlog.sln`) to the repository root. Include `.github/workflows/ci.yml`, `global.json`, and `coverage.runsettings`.
3. In the repository, open **Actions** and enable workflows if GitHub asks.
4. Push a commit or open a pull request. Check the **Actions** tab for the run. Download `test-and-coverage-reports` to inspect `.trx` and `coverage.cobertura.xml`; download `MarketBlog-release` for the published app files.
5. To require CI before merging, add a branch rule for `main` and require the **Build, test, coverage, and publish** status check.

No secrets are required for this CI pipeline. The workflow grants only read access to repository contents.

## Run locally

From this folder, run:

```powershell
dotnet restore MarketBlog.sln -m:1
dotnet build MarketBlog.sln --configuration Release --no-restore -m:1
dotnet test MarketBlog.Tests/MarketBlog.Tests.csproj --configuration Release --no-build --settings coverage.runsettings --collect:"XPlat Code Coverage"
dotnet publish MarketBlog/MarketBlog.csproj --configuration Release --no-restore --output artifacts/MarketBlog
```

Coverage is scoped to `MarketBlog.Core`, where the blog validation logic lives. The test assembly is excluded so the report measures application logic rather than test code.

## Continuous deployment

The workflow currently builds and stores a deployable artifact; it does not upload the app to a hosting provider. To deploy automatically, choose a host (for example, Azure App Service), then add a protected deployment job with that provider's publish profile or identity-based credentials stored as GitHub Actions secrets. The app currently persists posts to a local JSON file; use durable shared storage before relying on ephemeral cloud hosts.

## Framework support

The pipeline follows the existing `net8.0` projects. Microsoft ended .NET 8 support on November 10, 2026; before a production deployment, upgrade the app, Core, and tests to a supported LTS release and update `global.json` and the workflow SDK selection. See the [official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
