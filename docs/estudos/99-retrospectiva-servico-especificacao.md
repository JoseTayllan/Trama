# 99 — Serviço de fechamento: especificação

Data: 09/11/2026. 34 casos em RED para revisão antes da implementação.

As regras e a persistência estão implementadas. A próxima fatia propõe IAgendaRetrospectiveActions e SqliteAgendaRetrospectiveActions, recebendo caminho absoluto do banco e TimeProvider pelo construtor. Cada operação deve criar/descartar seu próprio contexto, abrir somente perfil existente e nunca inicializar silenciosamente um banco vazio.

## Operações propostas

| Operação | Comportamento |
| --- | --- |
| GetOrCloseAsync(referenceDate) | Normalizar a semana; devolver captura existente ou criar uma para semana já encerrada |
| GetAsync(weekStart) | Ler captura validada pela segunda-feira, sem criar relatório ou consultar tarefas atuais |
| ListAsync(from, through) | Ler histórico com datas iniciais inclusivas, mais recente primeiro, sem preencher semanas ausentes |

A captura existente tem prioridade sobre o relógio e os dados vivos. Não deve ser recalculada porque uma tarefa mudou ou porque o relógio/fuso atual mudou. Uma captura corrompida gera erro; não é substituída automaticamente. Entradas de calendário inválidas e cancelamento prévio são rejeitados antes de abrir banco ou consultar relógio.

Na criação, o relógio local injetado determina se o domingo já acabou, o instante ClosedAt e o fuso. Semana aberta é erro de argumento. Dados armazenados inválidos e falhas de banco são AgendaPersistenceException; cancelamento continua OperationCanceledException. Não inventar datas de conclusão nem modificar estado/planejamento das tarefas.

## Coordenação e concorrência

Consultar existência, ler planejamento/data real, calcular e inserir precisam participar de uma transação coerente. Chamadas simultâneas da mesma semana devem retornar o mesmo relatório vencedor, inclusive ClosedAt e conteúdo. Semanas diferentes mantêm ambas as capturas. Contextos não são compartilhados entre chamadas.

O teste transacional observa o comando de inserção antes de sua execução, pausa a captura e tenta editar uma tarefa por outra conexão. A edição deve esperar a transação terminar; a captura conserva a versão que leu e a tarefa pode mudar depois do commit. A observação usa eventos de diagnóstico do EF restritos ao banco temporário; não exige uma API de instrumentação no produto.

Cancelamento no limite da inserção e falha de gravação não deixam fechamento parcial. A mesma instância do serviço deve recuperar-se em nova chamada, sem entidades rastreadas ou transações antigas retidas. Perfis separados permanecem isolados.

## Composição e limites

DesktopComposition deve registrar IAgendaRetrospectiveActions para o mesmo perfil das demais funções. O teste de composição lê um relatório previamente salvo sem depender da data/fuso real da máquina. Nenhuma migração ou contrato antigo precisa mudar.

Esta fatia não fecha tarefas, move pendências, gera todos os relatórios atrasados, calcula comparação ou mostra o painel. GetOrClose só atua na semana solicitada. A política de abertura automática da retrospectiva e a integração visual virão depois, com testes do ponto de entrada real para evitar repetir a falha anterior de inicialização.

34 casos: 33 falhas esperadas pelo serviço/registro ausentes e uma proteção diária passando. Resultados finais, regressão e hashes no [relatório](../reviews/106-retrospective-service-red.md). Somente contrato e testes novos; nenhuma implementação antes da aprovação.
