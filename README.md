# recipe-tracker
Application to capture recipes from websites, books and videos, and cook from them on a phone or tablet

## Live app

https://rmcbain21.github.io/recipe-tracker/

Deployed to GitHub Pages by `.github/workflows/deploy.yml` on every push to `main`.

## Running locally

```
dotnet run --project src/RecipeTracker.Web
dotnet test RecipeTracker.slnx
```

Offline caching only happens in a published build (`service-worker.published.js`); `dotnet run` uses a service worker that caches nothing.

## Hosting notes

- The app is served from the `/recipe-tracker/` subpath. The workflow rewrites `<base href="/" />` in `wwwroot/index.html` **before** `dotnet publish`, because the service worker's asset manifest holds a hash of `index.html`; editing it after publishing breaks offline install. The service worker takes its base URL from its own registration scope.
- GitHub Pages serves `404.html` for unknown paths, so the workflow copies `index.html` to `404.html` and deep links and reloads of sub-pages still start the app.
- One-time repository setup: the repository is public, and **Settings → Pages → Build and deployment → Source** is set to **GitHub Actions**.
