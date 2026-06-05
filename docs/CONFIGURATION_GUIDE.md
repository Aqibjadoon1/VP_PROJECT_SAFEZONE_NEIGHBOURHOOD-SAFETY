# SafeZone Configuration Guide

## Quick Start

```bash
cd SafeZone.Server
dotnet run --launch-profile https
```
Opens at `https://localhost:7026`. Swagger at `/swagger`.

---

## 1. appsettings.json — Core Configuration

**File:** `SafeZone.Server/appsettings.json`

```json
{
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=SafeZone.db"
  },
  "Jwt": {
    "Key": "YOUR_32_CHAR_MINIMUM_SECRET_KEY_HERE",
    "Issuer": "SafeZone",
    "Audience": "SafeZoneClient",
    "ExpiryMinutes": 15
  }
}
```

| Setting | What It Does | Required? |
|---------|-------------|-----------|
| `SqliteConnection` | SQLite database file path | Yes — auto-created on first run |
| `Jwt:Key` | JWT signing key (min 32 chars) | Yes — used for all auth tokens |
| `Jwt:Issuer` | Token issuer name | Yes |
| `Jwt:Audience` | Token audience name | Yes |
| `Jwt:ExpiryMinutes` | How long JWT tokens last | Yes |

---

## 2. appsettings.Development.json — Optional Services

**File:** `SafeZone.Server/appsettings.Development.json`

All services in this file are **opt-in**. Leave them empty and the app runs in full simulation mode.

### 2.1 Gmail API (Real Email Sending)

```json
"Gmail": {
  "ClientId": "your-client-id.apps.googleusercontent.com",
  "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxx",
  "RefreshToken": "1//xxxxxxxxxxxxxxxxxxxx",
  "FromEmail": "youraccount@gmail.com",
  "ApplicationName": "SafeZone"
}
```

| Setting | Notes |
|---------|-------|
| `ClientId` | From Google Cloud Console → OAuth 2.0 Client ID |
| `ClientSecret` | From Google Cloud Console → OAuth 2.0 Client Secret |
| `RefreshToken` | Generated via OAuth 2.0 Playground (see below) |
| `FromEmail` | The Gmail address that sends emails |
| `ApplicationName` | Display name (default: SafeZone) |

**Step-by-step setup:**

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a project or select existing
3. Enable **Gmail API** (APIs & Services → Library → search "Gmail")
4. Go to **APIs & Services → Credentials**
5. Create OAuth 2.0 Client ID (Web application type)
6. Add `https://developers.google.com/oauthplayground` to Authorized redirect URIs
7. Copy `ClientId` and `ClientSecret`
8. Go to [OAuth 2.0 Playground](https://developers.google.com/oauthplayground)
9. Click gear icon → check "Use your own OAuth credentials" → paste ClientId/Secret
10. Select scope: `https://www.googleapis.com/auth/gmail.send`
11. Click "Authorize APIs" → sign in with your Gmail account
12. Click "Exchange authorization code for tokens"
13. Copy the **Refresh token**
14. Fill all values in `appsettings.Development.json`

**Leave all fields empty to disable.** The app logs "[Gmail API] Not configured" and continues normally.

### 2.2 Slack Webhook

```json
"Slack": {
  "WebhookUrl": "https://hooks.slack.com/services/T00000000/B00000000/xxxxxxxxxxxxxxxx"
}
```

| Setting | Notes |
|---------|-------|
| `WebhookUrl` | Full Slack incoming webhook URL |

**How to get it:**
1. Go to `https://api.slack.com/apps`
2. Create App → Incoming Webhooks → Activate
3. Copy the webhook URL
4. Posts go to `#authority-alerts` channel

**Leave empty to disable.** Critical incidents and SOS alerts post to Slack when configured.

### 2.3 Groq LLM (AI Emergency Scripts)

```json
"Groq": {
  "ApiKey": "gsk_your_groq_api_key_here",
  "ModelName": "llama-3.1-8b-instant",
  "Endpoint": "https://api.groq.com/openai/v1"
}
```

| Setting | Notes |
|---------|-------|
| `ApiKey` | From [console.groq.com](https://console.groq.com) |
| `ModelName` | Any Groq-supported model |
| `Endpoint` | Groq API base URL |

**Leave `ApiKey` empty** to use built-in keyword-based mock LLM.

### 2.4 ElevenLabs Voice Agent Widget

The landing page includes a floating voice agent widget that lets visitors speak to the SafeZone AI assistant. When a call completes, ElevenLabs sends a webhook to `POST /api/elevenlabswebhook` which creates an incident automatically.

**Widget configuration — `Pages/_Host.cshtml`:**
```html
<elevenlabs-convai agent-id="YOUR_AGENT_ID_HERE"></elevenlabs-convai>
```

| Setting | Notes |
|---------|-------|
| `agent-id` | Your ElevenLabs Convai agent ID (get from [elevenlabs.io](https://elevenlabs.io)) |

**Webhook flow:**
1. User calls via ElevenLabs widget
2. ElevenLabs sends POST to `https://your-server.com/api/elevenlabswebhook`
3. Webhook parses `dynamic_variables` (category, severity, location, description)
4. Creates incident in database
5. Broadcasts via SignalR MapHub → `ReportNewIncident`
6. Falls back to "Suspicious Activity" / Medium severity / Islamabad coords if fields missing

**No API key needed** — the widget auto-initializes from the agent-id in the HTML. The webhook is public (no auth required, but should be restricted to ElevenLabs IP ranges in production).

#### Webhook Tunneling (Local Dev)

ElevenLabs requires a public HTTPS URL for webhooks. Your local machine is NOT publicly reachable — you need a tunnel.

**Option A: Cloudflare Tunnel (free, no account limits)**

```bash
# Install once
winget install cloudflare.cloudflared
# Start tunnel
cloudflared tunnel --url https://localhost:7026
# → Output: https://your-random.trycloudflare.com
```
Copy the URL and set it as your ElevenLabs webhook: `https://your-random.trycloudflare.com/api/elevenlabswebhook`

**Option B: ngrok (the one you're already using)**

```bash
ngrok http https://localhost:7026
# → https://xxxx.ngrok.io → /api/elevenlabswebhook
```

**Option C: VS Dev Tunnels (built into Visual Studio 2022)**

1. In VS, click the dropdown next to the run button
2. Select "Dev Tunnels" → "Create Tunnel"
3. Set tunnel type to "Persistent"  
4. Set the ElevenLabs webhook URL to: `https://your-tunnel.devtunnels.ms/api/elevenlabswebhook`

**Test the webhook locally:**

```bash
curl -X POST https://localhost:7026/api/elevenlabswebhook `
  -H "Content-Type: application/json" `
  -d '{
    "agent_id": "test-123",
    "conversation_id": "conv-456",
    "caller_phone_number": "+923001234567",
    "dynamic_variables": {
      "category": "Fire",
      "severity": "high",
      "address": "Blue Area, Islamabad",
      "latitude": "33.6938",
      "longitude": "73.0560",
      "description": "Fire reported at commercial building"
    }
  }'
```

### 2.5 Google OAuth (Social Login)

```json
"Authentication": {
  "Google": {
    "ClientId": "your-client-id.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxx"
  }
}
```

**How to get it:**
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. APIs & Services → Credentials → Create OAuth 2.0 Client ID
3. Add `https://localhost:7026/external-login-callback` as redirect URI

**Leave empty to disable Google login.**

---

## 3. User Secrets (Recommended for Production)

Never commit real API keys to git. Use User Secrets:

```bash
cd SafeZone.Server
dotnet user-secrets set "Smtp:Password" "your-real-password"
dotnet user-secrets set "Groq:ApiKey" "gsk_real_key"
dotnet user-secrets set "Slack:WebhookUrl" "https://hooks.slack.com/services/..."
dotnet user-secrets set "Jwt:Key" "your-real-32-char-secret"
```

User secrets override `appsettings.json` at runtime.

---

## 4. Test Accounts (Pre-Seeded)

These users exist automatically on first run:

| Role | Phone | Password | Access |
|------|-------|----------|--------|
| SuperAdmin | +92511234567 | Admin123! | Everything + user management |
| Authority | +92511112233 | Officer123! | Authority dashboard + FIR review |
| Resident | +923001234567 | User123! | Report incidents + SOS |

---

## 5. Database

**SQLite — zero configuration needed.**

On first run, the database is auto-created at `SafeZone.Server/SafeZone.db` and seeded with:
- 4 roles (Resident, Authority, Admin, SuperAdmin)
- 15 incident categories
- 3 test users
- 5 sample incidents

To reset: delete `SafeZone.db` and restart. It auto-recreates.

---

## 6. Running the App

### Development
```bash
cd SafeZone.Server
dotnet run
# → http://localhost:5002 (HTTP)
# → https://localhost:7026 (HTTPS)
# Swagger: /swagger
```

### Production Publish
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet SafeZone.Server.dll --urls "http://0.0.0.0:5000"
```

### Deploy to Azure App Service
```bash
dotnet publish -c Release -o ./publish
# Deploy ./publish folder to Azure App Service (Windows)
# Set WEBSITE_WEBDEPLOY_USE_SCM=false in App Settings
```

---

## 7. What Works Out of the Box (No Config Needed)

Everything below runs in **simulation/mock mode** with zero API keys:

- Incident reporting & management
- Kanban board with status transitions
- Live dispatch map with clustered markers
- SOS emergency system
- FIR filing & review workflow
- Weather & heat map
- Voice call simulation (no real calls made)
- Auth (phone + password, cookie + JWT)
- Audit logging
- File upload (local filesystem)
- PDF generation for FIR reports
- Real-time SignalR notifications
- Analytics endpoints

---

## 8. Quick Checklist

| Feature | What to Configure | Where |
|---------|------------------|-------|
| [ ] JWT Auth | Set `Jwt:Key` to 32+ chars | `appsettings.json` |
| [ ] Email | Set `Smtp:Host/User/Password` | `appsettings.Development.json` |
| [ ] Slack | Set `Slack:WebhookUrl` | `appsettings.Development.json` |
| [ ] AI LLM | Set `Groq:ApiKey` | `appsettings.Development.json` |
| [ ] Google Login | Set `Authentication:Google:ClientId/Secret` | `appsettings.Development.json` |
| [ ] Production Deploy | Move all secrets to User Secrets / Environment Variables | `dotnet user-secrets` |
