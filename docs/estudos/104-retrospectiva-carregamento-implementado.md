# 104 — Carregamento do fechamento implementado

Data: 09/11/2026. Implementação após aprovação dos 29 casos.

`AgendaRetrospectiveViewModel` conecta o serviço de fechamento ao comparador e ao resumo de apresentação. Recebe as dependências no construtor, sem consultar dados ao ser criado. `OpenAsync` normaliza a referência para segunda-feira e verifica se o domingo correspondente já passou pela data local do relógio injetado.

O serviço obtém ou registra a semana escolhida; a consulta anterior usa apenas `GetAsync`, sem criar fechamentos adicionais. Ausência de registro é diferente de erro de consulta: somente a ausência real é enviada como nulo ao comparador. Na menor semana representável, não existe uma segunda-feira anterior para consultar.

## Publicação e navegação

O resultado completo fica em um registro privado com a referência escolhida, o relatório e o resumo. Esse registro é substituído uma única vez, antes das notificações de `Report` e `Summary`. Assim, bindings que reagem a qualquer uma delas já encontram os dois valores da mesma carga.

`RefreshAsync` usa a última referência carregada com sucesso. Falhar ao abrir outra semana não altera essa referência. Antes da primeira abertura, atualizar não escolhe nem cria um fechamento.

Um bloqueio com `Interlocked.CompareExchange` cobre as duas consultas e a montagem do resumo. Tentativas simultâneas retornam falso, evitando que uma carga antiga sobrescreva outra. `IsBusy` é notificado na entrada e na saída, e o bloqueio é liberado em `finally`. Na integração futura, o chamador da interface deve manter o contexto de UI para as notificações de binding.

## Falha e cancelamento

O token é conferido antes do relógio, entre consultas e antes da publicação, e propagado às duas chamadas. Cancelamento não é apresentado como erro de armazenamento. Exceções de consulta/comparação geram feedback genérico e `HasError`, sem publicar dados parciais ou expor caminhos internos. Uma nova tentativa é permitida.

Cancelar a apresentação não desfaz um fechamento que o serviço já tenha confirmado. Se a consulta anterior falhar depois do registro atual, a próxima tentativa recuperará a captura existente; o modelo não apaga nem recalcula esse fechamento.

## Integração seguinte

Esta entrega adiciona apenas o modelo de carregamento. A próxima especificação deve conectá-lo à Agenda, ao ciclo de fechamento e à inicialização real do App, preservando rascunhos e instâncias dos editores. Depois, os controles do painel terão validação ampla/compacta e aceite visual. O comportamento automático de abertura permanece pendente; não há disparo pelo construtor.

[Especificação aprovada](103-retrospectiva-navegacao-especificacao.md) · [Revisão](../reviews/111-retrospective-navigation-green.md).
