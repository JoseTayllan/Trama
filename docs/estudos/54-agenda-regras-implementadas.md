# 54 — Agenda: regras de gravação implementadas

Data: 02/11/2026. Implementação após aprovação dos 24 casos do capítulo 53; revisão do diff pendente.

## O que está implementado

`SaveAgendaTask`, na Application, valida um snapshot antes de encaminhá-lo a `IAgendaTaskRepository`. O caso de uso segue o padrão de `SaveNotebook`: validação na aplicação, escrita atrás de contrato e falhas esperadas representadas por resultado.

O fluxo é:

1. Rejeitar argumento nulo e respeitar cancelamento já solicitado.
2. Validar identidade, título, categoria, estado e intervalo.
3. Retornar ValidationFailed sem chamar o repositório se houver erro.
4. Encaminhar o mesmo snapshot e token, aguardando a gravação.
5. Retornar Saved após sucesso ou PersistenceFailed diante de AgendaPersistenceException.

Cancelamento e defeitos inesperados continuam sendo exceções. Não capturar Exception genericamente: isso esconderia erros e poderia apresentar cancelamento como resultado normal.

## Regras preservadas

- Título válido e descrição não são normalizados ou reescritos.
- Dia planejado usa DateOnly, sem correção para hoje ou conversão implícita para UTC.
- Horário ausente é válido; se preenchido, início e fim são obrigatórios e o fim deve ser posterior no mesmo dia.
- Somente categorias e estados definidos são aceitos.
- Reagendamento e conclusão/reabertura encaminham os dados recebidos, mantendo a identidade fornecida pelo consumidor.
- Não há inferência de tempo realizado nem criação de outra tarefa ao concluir.

O caso de uso não gera IDs e não consulta o estado anterior. O consumidor deverá fornecer a identidade correta ao editar. Transação, criação/atualização física, consultas e eventual controle de concorrência ainda pertencem à próxima fatia de persistência.

## Evidências e limites

Os 24 testes aprovados permanecem idênticos e passaram. Os 77 unitários anteriores também passaram: 101 aprovados, nenhum ignorado. Build Release da solução e verificação de formatação foram conferidos no WSL. A auditoria compara os hashes dos testes com a especificação aprovada.

Os testes usam repositório controlado, não SQLite. Não há migração, registro na composição, tarefas no banco, formulário ou Agenda habilitada. Não foi gerado outro executável, pois o comportamento visível não mudou.

A suíte de integração não foi repetida nesta fatia isolada. A instabilidade de prazo de autosave registrada no relatório 060 continua aberta; os 101 aprovados não significam 559 casos aprovados numa execução completa.

## Próxima etapa

Especificar consultas e armazenamento SQLite com testes próprios. Demonstrar reabertura, atualização pelo mesmo ID, ordenação por data/horário e migração preservando os cadernos. Incluir compatibilidade dos backups antigos e recuperação das tarefas antes de expor a Agenda como recurso completo.

## Para estudar

- Por que validar antes de chamar o repositório?
- Por que encaminhar o mesmo snapshot evita alterações silenciosas no texto?
- Qual a diferença entre concluir uma Task do C# e concluir uma tarefa da Agenda?
- Por que 101 testes unitários passando não demonstram que os dados sobrevivem ao fechamento do aplicativo?
