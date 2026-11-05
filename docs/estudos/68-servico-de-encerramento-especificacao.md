# 68 — Serviço e consulta de tarefas candidatas

**Atualização de 04/11/2026:** os 24 casos foram aprovados e implementados, preservados. Suíte completa com 703 aprovados; build/formatação passaram. [Entrega GREEN](69-servico-de-encerramento-implementado.md). O texto abaixo preserva o histórico RED; diálogo ao sair ainda pendente.

**Base anterior reexecutada:** 679 aprovados (117 unitários e 562 integrações), zero falhas/ignorados. Os 24 novos casos continuam em RED separado; não declarar a suíte completa GREEN.

Data: 04/11/2026. Especificação RED para revisão.

## Contrato proposto

`IAgendaCompletionActions` separa o fluxo de encerramento da edição existente em IAgendaActions. Oferece consulta de candidatas até um dia local, conclusão de seleção e leitura do último encerramento real. `SqliteAgendaCompletionActions` tem somente validação de caminho e operações vazias; não está registrado na composição. Esta fatia não altera a janela nem o banco.

O serviço guardará opções imutáveis do perfil, criando e descartando contexto por operação. Banco existente em ReadWrite e Pooling=false, sem inicializar ou migrar silenciosamente. Construção, validação e cancelamento prévio não devem criar arquivo/diretório.

## Consulta e conclusão

Candidatas são Pendentes ou Em andamento com dia planejado até o limite inclusivo. Incluir tarefas de dias anteriores, sem depender do dia selecionado na tela. Excluir datas posteriores e tarefas concluídas; não filtrar pelo último encerramento histórico de uma tarefa reaberta. Manter a ordem existente: dia, horário, tarefas sem horário depois, título e ID.

Completar reutiliza CompleteAgendaTasks e o adaptador transacional aprovado. Seleção inválida retorna ValidationFailed antes de abrir o disco. Seleção desatualizada pode resultar em PersistenceFailed após revalidação, preservando o lote. Falha de consulta propaga AgendaPersistenceException; cancelamento não vira conclusão. Último instante preserva offset e não é inventado para legados ou IDs ausentes.

## Casos executados

`AgendaCompletionServiceTests.cs` contém 24 casos: caminhos inválidos e construção sem I/O; consulta histórica/ordem; atualização externa/reabertura; conclusão e leitura após reabrir o serviço; perfis isolados com o mesmo ID; cancelamento das três operações; seleção inválida antes de disco; leitura de banco ausente; recuperação após falha; seleção desatualizada; concorrência para IDs distintos; composição no perfil correto; seleção nula e metadados legados.

Resultado: **24 descobertos, 20 falhas esperadas e quatro proteções passando**, zero ignorados. Dezenove falhas chegam a NotImplementedException e uma verifica a falta do registro em DesktopComposition. Guardas de caminho e construção passam. Build Release sem avisos/erros e formatação passaram. As verificações posteriores às operações vazias ainda não foram alcançadas; não declarar o serviço funcional.

```bash
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter FullyQualifiedName~AgendaCompletionServiceTests --logger trx --results-directory artifacts/agenda-completion-service-red --verbosity quiet
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaCompletionServiceTests' --logger trx --results-directory artifacts/agenda-completion-service-baseline --verbosity quiet
```

Os testes usam SQLite fictício e mantêm os cenários anteriores intactos. A concorrência especificada cobre tarefas distintas, não promete resolução de conflito no mesmo ID. O serviço futuro não deve guardar entidades/contextos de uma operação para outra.

## Próxima fatia

Após aprovação: implementar o serviço e registrar IAgendaCompletionActions no mesmo perfil inicializado pelo desktop. Depois especificar o diálogo com seleção e a coordenação de fechamento de Agenda e Cadernos. Cancelar uma decisão posterior não pode deixar conclusões gravadas por uma saída que não ocorreu. Não usar o último encerramento como duração de trabalho ou como conclusão atual de tarefa reaberta.

Sem migração, mudança de backup, dependência, teste antigo, perfil pessoal, Git ou publicação alterados. Revisão dos testes obrigatória conforme CODEX seção 9.

Para estudar: por que criar contexto por operação permite um serviço singleton? Por que a lista de candidatas é apenas uma fotografia? Por que não inicializar um banco quando uma consulta recebe um caminho errado?

[Relatório](../reviews/075-agenda-completion-service-red.md).
