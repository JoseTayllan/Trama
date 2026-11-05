# Agenda — SQLite e recuperação em RED

Data: 02/11/2026. Aguardando revisão explícita dos 24 casos antes de implementar.

**Atualização:** os 24 casos e as regras receberam aprovação explícita em 02/11. O GREEN posterior está no [relatório 064](064-agenda-storage-green.md); resultados RED abaixo são históricos.

## Alterações da especificação

- Novo contrato IAgendaTaskReader e SqliteAgendaTaskRepository com métodos vazios, não registrados no desktop.
- AgendaStorageTests usa bancos/arquivos fictícios, migrações reais já existentes e pacotes com manifesto/hashes calculados.
- Nenhuma migração, mapeamento EF, implementação de repositório ou alteração dos backups existentes.
- Caso de uso e testes de regras anteriormente aprovados preservados.

## Execução

```sh
dotnet test tests/Trama.IntegrationTests -c Release --no-restore --filter FullyQualifiedName~AgendaStorageTests --logger trx --results-directory artifacts/agenda-storage-red --verbosity quiet
dotnet test tests/Trama.UnitTests -c Release --no-restore --logger trx --results-directory artifacts/agenda-storage-red/unit --verbosity quiet
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter 'FullyQualifiedName~BackupStorageTests|FullyQualifiedName~NotebookStorageTests' --logger trx --results-directory artifacts/agenda-storage-red/existing-storage --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

24 casos novos compilados/descobertos: 23 falhas e um passando. 21 falhas são dos métodos ainda vazios; dois casos constatam ausência da migração nova. As verificações finais de corrupção/restauração só serão avaliadas depois da implementação, sem alegar funcionalidade real neste RED.

Base afetada: 101 unitários e 32 testes anteriores de cadernos/backup passaram, nenhum ignorado. A suíte completa não foi repetida. A instabilidade de autosave do relatório 060 permanece aberta. Formatação dos testes novos ajustada antes da aprovação; testes anteriores intactos.

## Decisões para aprovação

Período inclusivo; ordem por dia, horário, título e identidade; sem horário depois das tarefas agendadas; pacote de backup permanece 1. Evoluir o banco com migração nova, restaurar backups antigos e rejeitar dados inválidos sem tocar na origem. Contextos continuam pertencendo ao chamador.

Após aprovação, implementar sem alterar os testes. A integração com a composição e os formulários ficará para outra fatia; não habilitar Agenda ainda. A seção 9 do CODEX exige esta pausa.

Riscos: migração/backups são fronteiras de dados; não usar perfil pessoal para testar. A corrupção deliberada é restrita às fixtures. Não recriar banco ou reescrever migrações. Manual/PDF/README e Git intactos.

Esforço moderado, sem subagentes. Reaproveitados contexto, fixtures e serviço de backup existentes; manter a próxima revisão concentrada em persistência e recuperação.

Estudo: [capítulo 55](../estudos/55-agenda-persistencia-especificacao.md).
