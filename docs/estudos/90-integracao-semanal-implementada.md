# 90 — Integração semanal implementada

Data: 06/11/2026. Implementação após aprovação dos 34 casos, preservados sem alterações. Diff para revisão.

AgendaViewModel.Week.cs conecta o leitor por período e o projetor ao formulário existente. A nova sobrecarga recebe essas dependências; os construtores anteriores continuam funcionando no modo diário. DesktopComposition registra o projetor e fornece o leitor do mesmo perfil SQLite. Não há segundo editor nem banco semanal.

## Como o estado é atualizado

ShowWeekAsync valida o intervalo com o projetor antes de consultar, lê as tarefas sobrepostas e publica somente o resultado completo. A coleção original dessa consulta fica em memória para reaplicar filtros sem I/O. Uma falha mantém o último resultado e ativa HasWeekError, separado de HasListError da lista diária.

Navegar ou filtrar não resolve nem apaga rascunhos e não altera o dia do formulário. ShowDayAsync retorna ao modo diário sem mudar rascunho ou filtros. CreateTaskOnDayAsync valida que o dia pertence à semana visível e reutiliza ChangeDayAsync, com Salvar/Descartar/Cancelar. A consulta do dia precisa passar antes de substituir o rascunho.

Salvar, Atualizar, enviar para lixeira e restaurar atualizam também a semana ativa. A releitura semanal é independente da diária: falhar depois de commit indica lista desatualizada, preservando o ID e o resultado da gravação. Week e os filtros notificam bindings. IsBusy é o mesmo da agenda, protegendo ações e fechamento durante a consulta.

Abrir um cartão de continuação reutiliza EditTaskAsync com o ID original: o formulário conserva a data de início e a data final reais do planejamento, sem transformá-las no dia do cartão.

## Estado visual preparado

AgendaPresentationConverter agora fornece status-icon: ○ Pendente, ◐ Em andamento, ✓ Concluída. Rótulo e categoria continuam independentes. Esta entrega disponibiliza a apresentação para os bindings; nenhum controle ou estilo da tela foi alterado. A próxima etapa precisa verificar renderização dos símbolos, contraste, teclado e janela compacta.

## Verificação e limites

Os 34 casos aprovados passaram intactos, incluindo composição com SQLite temporário e falhas/retry de leitores controlados. Build Release e formatação aprovados no WSL; suíte completa e hashes no [relatório](../reviews/097-agenda-week-navigation-green.md).

Sem migração, pacote novo, alteração dos testes anteriores, execução do perfil pessoal ou Git. Sem nova publicação Windows ou modificação de manual/PDF. A visão semanal ainda não está acessível na interface: faltam controles de navegação, filtros, cartões e ações por dia.

Para estudar: por que dois estados de erro para lista diária e semana? Por que não devolver falha de gravação quando só a releitura falhou? Como o busy compartilhado evita fechar durante a consulta?

[Especificação](89-integracao-semanal-especificacao.md) · [Relatório](../reviews/097-agenda-week-navigation-green.md).
