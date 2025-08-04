param(
    [Parameter()]
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot)
)

Write-Host "Configurando tarefa agendada para AlertaBoletaService..." -ForegroundColor Green
Write-Host "Caminho do projeto: $ProjectPath" -ForegroundColor Yellow

$csprojPath = Join-Path $ProjectPath "AlertaBoletaService.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Error "ERRO: Arquivo do projeto não encontrado em: $csprojPath"
    exit 1
}

$taskName = "AlertaBoletaService-Daily"

$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "Tarefa '$taskName' já existe. Removendo..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

$action = New-ScheduledTaskAction -Execute "dotnet" -Argument "run --project `"$csprojPath`" --launch-profile AlertaBoletaService-Homolog" -WorkingDirectory $ProjectPath
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At "08:00:00"
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive

Write-Host "Criando tarefa agendada..." -ForegroundColor Green
$task = New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description "Executa AlertaBoletaService todos os dias de segunda a sexta às 8h da manhã em ambiente Homolog"

Register-ScheduledTask -TaskName $taskName -InputObject $task

$createdTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($createdTask) {
    Write-Host "Tarefa agendada criada com sucesso!" -ForegroundColor Green
    Write-Host "Configuração:" -ForegroundColor Cyan
    Write-Host " Nome: $taskName" -ForegroundColor White
    Write-Host "   Horário: 8:00h (segunda a sexta)" -ForegroundColor White
    Write-Host "   Ambiente: Homolog" -ForegroundColor White
} else {
    Write-Error "Erro ao criar tarefa agendada!"
    exit 1
}