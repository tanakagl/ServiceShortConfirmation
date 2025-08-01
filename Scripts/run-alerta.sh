LOG_DIR="/var/log/alerta-boleta"
LOG_FILE="$LOG_DIR/alerta-$(date +%Y%m%d).log"
CONTAINER_NAME="alerta-boleta-service"
IMAGE_NAME="alerta-boleta-service:latest"


mkdir -p $LOG_DIR

echo "$(date '+%Y-%m-%d %H:%M:%S') - Iniciando execução do AlertaBoletaService" >> $LOG_FILE

if [ $(docker ps -q -f name=$CONTAINER_NAME) ]; then
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Parando container existente" >> $LOG_FILE
    docker stop $CONTAINER_NAME
fi

if [ $(docker ps -aq -f name=$CONTAINER_NAME) ]; then
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Removendo container existente" >> $LOG_FILE
    docker rm $CONTAINER_NAME
fi

echo "$(date '+%Y-%m-%d %H:%M:%S') - Executando container" >> $LOG_FILE
docker run --name $CONTAINER_NAME \
    --network host \
    -e ASPNETCORE_ENVIRONMENT=Production \
    $IMAGE_NAME >> $LOG_FILE 2>&1

EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Execução concluída com sucesso" >> $LOG_FILE
else
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Execução falhou com código $EXIT_CODE" >> $LOG_FILE
fi

docker rm $CONTAINER_NAME >> /dev/null 2>&1

exit $EXIT_CODE 