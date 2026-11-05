# 74 — Datas de término: especificação em revisão

Data: 05/11/2026. Somente testes e contratos opcionais; comportamento ainda não implementado.

O responsável confirmou duas necessidades diferentes. No planejamento, uma tarefa pode começar em 05/11 às 22h e terminar em 06/11 às 02h. Na execução, deve ser possível informar quando ela realmente terminou, preservando o planejamento. LastCompletedAt já armazena data e hora, mas o diálogo atual usa o instante da confirmação.

## Regras propostas

- PlannedEndDate opcional no snapshot. Sem essa data, o intervalo continua no mesmo dia; não inferir amanhã a partir de uma hora menor.
- Data final anterior à inicial é inválida. No mesmo dia, fim deve ser posterior ao início. Em dia posterior, horas menores são permitidas.
- Datas sem horários e início sem fim continuam permitidos. Hora final continua exigindo hora inicial.
- CompletedAt opcional na decisão de conclusão. Quando informado, guardar esse instante e seu offset; sem valor, preservar o horário automático atual.
- Rejeitar conclusão real futura ou anterior ao dia inicial planejado, sem concluir tarefas. A ação de manter abertas ignora esse campo.

Esta primeira fatia verifica as regras e o protocolo entre prompt e coordenador. Não adiciona campos visuais, coluna SQLite ou suporte persistido a PlannedEndDate. Não usar os contratos novos pela interface antes das fatias de persistência e apresentação. O atributo JSON omite a nova data nula para preservar a representação legada; construtores posicionais permanecem intactos.

## Evidências e limites

AgendaCrossDayRulesTests tem 14 casos: cinco falhas esperadas e nove proteções passando. AgendaManualCompletionTests tem quatro: três falhas esperadas e uma proteção passando. As falhas mostram comparação apenas de horas, ausência de validação da data planejada e coordenador ignorando o instante informado. Não são falhas de compilação.

Build Release passou sem avisos/erros, formatação passou e os 769 testes anteriores passaram separadamente (117 unitários e 652 integrações). Todos os testes anteriores permaneceram intactos nesta fatia. A suíte com os novos casos está deliberadamente RED: 18 novos, oito falhas e dez aprovados. A persistência da lixeira foi verificada antes, mas sua confirmação/tela ainda falta.

Próximas fatias: aprovação destes casos; regras e encaminhamento da conclusão; testes de migração/SQLite/backup da data planejada; campos compactos de planejamento e conclusão real; confirmação/editor/lixeira; semana visual. Consulta semanal de tarefas que atravessam dias terá especificação própria.

Nenhum perfil pessoal, publicação Windows, Git ou manual/PDF foi alterado. Para estudar: por que data planejada e instante real precisam de campos distintos? Como a ausência de uma data preserva compatibilidade? Por que um roundtrip JSON não prova persistência SQLite?

[Relatório](../reviews/081-agenda-cross-day-red.md) · [Lixeira persistida](73-lixeira-da-agenda-persistida.md).
