# AlertaBoletaService

**Worker Service** para alerta automático de boletas em reaprovação integrado ao sistema OPUS-COMEX.
**Executa uma única vez** via **cron jobs** - sem API.

## 📋 Funcionalidades

- ✅ **Execução Única**: Processa alertas e finaliza automaticamente
- ✅ **Envio de Email**: Alertas automáticos por email em HTML
- ✅ **Configuração por Empresa**: Parâmetros via tabela `MV_PARAM_EMPRESA`
- ✅ **Integração Oracle**: Acesso direto ao banco de dados do COMEX
- ✅ **Cron Ready**: Pronto para agendamento via cron jobs
- ✅ **Docker Support**: Container para deploy fácil
- ⚠️ **Sem API**: Apenas Worker Service para máxima simplicidade

## 🏗️ Arquitetura

**Worker Service simples:**
- Executa uma vez e para
- Busca empresas ativas na `MV_PARAM_EMPRESA`
- Processa boletas em status `'RP'` (reaprovação)
- Busca emails de usuários com permissão
- Envia alertas e finaliza

## ⚙️ Configuração

### 1. Configurar Banco Oracle

Execute o script `SQL/CreateTables.sql` no Oracle:

```sql
-- Adiciona 3 colunas para alerta de boletas
ALTER TABLE OPUS.MV_PARAM_EMPRESA 
ADD (
    IN_NOTIFICA_BOLETA_REAPROVACAO CHAR(1) DEFAULT 'N',
    NR_PERIODO_HORAS_BOLETA NUMBER(3) DEFAULT 24,
    DT_ULTIMA_EXECUCAO_BOLETA DATE
);
```

### 2. Configurar Empresas (Via SQL)

```sql
-- Ativar para empresa específica
UPDATE OPUS.MV_PARAM_EMPRESA 
SET IN_NOTIFICA_BOLETA_REAPROVACAO = 'S',
    NR_PERIODO_HORAS_BOLETA = 24
WHERE ID_EMPRESA = 1;

-- Ativar para múltiplas empresas
UPDATE OPUS.MV_PARAM_EMPRESA 
SET IN_NOTIFICA_BOLETA_REAPROVACAO = 'S'
WHERE ID_EMPRESA IN (1, 2, 3, 5);
```

### 3. Build do Container

```bash
# Build da imagem
docker build -t alerta-boleta-service .

# Teste manual
docker run --rm --name alerta-boleta-test alerta-boleta-service
```

## 🕐 Configuração do Cron

### **Instalação Automática**
```bash
# Tornar scripts executáveis
chmod +x Scripts/*.sh

# Instalar cron job (executar como root)
sudo Scripts/install-cron.sh
```

### **Configuração Manual**
```bash
# Editar crontab
crontab -e

# Adicionar linha (exemplo: executar a cada 2 horas)
0 */2 * * * /caminho/para/Scripts/run-alerta.sh

# Outros exemplos:
# A cada 30 minutos: */30 * * * *
# Diariamente às 8h:  0 8 * * *
# A cada 6 horas:     0 */6 * * *
```

## 🚀 Como Funciona

### **Fluxo de Execução:**
```
1. Cron aciona → Scripts/run-alerta.sh
2. Script executa → docker run alerta-boleta-service
3. Container inicia → Worker Service
4. Worker processa empresas ativas uma vez
5. Para cada empresa ativa:
   - Verifica período (horas desde última execução)
   - Busca boletas em reaprovação (DS_STATUS_BOLETA = 'RP')
   - Busca emails de usuários com permissão
   - Envia alertas se houver boletas
   - Atualiza DT_ULTIMA_EXECUCAO_BOLETA
6. Container finaliza automaticamente
7. Script captura logs e exit code
```

### **Logs Automáticos:**
- **Localização**: `/var/log/alerta-boleta/`
- **Formato**: `alerta-YYYYMMDD.log`
- **Rotação**: Mantém últimos 30 dias

## 🔄 Controle de Execução

O serviço respeita o **período configurado por empresa**:

- Se `DT_ULTIMA_EXECUCAO_BOLETA` for NULL → Executa
- Se `NOW() >= ULTIMA_EXECUCAO + NR_PERIODO_HORAS_BOLETA` → Executa  
- Caso contrário → Pula empresa

**Exemplo**: Empresa configurada com 24h, última execução às 10h
- 11h → Não executa (só passou 1h)
- 22h → Não executa (só passaram 12h)  
- 10h30 do dia seguinte → Executa (passaram 24h30)

## 📧 Emails de Usuários

O sistema busca emails de usuários com permissão para renovar boletas:

```sql
-- Query atual (temporária)
SELECT DISTINCT u.EMAIL 
FROM MC_USUARIO u 
WHERE u.CD_EMPRESA = :empresaId 
  AND u.ATIVO = 'S'
  AND u.EMAIL IS NOT NULL
  AND (u.PERFIL LIKE '%BOLETA%' OR u.PERFIL LIKE '%RENOVAR%')
```

> **TODO**: Ajustar query conforme estrutura real da tabela de usuários

## 📈 Monitoramento

### **Verificar Logs:**
```bash
# Logs do dia atual
tail -f /var/log/alerta-boleta/alerta-$(date +%Y%m%d).log

# Logs dos últimos 7 dias
find /var/log/alerta-boleta -name "alerta-*.log" -mtime -7 -exec cat {} \;

# Buscar erros específicos
grep -i "erro\|error\|exception" /var/log/alerta-boleta/alerta-*.log
```

### **Verificar Cron:**
```bash
# Ver cron jobs instalados
crontab -l

# Logs do cron (varia por sistema)
sudo tail -f /var/log/cron

# Testar execução manual
sudo Scripts/run-alerta.sh
```

### **Verificar Empresas no Banco:**
```sql
-- Empresas com alerta ativo
SELECT ID_EMPRESA, IN_NOTIFICA_BOLETA_REAPROVACAO, 
       NR_PERIODO_HORAS_BOLETA, DT_ULTIMA_EXECUCAO_BOLETA
FROM OPUS.MV_PARAM_EMPRESA 
WHERE IN_NOTIFICA_BOLETA_REAPROVACAO = 'S';

-- Boletas em reaprovação por empresa
SELECT CD_EMPRESA, COUNT(*) as BOLETAS_PENDENTES
FROM OPUS.MV_BOLETA 
WHERE DS_STATUS_BOLETA = 'RP'
GROUP BY CD_EMPRESA;
```

## 🛠️ Troubleshooting

### **Container não inicia:**
```bash
# Verificar logs do container
docker logs alerta-boleta-service

# Testar conexão Oracle
docker run --rm alerta-boleta-service dotnet --version
```

### **Emails não são enviados:**
1. Verificar configuração SMTP no `appsettings.json`
2. Testar conectividade: `telnet smtp.amaggi.com.br 25`
3. Verificar se há usuários com permissão na empresa
4. Verificar logs para erros de autenticação

### **Cron não executa:**
```bash
# Verificar se cron está rodando
sudo systemctl status cron

# Verificar permissões do script
ls -la Scripts/run-alerta.sh

# Verificar se script encontra o Docker
which docker
```

### **Nenhuma boleta encontrada:**
```bash
# Verificar status das boletas
SELECT DS_STATUS_BOLETA, COUNT(*) 
FROM OPUS.MV_BOLETA 
GROUP BY DS_STATUS_BOLETA;

# Boletas por empresa
SELECT CD_EMPRESA, DS_STATUS_BOLETA, COUNT(*)
FROM OPUS.MV_BOLETA 
WHERE CD_EMPRESA = 1
GROUP BY CD_EMPRESA, DS_STATUS_BOLETA;
```

## 📋 Dependências

- .NET 8.0
- Oracle.EntityFrameworkCore
- Docker
- Cron (para agendamento)

## 🚀 Deploy Produção

```bash
# 1. Build da imagem
docker build -t alerta-boleta-service .

# 2. Configurar empresas no banco
-- Execute SQL para ativar empresas

# 3. Configurar cron
sudo Scripts/install-cron.sh

# 4. Testar execução manual
Scripts/run-alerta.sh

# 5. Verificar logs
tail -f /var/log/alerta-boleta/alerta-$(date +%Y%m%d).log
```

## 📞 Suporte

Para dúvidas ou problemas:
1. Verificar logs em `/var/log/alerta-boleta/`
2. Testar conectividade Oracle e SMTP
3. Validar configurações das empresas no banco
4. Consultar logs do cron/sistema 