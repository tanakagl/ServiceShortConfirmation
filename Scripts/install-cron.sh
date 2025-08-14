SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUN_SCRIPT="$SCRIPT_DIR/run-alerta.sh"
CRON_USER="root"

echo "Configuring cron job for AlertaBoletaService..."

if [ ! -f "$RUN_SCRIPT" ]; then
    echo "ERROR: Script $RUN_SCRIPT not found!"
    exit 1
fi

chmod +x "$RUN_SCRIPT"

CRON_ENTRY="0 8,9 * * 1-5 $RUN_SCRIPT"

if crontab -u $CRON_USER -l 2>/dev/null | grep -q "$RUN_SCRIPT"; then
    echo "Cron job already exists. Removing previous configuration..."
    crontab -u $CRON_USER -l 2>/dev/null | grep -v "$RUN_SCRIPT" | crontab -u $CRON_USER -
fi

echo "Adding cron job: $CRON_ENTRY"
(crontab -u $CRON_USER -l 2>/dev/null; echo "$CRON_ENTRY") | crontab -u $CRON_USER -

if crontab -u $CRON_USER -l 2>/dev/null | grep -q "$RUN_SCRIPT"; then
    echo "Cron job configured successfully!"
    echo "Current configuration:"
    crontab -u $CRON_USER -l | grep "$RUN_SCRIPT"
    echo ""
    echo "Logs will be saved in: /var/log/alerta-boleta/"
    echo "Execution: Monday to Friday at 8AM and 9AM"
else
    echo "Error configuring cron job!"
    exit 1
fi 