# Oracle Always Free Deployment

This project is a Blazor Server / ASP.NET Core app, so it needs a persistent VM or container host. Oracle Always Free can run it as a Docker Compose deployment.

## What Gets Deployed

- `safezone`: the ASP.NET Core app on port `8080`
- `caddy`: HTTPS reverse proxy on ports `80` and `443`
- Persistent Docker volumes for SQLite and TLS certificates

The public hostname uses the free `sslip.io` DNS pattern:

```text
https://<oracle-public-ip>.sslip.io
https://<oracle-public-ip>.sslip.io/api/ElevenLabsWebhook
```

## VM Requirements

- Ubuntu image
- Open inbound ports `22`, `80`, and `443`
- Always Free shape:
  - Best: `VM.Standard.A1.Flex`, 1 OCPU, 6 GB RAM
  - Fallback: `VM.Standard.E2.1.Micro`

## Deploy After VM Creation

From the project root:

```powershell
.\deploy\oracle\publish-to-vm.ps1 -PublicIp "<oracle-public-ip>" -SshKeyPath "C:\path\to\ssh-key"
```

The script uploads the project, installs Docker, builds the app, starts HTTPS, and prints the final site/webhook URLs.
