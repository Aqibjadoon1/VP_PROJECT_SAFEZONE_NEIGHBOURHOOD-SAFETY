# SafeZone Deployment Guide

## Quick Start

```bash
dotnet run --project SafeZone.Server/SafeZone.Server.csproj
```

App runs on `http://localhost:5002` by default.

---

## Required Environment Variables

Set these via your hosting platform (Render, Azure, AWS, etc.) or in `appsettings.Production.json`.

### 1. JWT Signing Key (CRITICAL — API will fail without this)

```bash
Jwt__Key="YourVeryLongRandomSecretKey32CharsMin!"
```

Generate a strong key:
```bash
openssl rand -base64 48
```

### 2. Gmail API Credentials (for email notifications)

**Why Gmail API?** SafeZone uses Gmail API (not SMTP) as requested by the project owner.

**Setup steps:**
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a project → Enable **Gmail API**
3. Go to **APIs & Services → Credentials → Create OAuth 2.0 Client ID**
4. Choose **Desktop app** type
5. Download the client credentials JSON
6. Use [OAuth 2.0 Playground](https://developers.google.com/oauthplayground) to get a Refresh Token:
   - Settings → Use your own OAuth credentials → paste Client ID & Secret
   - Select scope: `https://www.googleapis.com/auth/gmail.send`
   - Click **Authorize APIs** → **Exchange authorization code for tokens**
   - Copy the **Refresh Token**

```bash
Gmail__ClientId="your-client-id.apps.googleusercontent.com"
Gmail__ClientSecret="your-client-secret"
Gmail__RefreshToken="your-refresh-token"
Gmail__FromEmail="notifications@yourdomain.com"
```

### 3. Slack Webhook (for incident alerts)

1. Go to [Slack API Webhooks](https://api.slack.com/messaging/webhooks)
2. Create an app → Incoming Webhooks → Add to Slack
3. Choose the channel (e.g., #incidents)
4. Copy the Webhook URL

```bash
Slack__WebhookUrl="SLACK_INCOMING_WEBHOOK_URL"
```

### 4. Database

SQLite is used by default for local development. The database file `SafeZone.db` is created automatically on first run.

For Render or any free hosting plan with an ephemeral filesystem, use PostgreSQL. SafeZone automatically switches to PostgreSQL when either `DATABASE_URL` or `ConnectionStrings__PostgresConnection` is set:

```bash
DATABASE_URL="POSTGRES_CONNECTION_STRING"
```

### 5. Google OAuth (optional — for resident Google login)

```bash
Authentication__Google__ClientId="your-google-client-id"
Authentication__Google__ClientSecret="your-google-client-secret"
```

---

## Docker Build

```bash
docker build -t safezone .
docker run -p 8080:8080 \
  -e Jwt__Key="your-key" \
  -e DATABASE_URL="POSTGRES_CONNECTION_STRING" \
  -e Gmail__ClientId="..." \
  -e Gmail__ClientSecret="..." \
  -e Gmail__RefreshToken="..." \
  -e Gmail__FromEmail="..." \
  -e Slack__WebhookUrl="..." \
  safezone
```

---

## Render Deploy

This repo includes `render.yaml` for a Docker web service. Render will prompt for unsynced secrets when you create the Blueprint.

1. Create a free PostgreSQL database on Neon and copy the pooled connection string.
2. Push this repository to GitHub.
3. In Render, create a new Blueprint from the repo.
4. Set `DATABASE_URL` to the Neon connection string.
5. Set the Gmail and Slack variables listed above.
6. Deploy.

After deploy, your ElevenLabs webhook URL will be:

```text
https://YOUR-SERVICE-NAME.onrender.com/api/ElevenLabsWebhook
```

---

## Fly.io Deploy

```bash
fly deploy
```

Set secrets:
```bash
fly secrets set Jwt__Key="your-key"
fly secrets set Gmail__ClientId="..."
fly secrets set Gmail__ClientSecret="..."
fly secrets set Gmail__RefreshToken="..."
fly secrets set Gmail__FromEmail="..."
fly secrets set Slack__WebhookUrl="..."
```

---

## Health Checks

- `GET /health` — basic app health
- Check server logs for `[DEPLOY]` startup warnings if notifications are not configured

---

## Notification Behavior

| Severity | Email to Reporter | Slack Alert |
|----------|-------------------|-------------|
| Low      | Yes               | No          |
| Medium   | Yes               | No          |
| High     | Yes               | Yes         |
| Critical | Yes               | Yes         |

**Note:** Emails are sent via Gmail API. If Gmail is not configured, the app logs the email content so you can verify what would be sent. Slack requires the webhook URL.

---

## Troubleshooting

### "[Gmail API] Not configured"
Set the `Gmail:*` environment variables. See section 2 above.

### "[Slack] Webhook URL not configured"
Set `Slack__WebhookUrl`. See section 3 above.

### Maps not loading
Hard-refresh the browser (Ctrl+Shift+R) to clear the JavaScript cache.

### Database locked
SQLite does not support multiple concurrent writers and is not suitable for free Render web services. Set a PostgreSQL connection string:

```bash
DATABASE_URL="POSTGRES_CONNECTION_STRING"
```
