# Gmail API + Slack Webhook — Setup Guide

Based on official documentation from [Google Gmail API](https://developers.google.com/gmail/api/guides/sending) and [Slack Incoming Webhooks](https://api.slack.com/messaging/webhooks).

---

## Part 1: Slack Incoming Webhook

### Step 1: Create a Slack App

1. Go to https://api.slack.com/apps
2. Click **Create New App** → **From scratch**
3. Name it `SafeZone Alerts` → pick your workspace → **Create App**

### Step 2: Enable Incoming Webhooks

1. In the left sidebar, click **Incoming Webhooks**
2. Toggle **Activate Incoming Webhooks** to **On**

### Step 3: Generate the Webhook URL

1. Click **Add New Webhook to Workspace**
2. Pick a channel (e.g., `#authority-alerts`, or create one)
3. Click **Allow**
4. Copy the URL:
```
SLACK_INCOMING_WEBHOOK_URL
```

### Step 4: Configure SafeZone

Paste the URL into `SafeZone.Server/appsettings.Development.json`:
```json
"Slack": {
  "WebhookUrl": "SLACK_INCOMING_WEBHOOK_URL"
}
```

### Step 5: Verify

```bash
curl -X POST -H "Content-type: application/json" \
  -d '{"text":"SafeZone webhook test — connection successful!"}' \
  "$SLACK_INCOMING_WEBHOOK_URL"
```
You should see the message in your Slack channel.

### What gets posted to Slack:

| Trigger | Format |
|---------|--------|
| Critical/High incident | `*SafeZone Alert:* [Title]` with color-coded severity |
| SOS emergency | `SOS EMERGENCY: Police Emergency — triggered by [User] at [Location]` |
| FIR accepted/rejected | `FIR FIR-2026-0001 — Accepted` |
| App startup | (no notification — Slack is event-driven only) |

---

## Part 2: Gmail API (Send Emails)

### Step 1: Create a Google Cloud Project

1. Go to https://console.cloud.google.com
2. Click the project dropdown (top bar) → **New Project**
3. Name: `SafeZone` → **Create**

### Step 2: Enable the Gmail API

1. In the left menu, go to **APIs & Services** → **Library**
2. Search for `Gmail API`
3. Click **Gmail API** → **Enable**

### Step 3: Configure the OAuth Consent Screen

1. Go to **APIs & Services** → **OAuth consent screen**
2. Choose **External** user type → **Create**
3. Fill in:
   - App name: `SafeZone`
   - User support email: your email
   - Developer contact email: your email
4. Click **Save and Continue** (skip scopes and test users pages)

Wait — since this is a personal/dev project, you can skip this for testing:

### Step 4 (Quick Method): Use OAuth 2.0 Playground

Instead of building a full OAuth flow, use Google's OAuth Playground to get a refresh token:

1. Go to https://developers.google.com/oauthplayground
2. Click the **gear icon** (⚙️) in the top right
3. Check **"Use your own OAuth credentials"**
4. But first, get credentials:

### Step 5: Create OAuth 2.0 Credentials

1. Go to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth client ID**
3. Application type: **Web application**
4. Name: `SafeZone OAuth`
5. Under **Authorized redirect URIs**, click **Add URI**:
   ```
   https://developers.google.com/oauthplayground
   ```
6. Click **Create**
7. Copy the **Client ID** and **Client Secret** shown in the popup

### Step 6: Get the Refresh Token

1. Back in the [OAuth 2.0 Playground](https://developers.google.com/oauthplayground):
   - Paste your Client ID and Client Secret in the gear settings
   - Close the settings dialog
2. In Step 1 (left panel), scroll down to **Gmail API v1**
3. Check the scope: `https://www.googleapis.com/auth/gmail.send`
4. Click **Authorize APIs**
5. Sign in with the **Gmail account you want to send FROM**
6. Accept the consent screen
7. In Step 2, click **Exchange authorization code for tokens**
8. Copy the **Refresh token** from the response (it starts with `1//`)

### Step 7: Configure SafeZone

Paste all values into `SafeZone.Server/appsettings.Development.json`:
```json
"Gmail": {
  "ClientId": "GOOGLE_OAUTH_CLIENT_ID",
  "ClientSecret": "GOOGLE_OAUTH_CLIENT_SECRET",
  "RefreshToken": "GMAIL_REFRESH_TOKEN",
  "FromEmail": "yourname@gmail.com",
  "ApplicationName": "SafeZone"
}
```
- `FromEmail` must be the **same Gmail account** you authorized in the OAuth Playground
- `ApplicationName` shows as the sender display name in recipients' inboxes

### Step 8: Verify

Run the app and trigger an incident. Check Console output:
```
[Gmail API] Not configured — logging: Your Incident is Registered to +923001234567
```
This means Gmail IS NOT yet configured (check your JSON syntax).

When configured correctly:
```
[Gmail API] Email sent to +923001234567: Your Incident is Registered
```

### What emails are sent:

| Trigger | Subject | Recipient |
|---------|---------|-----------|
| New incident reported | `[High] Incident Alert: Car Theft` | Reporter's phone (as email) |
| SOS emergency | `[Critical] Incident Alert: SOS: Police` | User's phone |
| FIR accepted | `FIR FIR-2026-0001 — Status Update` | FIR reporter |
| FIR rejected | `FIR FIR-2026-0001 — Status Update` | FIR reporter |
| ElevenLabs webhook | (anonymous — no reporter email) | — |

---

## Part 3: Testing Everything End-to-End

1. Start the app:
```bash
dotnet run --launch-profile https
```

2. Login as Resident (+923001234567 / User123!)

3. Report an incident → check:
   - Slack channel for the alert
   - Console log for Gmail status

4. Login as Authority (+92511112233 / Officer123!)

5. Review the FIR → check:
   - Slack for "FIR accepted"
   - Console for email sent

6. Trigger SOS → check:
   - Slack for "SOS EMERGENCY"
   - Console for email

### Troubleshooting

| Problem | Fix |
|---------|-----|
| Slack: "invalid_payload" | Check JSON syntax in your appsettings |
| Slack: "no_service" | Webhook URL was revoked — regenerate |
| Gmail: "invalid_grant" | Refresh token expired or wrong account |
| Gmail: "Not configured" | `ClientId`, `ClientSecret`, or `RefreshToken` is empty |
| Neither works | Leave all fields empty — app runs fine without them |
