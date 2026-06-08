# SafeZone on Render

This project is prepared for a free Render web service with an external PostgreSQL database such as Neon.

## Why Postgres

Render web services use an ephemeral filesystem unless you attach a paid disk. SafeZone now keeps SQLite for local development and automatically switches to PostgreSQL when either `DATABASE_URL` or `ConnectionStrings__PostgresConnection` is set.

## Deploy Steps

1. Push this repository to GitHub.
2. Create a free Neon PostgreSQL database and copy its pooled connection string.
3. In Render, create a new Blueprint from this repository. Render reads `render.yaml`.
4. When Render asks for unsynced variables, paste:
   - `DATABASE_URL`: Neon PostgreSQL connection string
   - `Gmail__ClientId`
   - `Gmail__ClientSecret`
   - `Gmail__RefreshToken`
   - `Gmail__FromEmail`
   - `Slack__WebhookUrl`
5. Deploy the service.

## ElevenLabs Webhook

After Render deploys, use this URL in ElevenLabs:

```text
https://YOUR-SERVICE-NAME.onrender.com/api/ElevenLabsWebhook
```

The app currently accepts unsigned ElevenLabs server-tool calls because your provided ElevenLabs tool config has no auth connection.

## Health Check

```text
https://YOUR-SERVICE-NAME.onrender.com/health
```
