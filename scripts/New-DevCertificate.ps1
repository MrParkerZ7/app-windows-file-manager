# New-DevCertificate.ps1
# Generates a self-signed code signing certificate for local MSIX testing.
# Run from an elevated PowerShell prompt.
#
# IMPORTANT: The -Subject value MUST match the Publisher in Package.appxmanifest.
# Currently: CN=WindowsFileManager

param(
    [string]$Subject = "CN=WindowsFileManager",
    [string]$FriendlyName = "Windows File Manager Dev Certificate",
    [string]$OutputPath = ".\certificate.pfx",
    [string]$Password = "DevPassword123!"
)

$ErrorActionPreference = "Stop"

# Create the self-signed certificate
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName $FriendlyName `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

Write-Host "Certificate created: $($cert.Thumbprint)" -ForegroundColor Green

# Export to PFX
$securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" `
    -FilePath $OutputPath `
    -Password $securePassword | Out-Null

Write-Host "Exported to: $OutputPath" -ForegroundColor Green

# Install to TrustedPeople for local sideloading
try {
    Import-PfxCertificate `
        -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" `
        -FilePath $OutputPath `
        -Password $securePassword | Out-Null
    Write-Host "Installed to TrustedPeople store (sideloading enabled)" -ForegroundColor Green
}
catch {
    Write-Host "Could not install to TrustedPeople — run as Administrator for sideloading support" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Build MSIX:  dotnet publish src/WindowsFileManager -c Release -r win-x64 -p:WindowsPackageType=MSIX"
Write-Host "  2. For GitHub Actions, encode the PFX as base64:"
Write-Host "     [Convert]::ToBase64String([IO.File]::ReadAllBytes('$OutputPath')) | Set-Clipboard"
Write-Host "  3. Store in GitHub Secrets as CERTIFICATE_PFX and CERTIFICATE_PASSWORD"
