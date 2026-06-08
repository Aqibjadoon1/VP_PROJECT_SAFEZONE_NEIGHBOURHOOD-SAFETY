param(
    [Parameter(Mandatory = $true)]
    [string] $PublicIp,

    [Parameter(Mandatory = $true)]
    [string] $SshKeyPath,

    [string] $User = "ubuntu"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$work = Join-Path $env:TEMP ("safezone-oracle-" + [Guid]::NewGuid().ToString("N"))
$archive = Join-Path $work "safezone.tar.gz"
$hostname = "$PublicIp.sslip.io"

New-Item -ItemType Directory -Path $work | Out-Null

try {
    Push-Location $root
    tar `
        --exclude='.git' `
        --exclude='.vs' `
        --exclude='.agents' `
        --exclude='bin' `
        --exclude='obj' `
        --exclude='*.log' `
        --exclude='SafeZone.Server/SafeZone.db' `
        -czf $archive .
    Pop-Location

    scp -i $SshKeyPath -o StrictHostKeyChecking=accept-new $archive "${User}@${PublicIp}:/tmp/safezone.tar.gz"
    scp -i $SshKeyPath -o StrictHostKeyChecking=accept-new (Join-Path $PSScriptRoot "bootstrap-vm.sh") "${User}@${PublicIp}:/tmp/bootstrap-vm.sh"

    $remote = @"
set -euo pipefail
bash /tmp/bootstrap-vm.sh
rm -rf /opt/safezone/source
mkdir -p /opt/safezone/source
tar -xzf /tmp/safezone.tar.gz -C /opt/safezone/source
cd /opt/safezone/source/deploy/oracle
cp Caddyfile.template Caddyfile
sed -i 's/__SAFEZONE_HOSTNAME__/$hostname/g' Caddyfile
if [ ! -f .env ]; then
  JWT_KEY=`$(openssl rand -base64 48)
  cat > .env <<EOF
JWT_KEY=`$JWT_KEY
EOF
fi
sudo docker compose up -d --build
sudo docker compose ps
"@

    ssh -i $SshKeyPath -o StrictHostKeyChecking=accept-new "${User}@${PublicIp}" $remote

    Write-Host ""
    Write-Host "SafeZone URL: https://$hostname"
    Write-Host "ElevenLabs webhook URL: https://$hostname/api/ElevenLabsWebhook"
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
