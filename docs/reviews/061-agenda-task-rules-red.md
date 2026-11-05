# Agenda — regras das tarefas em RED

Data: 02/11/2026. Implementação das regras aguardando aprovação dos 24 casos.

**Atualização:** aprovação explícita recebida em 02/11 para os 24 casos e suas regras. A implementação posterior está no [relatório 062](062-agenda-task-rules-green.md); resultados RED abaixo são históricos.

## Escopo

Três arquivos mínimos na Application/Agenda: snapshot/enums/resultados, contrato de repositório/exceção e entrada SaveAgendaTask sem implementação funcional. Um arquivo novo de testes unitários. Sem banco, migração, tela, dependências ou alteração da composição. Não foi habilitado o botão Agenda.

Regras propostas: título/ID obrigatórios; categorias pessoal/trabalho/estudo; estados pendente/em andamento/concluída; dia local e intervalo opcional completo no mesmo dia; preservação de conteúdo/identidade; falhas, espera e cancelamento. Reabrir tarefa mantém planejamento. Não criar duração realizada automaticamente.

## Execução

```sh
dotnet test tests/Trama.UnitTests -c Release --no-restore --filter FullyQualifiedName~SaveAgendaTaskTests --logger trx --results-directory artifacts/agenda-rules-red --verbosity quiet
dotnet test tests/Trama.UnitTests -c Release --no-restore --filter FullyQualifiedName!~SaveAgendaTaskTests --logger trx --results-directory artifacts/agenda-rules-red/baseline --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

24 casos novos descobertos e compilados. 23 falham em ExecuteAsync ainda não implementado; um passa com a guarda do construtor. As asserções específicas só serão exercidas após o GREEN. Esse RED não demonstra suporte a agenda, gravação real ou recuperação.

Os 77 casos unitários anteriores passaram nesta execução. Formatação verificada após ajustar somente a disposição de um inicializador no teste novo, antes da aprovação. Os testes anteriores permanecem intactos.

Nenhum teste anterior foi alterado. A suíte completa não foi repetida nesta fatia sem integração com o desktop. O último resultado completo continua sendo o relatório 060: 534 aprovados e um timeout em autosave, com repetição focada passando; causa pendente. Não apresentar o histórico como nova execução.

## Revisão obrigatória

A seção 9 do CODEX exige aprovação explícita antes de implementar as regras. Revisar especialmente o intervalo em pares e a possibilidade de datas passadas/reabertura. Depois da aprovação, implementar sem alterar os casos e verificar novamente; SQLite/backup/interface recebem suas próprias especificações.

Esforço baixo a moderado, um agente sem subagentes. Exploração limitada aos padrões existentes e ao planejamento da Agenda. Reutilizar esse mapa nas próximas fatias para reduzir exploração e repetição de testes não afetados. Manual/PDF/README e Git intactos.

Estudo e decisões: [capítulo 53](../estudos/53-agenda-regras-das-tarefas.md).
