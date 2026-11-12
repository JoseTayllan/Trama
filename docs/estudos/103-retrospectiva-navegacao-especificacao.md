# 103 — Carregar o fechamento para o painel

Data: 09/11/2026. Especificação proposta com 29 casos, antes da implementação.

O cálculo, o armazenamento, o serviço e a comparação estão implementados. Esta fatia conecta esses componentes em um modelo de apresentação dedicado: `AgendaRetrospectiveViewModel`. Ele receberá `IAgendaRetrospectiveActions`, `IAgendaRetrospectiveComparer` e `TimeProvider`. Não haverá XAML, alteração da Agenda, inicialização pelo App ou abertura automática nesta fatia.

## Fluxo proposto

`OpenAsync(DateOnly referenceDate, CancellationToken)` normaliza a referência para segunda-feira e aceita somente semanas encerradas, conforme a data local do relógio injetado. Obtém ou registra o fechamento escolhido pelo serviço existente e consulta apenas a captura imediatamente anterior com `GetAsync`. Não registra a semana anterior automaticamente nem pesquisa outra semana mais antiga para preencher uma lacuna.

O comparador recebe as duas capturas, inclusive anterior nula, e o resumo existente prepara os textos. As propriedades `Report` e `Summary` só são substituídas após concluir a sequência. `IsBusy`, `HasError` e `Feedback` comunicam carregamento e falhas para os futuros bindings. Não há acesso direto ao SQLite nem dados fictícios no produto.

`RefreshAsync(CancellationToken)` reabre a última semana carregada com sucesso. Antes de qualquer abertura, não escolhe nem cria uma semana. Uma tentativa de navegação que falha não altera a referência usada pela atualização.

## Segurança do estado

- Falha ao ler a semana anterior é erro de consulta, não ausência de histórico.
- Falhas mantêm o último relatório e resumo publicados, liberam o carregamento e permitem tentar novamente.
- Cancelamento antes da consulta não lê relógio/banco; durante a consulta preserva os dados e permite tentar novamente. O token segue para ambas as chamadas.
- Enquanto carregando, outra navegação ou atualização é recusada, evitando resultados concorrentes fora de ordem.
- Detalhes internos de exceções não aparecem no feedback.
- A menor semana representável não tenta consultar uma segunda-feira negativa.
- Instanciar o modelo não consulta nem registra fechamentos.

Uma captura pode ser gravada pelo serviço antes de uma falha na consulta anterior ou de um cancelamento posterior. O coordenador não desfaz esse registro: ele permanece preservado, e a próxima tentativa deve recuperá-lo. Não prometer cancelamento como desfazer uma transação que já foi confirmada.

## Testes e limites

Os testes usam o builder/comparador/resumo existentes, um relógio fixo e serviço controlado. Gates com cancelamento exercitam o período inteiro de carregamento, sem sleeps. A implementação será descoberta por reflexão, mantendo a solução compilável antes de existir o novo tipo. Os casos falham pela ausência dele, não por compilação.

Não há mudança em tarefas, rascunhos ou histórico de edição nesta fatia. A preservação desses estados ao abrir o painel precisa ser testada quando ele for conectado à Agenda/MainWindow; não é demonstrada apenas por testar este modelo isolado. A inicialização real do App, o fechamento coordenado e a renderização ampla/compacta terão cobertura própria antes da liberação da tela.

[Revisão](../reviews/110-retrospective-navigation-red.md) · [Comparação implementada](102-retrospectiva-comparacao-implementada.md).
