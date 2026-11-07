# 93 — Semana ausente na inicialização real

Data: 07/11/2026. Regressão em RED; correção aguardando aprovação dos dois casos.

A captura do responsável mostrou o modo Dia, mas nenhum botão Semana. A View nova já estava carregada. A condição HasWeekView era falsa porque App.axaml.cs ainda criava AgendaViewModel pelo construtor de quatro argumentos, sem IAgendaPeriodReader e IAgendaWeekProjector. DesktopComposition já fornecia essas dependências; o caminho de inicialização duplicado não havia sido atualizado.

Os testes e capturas anteriores confirmaram a composição e os controles isolados, mas não percorreram esse ponto de entrada. A falha ficou na ligação entre App e o ViewModel, sem relação com migração, dados ou largura da janela.

## Regressões propostas

AgendaStartupWeekTests chama App.OnFrameworkInitializationCompleted com ClassicDesktopStyleApplicationLifetime concreto, passa --data-directory para um perfil temporário e abre a MainWindow produzida por App. Não monta AgendaViewModel manualmente e não resolve a instância da composição no lugar da inicialização real.

1. Abrir Agenda, exigir Semana visível e acessível, consultar uma tarefa que começou antes do dia selecionado e conferir que vem do perfil informado.
2. Criar tarefa pelo formulário nativo após iniciar, abrir Semana e exigir a mesma tarefa/identidade, com formulário limpo e lixeira disponível.

Ambos falham na asserção que exige Semana visível, reproduzindo a captura. O aplicativo chega a Pronto e a gravação diária funciona antes da segunda falha; não são erros de compilação ou infraestrutura. Testes anteriores são verificados separadamente.

## Correção prevista após aprovação

Fornecer leitor e projetor também à Agenda criada por App, preservando o prompt específico das tarefas, perfil, lixeira, relógio e fechamento coordenado. Não mudar a condição da View para expor um botão sem dependências funcionais. Depois executar os casos intactos e a suíte completa e conferir a janela criada pelo ponto de entrada real.

Sem código do produto alterado nesta etapa. [Relatório](../reviews/100-agenda-startup-week-red.md).
