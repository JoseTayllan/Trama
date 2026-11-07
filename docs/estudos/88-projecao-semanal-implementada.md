# 88 — Projeção semanal implementada

Data: 06/11/2026. Implementação após aprovação explícita dos 36 casos e contratos; diff para revisão.

AgendaWeekProjector implementa IAgendaWeekProjector na camada Application. Recebe a data de referência e snapshots já consultados, sem acessar banco, interface ou relógio. A tela semanal ainda não foi integrada.

## Fluxo

O deslocamento do dia da semana encontra a segunda-feira. A rotina rejeita uma semana cujo domingo ultrapassaria DateOnly.MaxValue. Copia a coleção de entrada e valida todos os snapshots e IDs antes de filtrar, reutilizando AgendaTaskValidation. Filtros de categoria e estado são opcionais e combinados com AND.

A seleção inclui tarefas com início até domingo e fim efetivo a partir de segunda-feira. O fim efetivo usa PlannedEndDate ou PlannedDate, mantendo uma tarefa aberta sem data final apenas no dia planejado. A ordenação considera início original, horário inicial (ausentes depois), título ordinal e ID, sem depender da cultura da máquina.

Cada um dos sete dias recebe entradas das tarefas que o atravessam. Cada entrada conserva o snapshot original; o início só é mostrado na data inicial e o fim só na final. As flags de continuidade comparam o dia com as datas originais, inclusive quando a tarefa começa antes ou termina depois da semana. Fim à meia-noite permanece inclusivo na data final.

A contagem usa a seleção de tarefas distintas, não a soma dos cartões. Coleções de dias/entradas são materializadas e expostas como somente leitura, sem compartilhar a coleção mutável de entrada. Complexidade: ordenação O(n log n) e distribuição por sete dias O(7n), sem percorrer todo o intervalo de tarefas longas.

## Categoria e estado na interface futura

O modelo preserva categoria e estado separadamente, incluindo tarefas de vários dias. A proposta visual registrada continua: faixa/cor para categoria; círculo e rótulo para Pendente, progresso e rótulo para Em andamento, check e rótulo para Concluída. Não há estilos, ícones ou controles novos nesta entrega. Essa próxima fatia usará a paleta dinâmica do Desktop.

## Verificação e limites

Os 36 casos aprovados passaram sem alterações. Compilação Release sem avisos/erros e formatação aprovadas no WSL. Resultado da suíte completa e hashes no [relatório](../reviews/095-agenda-week-model-green.md).

Sem migração, dependência, alteração da leitura diária, registro no Desktop ou edição dos testes anteriores. O projetor não conhece a lixeira: recebe os snapshots ativos fornecidos pelo leitor de períodos. Ainda será necessário integrar carga/navegação semanal, filtros e editor, com tratamento de falhas e preservação dos rascunhos.

Para estudar: por que a contagem de tarefas difere da contagem de entradas? Por que flags usam datas originais e não os limites da semana? Por que um filtro não deve esconder uma entrada inválida?

[Especificação aprovada](87-semana-projecao-e-estados-especificacao.md) · [Relatório](../reviews/095-agenda-week-model-green.md).
