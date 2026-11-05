# Agenda — serviço e composição em RED

Data: 03/11/2026. Aguardando aprovação dos 15 casos antes da implementação.

**Atualização:** aprovação explícita recebida em 03/11. O GREEN posterior está no [relatório 066](066-agenda-service-green.md); os resultados RED abaixo são históricos.

## Escopo

IAgendaActions, serviço vazio SqliteAgendaActions e AgendaServiceTests. Guardas de caminho são a estrutura mínima para compilar a especificação. Nenhuma operação funcional, registro de composição, migração ou alteração de formulário nesta etapa.

15 casos compilados: 11 falhas esperadas e quatro proteções passando. Dez falhas ocorrem nas operações ainda não implementadas; uma na ausência do serviço em DesktopComposition. Não são falhas de compilação. Os testes posteriores de concorrência/recuperação serão efetivamente exercidos no GREEN.

Casos relevantes: abrir banco existente, validar antes de acessar disco, respeitar cancelamento, consultar dados por outra instância, isolar perfis, recuperar após falha e preservar seis gravações simultâneas em tarefas distintas. A composição deve fornecer a fachada para o perfil correto, sem expor DbContext ao consumidor.

## Verificações

```sh
dotnet test tests/Trama.IntegrationTests -c Release --no-restore --filter FullyQualifiedName~AgendaServiceTests --logger trx --results-directory artifacts/agenda-service-red --verbosity quiet
dotnet test tests/Trama.UnitTests -c Release --no-restore --logger trx --results-directory artifacts/agenda-service-red/unit --verbosity quiet
dotnet test tests/Trama.IntegrationTests -c Release --no-restore --filter FullyQualifiedName~AgendaStorageTests --logger trx --results-directory artifacts/agenda-service-red/storage --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Imports do teste novo ajustados antes da aprovação. Os testes anteriores permanecem preservados. A suíte completa histórica de 583 aprovados pertence à entrega SQLite de 02/11; não foi repetida nesta especificação isolada e não deve ser apresentada como resultado novo.

Nesta execução, 101 unitários e os 24 casos de AgendaStorageTests passaram, nenhum ignorado. A verificação de formatação também passou. As proteções antigas relevantes foram conferidas sem repetir a suíte visual completa.

## Riscos e revisão

Não compartilhar estado rastreado entre operações, não criar arquivo vazio ao ler banco ausente e não transformar cancelamento em sucesso. Trigger de falha é restrito ao banco fictício dos testes. Nenhuma operação no perfil pessoal.

Após aprovação explícita conforme CODEX, seção 9, implementar o serviço e o registro, mantendo os 15 casos. Interface e proteção de rascunhos serão a próxima fatia. Migrações, manual/PDF/README e Git intactos.

Esforço baixo a moderado, sem subagentes. Reutilizar a fachada aprovada nos testes do formulário para evitar repetir toda a cobertura de SQLite na camada visual.

Estudo: [capítulo 57](../estudos/57-agenda-servico-especificacao.md).
