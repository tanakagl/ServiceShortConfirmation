-- Script para integração do sistema de alerta de boletas com MV_PARAM_EMPRESA existente
-- Executar no Oracle conforme ambiente

-- Adicionar colunas para controle de notificação de boletas na tabela existente
ALTER TABLE OPUS.MV_PARAM_EMPRESA 
ADD (
    IN_NOTIFICA_BOLETA_REAPROVACAO CHAR(1) DEFAULT 'N' CHECK (IN_NOTIFICA_BOLETA_REAPROVACAO IN ('S','N')),
    NR_PERIODO_HORAS_BOLETA NUMBER(3) DEFAULT 24,
    DT_ULTIMA_EXECUCAO_BOLETA DATE
);

COMMENT ON COLUMN OPUS.MV_PARAM_EMPRESA.IN_NOTIFICA_BOLETA_REAPROVACAO IS 'Flag de ativação do alerta de boletas em reaprovação (S=Sim, N=Não)';
COMMENT ON COLUMN OPUS.MV_PARAM_EMPRESA.NR_PERIODO_HORAS_BOLETA IS 'Período em horas entre execuções do alerta de boletas';
COMMENT ON COLUMN OPUS.MV_PARAM_EMPRESA.DT_ULTIMA_EXECUCAO_BOLETA IS 'Data e hora da última execução do alerta de boletas';

-- Inicializar parâmetros para todas as empresas existentes (desativados por padrão)
UPDATE OPUS.MV_PARAM_EMPRESA 
SET IN_NOTIFICA_BOLETA_REAPROVACAO = 'N',
    NR_PERIODO_HORAS_BOLETA = 24
WHERE IN_NOTIFICA_BOLETA_REAPROVACAO IS NULL;

COMMIT; 