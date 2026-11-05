# 76 — Data final planejada: especificação de persistência

Data: 05/11/2026. Testes em revisão; nenhuma implementação de armazenamento nesta fatia.

As regras aprovadas já permitem intervalos entre dias em memória. O SQLite atual ainda não grava PlannedEndDate: salvar perde a data e pode tornar ilegível um intervalo como 22h–02h ao relê-lo sem a data final. Antes dos controles visuais, precisamos fechar essa diferença entre o modelo e o banco.

## Contrato proposto

A décima migração deve adicionar PlannedEndDate como data opcional, preservando as nove anteriores, Cadernos, tarefas, exclusões e instantes reais. Tarefas antigas devem continuar sem data final explícita; não inferir um fim nem alterar o planejamento anterior.

Gravação, leitura e listagem devem preservar datas e horários opcionais, identidade e edição/limpeza da data. Conclusão, reabertura e lixeira devem manter a data planejada separada de LastCompletedAt. O filtro por período continua usando o dia inicial planejado; interseção de intervalos na visão semanal terá especificação própria.

Backup continua no formato 1. Deve preservar datas de tarefas ativas e excluídas e rejeitar valores malformados ou anteriores ao início, inclusive na lixeira. Restauração valida também pacotes com hashes corretos mas dados inválidos; backups da nona migração são migrados na cópia, sem modificar a origem. A conclusão em lote revalida as datas persistidas antes de escrever.

## Casos e execução

AgendaEndDateStorageTests contém 24 casos: 22 falhas esperadas e duas proteções passando. Falhas são perda de data no roundtrip e ausência da décima migração/coluna. Os testes que precisam alterar uma coluna existente primeiro exigem sua presença por uma asserção em PRAGMA table_info; não tratam erro SQL ou compilação como prova de RED.

Proteções que passam: data inválida não substitui tarefa anterior; ausência de data mantém a regra do mesmo dia. Outros casos cobrem cancelamento, rollback por trigger, perfis isolados, backup/restauração, legado, metadados reais e lixeira.

Build Release sem avisos/erros e formatação aprovados no WSL. Os 787 casos anteriores passaram separadamente (131 unitários e 656 integrações). Todos os arquivos anteriores de src/tests permanecem intactos por hashes. Não foi criada migração nem alterado código de implementação. Suíte com os novos casos permanece deliberadamente RED.

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --no-build --filter FullyQualifiedName~AgendaEndDateStorageTests
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaEndDateStorageTests'
```

Após aprovação: gerar migração, mapear e validar a data no armazenamento, executar os casos intactos e a suíte completa. Só então especificar campos compactos do formulário e a escolha da conclusão real no diálogo. Não expor PlannedEndDate pela interface enquanto sua persistência estiver incompleta. Confirmação/tela da lixeira e semana visual continuam pendentes.

Sem perfil pessoal, Git, publicação Windows ou manual/PDF alterados. Para estudar: por que um backup com hash correto ainda pode ser inválido? Por que não devemos inferir a data final de tarefas antigas? Como provar que uma migração preserva o histórico sem fixar a quantidade máxima de migrações?

[Relatório](../reviews/083-agenda-end-date-storage-red.md) · [Regras implementadas](75-datas-de-termino-regras-implementadas.md).
