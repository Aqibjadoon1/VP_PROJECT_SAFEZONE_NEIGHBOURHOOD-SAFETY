# Free Deployment Options

Blazor Server needs a persistent .NET process — static hosts (Vercel/Netlify/GitHub Pages) won't work.

## Option 1: fly.io (free — best)

```bash
# Install once
winget install flyctl

# Sign up (needs credit card for verification only — no charges on free tier)
fly auth signup

# Deploy
fly launch
fly deploy
```

Free tier: 3 shared VMs, 256MB RAM each, 3GB storage.

## Option 2: Render.com (free — Docker)

```bash
# Push to GitHub, then:
# 1. Go to render.com → New Web Service
# 2. Connect your GitHub repo
# 3. Select "Docker" runtime
# 4. Set port to 8080
# 5. Deploy
```

Free tier: 750 hours/month, cold starts after 15min idle.

## Option 3: Azure for Students (free — $100 credit)

```bash
# If you have a .edu email:
# 1. Go to azure.microsoft.com/free/students
# 2. Create App Service (F1 free tier or use credits)
# 3. Deploy from VS: Right-click project → Publish → Azure

# Or via CLI:
az webapp up --name safe-zone --runtime DOTNET:8.0 --os linux
```

## Option 4: Local PC + ngrok (free — for demos)

```bash
dotnet run --launch-profile https
ngrok http https://localhost:7026
# Share the ngrok URL — works for demos, not 24/7
```
