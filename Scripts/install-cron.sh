SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUN_SCRIPT="$SCRIPT_DIR/run-alerta.sh"
CRON_USER="root"

echo "Configurando cron job para AlertaBoletaService..."

if [ ! -f "$RUN_SCRIPT" ]; then
    echo "ERRO: Script $RUN_SCRIPT não encontrado!"
    exit 1
fi

chmod +x "$RUN_SCRIPT"

CRON_ENTRY="0 */2 * * * $RUN_SCRIPT"

if crontab -u $CRON_USER -l 2>/dev/null | grep -q "$RUN_SCRIPT"; then
    echo "Cron job já existe. Removendo configuração anterior..."
    crontab -u $CRON_USER -l 2>/dev/null | grep -v "$RUN_SCRIPT" | crontab -u $CRON_USER -
fi

echo "Adicionando cron job: $CRON_ENTRY"
(crontab -u $CRON_USER -l 2>/dev/null; echo "$CRON_ENTRY") | crontab -u $CRON_USER -

if crontab -u $CRON_USER -l 2>/dev/null | grep -q "$RUN_SCRIPT"; then
    echo "Cron job configurado com sucesso!"
    echo "Configuração atual:"
    crontab -u $CRON_USER -l | grep "$RUN_SCRIPT"
    echo ""
    echo "Logs serão salvos em: /var/log/alerta-boleta/"
    echo "Execução: A cada 2 horas"
    echo ""
    echo "Para alterar o horário, edite o cron com: crontab -e"
    echo "Formato: minuto hora dia mês dia_semana comando"
    echo "Exemplo para executar diariamente às 8h: 0 8 * * * $RUN_SCRIPT"
else
    echo "Erro ao configurar cron job!"
    exit 1
fi 