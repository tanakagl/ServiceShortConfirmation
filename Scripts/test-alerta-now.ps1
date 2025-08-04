param(
    [Parameter()]
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot)
)

Write-Host "Testando AlertaBoletaService em ambiente Homolog..." -ForegroundColor Green

$csprojPath = Join-Path $ProjectPath "AlertaBoletaService.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Error "ERRO: Arquivo do projeto não encontrado em: $csprojPath"
    exit 1
}

Write-Host "Executando AlertaBoletaService..." -ForegroundColor Cyan

Set-Location $ProjectPath
dotnet run --project $csprojPath --launch-profile AlertaBoletaService-Homolog 