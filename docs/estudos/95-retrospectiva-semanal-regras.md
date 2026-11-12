# 95 — Retrospectiva semanal: regras de fechamento

Data: 07/11/2026. Especificação RED; implementação aguardando revisão dos 32 casos.

O responsável confirmou guardar o fechamento, sem recalculá-lo por edições posteriores, e contar tarefas de vários dias uma única vez, com planejamento no início e conclusão na data real. Tarefas concluídas sem data registrada devem ser identificadas. A referência visual é [retrospectiva-semanal-v1.png](../prototipos/retrospectiva-semanal-v1.png); seus dados são fictícios.

## Primeira fatia: cálculo sem persistência ou interface

AgendaRetrospectiveTask combina o snapshot existente e LastCompletedAt opcional. IAgendaRetrospectiveBuilder propõe receber data de referência, coleção, instante de captura e fuso explícito; retorna AgendaWeekRetrospective. Os contratos mínimos compilam, mas AgendaRetrospectiveBuilder ainda não existe. Nenhuma regra funcional foi implementada antes da aprovação.

O relatório identifica segunda/domingo, instante real de captura, fuso, tarefas capturadas, indicadores, sete dias e todas as categorias. Só uma semana já encerrada no fuso informado pode ser fechada: a partir da segunda-feira seguinte à meia-noite. Não completa, reagenda ou move tarefas automaticamente.

## Regras propostas para os números

| Indicador | Regra |
| --- | --- |
| Planejadas no período | IDs distintos com planejamento sobreposto à semana, incluindo continuidades anteriores |
| Iniciadas antes | Subconjunto cujo início precede segunda-feira; separado das barras dos sete dias |
| Planejadas por dia | Uma ocorrência na data inicial original, sem repetir em cada dia atravessado |
| Concluídas na semana | Estado Concluída e data real de LastCompletedAt dentro de segunda–domingo, convertida ao fuso do relatório |
| Concluídas sem data | Estado Concluída e LastCompletedAt ausente; sem presumir data planejada ou horário de fim |
| Concluídas fora do período | Estado Concluída com data real conhecida antes/depois da semana |
| Abertas | Pendentes e Em andamento, separadamente; registro antigo de conclusão de tarefa reaberta não vira nova conclusão |
| Percentual | Concluídas com data na semana / total planejado sobreposto × 100, arredondado para uma casa; sem tarefas retorna nulo |
| Categorias | Mesma população e critério de conclusão, sem duplicar tarefas |

Somar as barras planejadas e as iniciadas antes deve produzir o total planejado. Somar concluídas no período, concluídas sem data, concluídas fora e os dois estados abertos deve explicar toda a população. As barras concluídas usam somente datas reais dentro da semana. Tarefa cujo planejamento está totalmente fora dessa semana fica fora desta população, mesmo que tenha conclusão no período: o relatório não deve misturar esse conjunto com uma contagem geral de toda atividade.

Percentual mede conclusões datadas dentro do período, não a proporção de todos os estados Concluída atuais. A interface precisará explicitar isso e destacar dados sem data ou fora do período para não sugerir desempenho inexistente. Não calcular horas trabalhadas: não existe registro completo de início real.

## Captura e limites históricos

O resultado materializado deve sobreviver à troca/limpeza da coleção de entrada. A persistência futura guardará o fechamento e não o sobrescreverá silenciosamente. Esta fatia só protege o cálculo/cópia em memória; ainda não garante armazenamento histórico em disco.

A primeira captura usa os dados disponíveis no momento do registro. LastCompletedAt não é um histórico de transições: não reconstrói edições/reaberturas anteriores à captura nem versões anteriores do planejamento. Conclusão após o domingo, mas antes da captura, fica fora do período. Timestamp posterior à própria captura é rejeitado. Não apresentar essa captura tardia como um registro exato feito no domingo.

Entradas nulas, snapshots inválidos, descrições nulas e IDs duplicados são rejeitados antes do recorte; um filtro não deve esconder dados inválidos. Semana incompleta fora do limite de DateOnly é rejeitada.

## Próximas fatias

Após aprovação e implementação das regras: especificar leitura consistente de tarefa/data real e persistência atômica do fechamento, migração e backup compatível; depois comparação com semana anterior, serviço de fechamento e abertura do painel. Por fim integrar a tela, reflexões e revisão de pendências sem movimentação automática. Disparo automático e regras de releitura precisam de testes próprios; esta entrega não os antecipa.

[Relatório](../reviews/102-retrospective-rules-red.md). Para estudar: por que as barras diárias podem somar menos que o total quando há continuidades? Por que Concluída sem data precisa de um contador próprio? Por que fechamento salvo não substitui histórico de eventos?
