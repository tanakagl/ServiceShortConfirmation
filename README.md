# AlertaBoletaService

Worker Service para alerta automático de boletas em reaprovação.

## Build do Container

```bash
docker build -t alerta-boleta-service .
```

## Instalação (Linux)

```bash
# Tornar scripts executáveis
chmod +x Scripts/*.sh

# Instalar cron job
sudo Scripts/install-cron.sh
```

## Execução Manual

### Linux
```bash
Scripts/run-alerta.sh
```

### Windows
```powershell
Scripts/run-alerta.ps1
```

## Logs

**Linux**: `/var/log/alerta-boleta/alerta-YYYYMMDD.log`

**Windows**: `C:\temp\alerta-boleta-logs\alerta-YYYYMMDD.log`
