# ElevenLabs Webhook — ngrok Setup

ElevenLabs requires HTTPS. Localhost is HTTP-only from the outside. ngrok creates a public HTTPS tunnel to your local machine.

## 1. Install ngrok

```bash
winget install ngrok.ngrok
```

Sign up at [ngrok.com](https://ngrok.com) (free). Get your auth token from the dashboard.

```bash
ngrok config add-authtoken YOUR_TOKEN
```

## 2. Start the tunnel

```bash
ngrok http https://localhost:7026
```

You'll see:

```
Session Status   online
Forwarding       https://abc123.ngrok-free.app → https://localhost:7026
```

## 3. Set the webhook URL in ElevenLabs

Copy the `https://abc123.ngrok-free.app` URL and add `/api/elevenlabswebhook`:

```
https://abc123.ngrok-free.app/api/elevenlabswebhook
```

Paste this into your ElevenLabs Convai agent's webhook settings.

## 4. Verify it works

```bash
curl -X POST https://abc123.ngrok-free.app/api/elevenlabswebhook `
  -H "Content-Type: application/json" `
  -d '{"agent_id":"test","conversation_id":"test","dynamic_variables":{"category":"Theft","severity":"high"}}'
```

You should get back:

```json
{"success":true,"message":"Incident received and logged.","incidentId":"...","incidentNumber":"INC-2026..."}
```

## 5. Test from ElevenLabs

Make a test call through the widget. Check VS output window — you'll see:

```
ElevenLabs webhook received. AgentId=agent_5701..., ConversationId=..., Phone=...
Incident created from ElevenLabs webhook. Id=..., Number=INC-..., Category=Theft
```

New incidents appear on the Dispatch Map in real time.

## Done

| Step | Command / URL |
|------|--------------|
| Start app | `dotnet run --launch-profile https` |
| Start tunnel | `ngrok http https://localhost:7026` |
| Webhook URL | `https://YOUR-ID.ngrok-free.app/api/elevenlabswebhook` |
