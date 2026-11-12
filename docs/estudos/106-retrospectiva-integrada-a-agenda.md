# 106 — Retrospectiva integrada à Agenda

Data: 09/11/2026. Implementação após aprovação dos 22 casos.

AgendaViewModel recebe uma nova sobrecarga com AgendaRetrospectiveViewModel, preservando os construtores anteriores. `Retrospective` expõe o carregador; `HasRetrospectiveView` indica se ele existe e `IsRetrospectiveView` representa o novo modo. A composição registra comparador sem estado e carregador por instância, com o serviço do perfil selecionado e TimeProvider.System.

O caminho manual de inicialização em App também fornece o carregador. Isso é necessário porque a janela real constrói a Agenda com seu próprio prompt, enquanto testes e outros consumidores podem usar DesktopComposition diretamente. Ambos devem habilitar os mesmos recursos.

## Fronteira das operações

ShowRetrospectiveAsync e RefreshRetrospectiveAsync passam pelo mesmo método privado. Ele recusa operações quando IsBusy já está ativo, mantém esse estado durante toda a consulta e o libera em finally. Assim, os comandos existentes da Agenda, a navegação entre módulos e o coordenador de saída enxergam a operação e reaproveitam suas proteções.

Somente após o carregador retornar sucesso, a Agenda desativa o modo Semana e ativa a Retrospectiva. Em falha ou cancelamento, os modos ficam como estavam, o feedback vem do carregador e o último relatório permanece disponível. ShowDayAsync desativa o novo modo; ShowWeekAsync só o desativa depois de carregar a semana com sucesso.

Os métodos não resolvem alterações, salvam tarefas, trocam o dia selecionado ou recriam editores. Rascunhos da Agenda, inclusive horário parcialmente digitado, e a instância do editor de Cadernos são preservados. A semana em cache e a lista diária também continuam disponíveis.

## Verificação e limites

Os testes incluem a sequência completa com SQLite: capturar uma semana, editar a tarefa atual e atualizar a retrospectiva, mantendo os dados e o instante do fechamento. Dois casos iniciam App/Opened/MainWindow em perfil temporário e verificam a disponibilidade, a ausência de captura automática e a gravação no perfil correto com os dois rascunhos preservados.

A integração não adiciona controles visuais nem abre retrospectiva automaticamente. A próxima fatia deve ligar os botões aos métodos protegidos da Agenda, apresentar o fechamento e verificar bindings, erros e layout amplo/compacto. Ligar uma ação diretamente ao carregador contornaria o bloqueio compartilhado do módulo e não é a rota prevista.

[Especificação aprovada](105-retrospectiva-integracao-especificacao.md) · [Revisão](../reviews/113-retrospective-host-green.md).
