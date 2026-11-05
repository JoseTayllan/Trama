# 66 — Encerramento real: especificação SQLite

**Atualização de 04/11/2026:** os 22 casos e o modelo foram aprovados e implementados, sem alterar os testes. Suíte final: 679 aprovados, com build/formatação. [Entrega GREEN](67-encerramento-sqlite-implementado.md). O texto abaixo preserva o histórico RED; integração com o fechamento ainda pendente.

Data: 04/11/2026. RED para revisão; nenhuma migração ou persistência nova implementada.

## Modelo proposto

Adicionar `LastCompletedAt` opcional em AgendaTasks por uma oitava migração, preservando as sete anteriores. Representa o último encerramento real como DateTimeOffset, incluindo offset. Os campos planejados continuam intactos e AgendaTaskSnapshot mantém seu contrato. `IAgendaTaskCompletionReader.GetLastCompletedAtAsync` oferece a leitura separada; tarefas antigas ou inexistentes retornam null, sem inventar um instante.

Editar ou reabrir uma tarefa preserva esse último registro real. Uma nova conclusão substitui o último instante; esta proposta não mantém um histórico completo de todas as conclusões e reaberturas. Ao apresentar uma tarefa reaberta, chamar esse dado de último encerramento, não de conclusão atual. Essa regra está incluída na revisão dos testes.

## Garantias do adaptador

`SqliteAgendaTaskCompletionRepository` implementará os contratos de conclusão e leitura. O chamador continua dono do TramaDbContext. Validar seleção vazia/repetida, reler os registros atuais na transação e rejeitar tarefa ausente, já concluída ou reagendada para data futura. Atualizar estado e instante de todas as selecionadas atomicamente. Falha em qualquer item desfaz o lote; nunca sobrescrever data, horários ou conteúdo planejados.

O pacote de backup permanece na versão 1. Backups anteriores são migrados na cópia restaurada, sem mudar a origem; o encerramento antigo fica null. Validar metadados reais de todo o acervo, mesmo fora do dia exibido. Um instante inválido deve impedir leitura e publicação/restauração de backup, ainda que hashes sejam válidos.

## Casos executados

Arquivo: `tests/Trama.IntegrationTests/Knowledge/AgendaCompletionStorageTests.cs`.

- Encerrar Pendentes e Em andamento, inclusive após a meia-noite, preservando planejamento e offset.
- Afetar somente IDs selecionados e manter o contexto do chamador utilizável.
- Rejeitar seleção vazia/repetida, tarefa ausente, concluída ou reagendada; preservar o lote inteiro.
- Reverter estado e instante diante de falha real SQLite, provocada por trigger em perfil fictício.
- Respeitar cancelamento prévio e validar contexto ausente.
- Não inventar instante para tarefa ausente ou conclusão legada.
- Migrar mantendo as sete migrações, tarefas e notas dos Cadernos.
- Preservar o último instante ao editar/reabrir; atualizá-lo ao concluir novamente.
- Restaurar backup novo e anterior; rejeitar instante malformado na leitura e em backups.

Resultado RED: **22 descobertos, 21 falhas e uma guarda passando**, zero ignorados. Vinte falhas chegam às operações ainda NotImplementedException; uma constata sete migrações onde a especificação exige oito. Os testes compilaram; a ausência da oitava migração é parte da funcionalidade especificada. As asserções posteriores à operação ausente ainda não foram alcançadas: não são prova de atomicidade ou recuperação prontas.

Separadamente, os **657 testes anteriores passaram** (117 unitários e 540 integrações), zero falhas/ignorados. Não apresentar a suíte completa como GREEN: os 22 novos casos estão em RED. O fixture legado usa inserção parametrizada no esquema antigo para não depender de um modelo EF que ganhará a coluna nova.

Contratos mínimos compiláveis foram adicionados sem registro na composição. Não houve mudança no modelo EF, nas migrações, no backup, na tela ou no fechamento. Aprovar os casos antes de implementar, conforme CODEX seção 9.

```bash
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter FullyQualifiedName~AgendaCompletionStorageTests --logger trx --results-directory artifacts/agenda-completion-storage-red --verbosity quiet
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaCompletionStorageTests' --logger trx --results-directory artifacts/agenda-completion-storage-baseline --verbosity quiet
```

## Depois desta fatia

Serviço com contexto por operação e integração com o diálogo terão especificação própria. O aviso precisará consultar tarefas de outros dias, permitir seleção e coordenar as decisões da Agenda/Cadernos antes de concluir qualquer lote. Exclusão com recuperação e visão semanal continuam pendentes.

Para estudar: por que validar a seleção em memória não substitui reler na transação? Por que preservar o offset e não apenas uma hora? Qual é a diferença entre último encerramento e histórico completo?

[Relatório](../reviews/073-agenda-completion-storage-red.md).
