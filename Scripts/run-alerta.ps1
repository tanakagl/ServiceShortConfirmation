param(
    [string]$LogDir = "C:\temp\alerta-boleta-logs",
    [string]$ContainerName = "alerta-boleta-service",
    [string]$ImageName = "alerta-boleta-service:latest"
)

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "$timestamp - $Message"
    Write-Host $logMessage
    Add-Content -Path $LogFile -Value $logMessage
}

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

$LogFile = Join-Path $LogDir "alerta-$(Get-Date -Format 'yyyyMMdd').log"

Write-Log "Iniciando execução do AlertaBoletaService"

try {
    $dockerRunning = docker info 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Log "ERRO: Docker não está rodando ou não está instalado"
        exit 1
    }

    $imageExists = docker images $ImageName --format "{{.Repository}}:{{.Tag}}" 2>$null
    if (-not $imageExists) {
        Write-Log "AVISO: Imagem '$ImageName' não encontrada. Tentando build..."
        
        Write-Log "Executando docker build..."
        docker build -t $ImageName . 2>&1 | Tee-Object -FilePath $LogFile -Append
        
        if ($LASTEXITCODE -ne 0) {
            Write-Log "ERRO: Falha no build da imagem Docker"
            exit 1
        }
    }

    $runningContainer = docker ps -q -f name=$ContainerName 2>$null
    if ($runningContainer) {
        Write-Log "Parando container existente"
        docker stop $ContainerName | Out-Null
    }

    $existingContainer = docker ps -aq -f name=$ContainerName 2>$null
    if ($existingContainer) {
        Write-Log "Removendo container existente"
        docker rm $ContainerName | Out-Null
    }

    Write-Log "Executando container"
    
    $dockerArgs = @(
        "run", "--name", $ContainerName,
        "--rm",
        "-e", "ASPNETCORE_ENVIRONMENT=Production",
        $ImageName
    )
    
    $output = docker @dockerArgs 2>&1
    $exitCode = $LASTEXITCODE
    
    $output | ForEach-Object { Write-Log "Docker: $_" }
    
    if ($exitCode -eq 0) {
        Write-Log "Execução concluída com sucesso"
    } else {
        Write-Log "Execução falhou com código $exitCode"
    }
    
    exit $exitCode
}
catch {
    Write-Log "ERRO GERAL: $($_.Exception.Message)"
    exit 1
}
finally {
    $existingContainer = docker ps -aq -f name=$ContainerName 2>$null
    if ($existingContainer) {
        docker rm $ContainerName 2>$null | Out-Null
    }
} 