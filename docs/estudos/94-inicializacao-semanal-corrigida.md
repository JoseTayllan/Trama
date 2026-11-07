# 94 — Inicialização semanal corrigida

Data: 07/11/2026. Correção após aprovação dos dois casos, mantidos intactos. Responsável informou uso por WSL/desenvolvimento.

App.axaml.cs agora fornece IAgendaPeriodReader e IAgendaWeekProjector à Agenda criada no evento Opened. Esses serviços já estavam registrados em DesktopComposition no mesmo perfil. A Agenda da janela real passa a habilitar HasWeekView e o botão Semana.

O prompt específico de alterações de tarefas, a lixeira, o relógio, o perfil informado e o coordenador de fechamento foram preservados. A mudança não altera a visibilidade para disfarçar dependências ausentes; torna o fluxo semanal funcional na instância efetivamente usada pelo aplicativo.

## Por que os testes anteriores não viram a falha

Testes de composição resolviam AgendaViewModel pelo registro atualizado. O harness visual montava uma instância com leitor/projetor. App ainda tinha sua criação manual antiga. Uma integração válida isoladamente não garantia que o ponto de entrada a usasse.

AgendaStartupWeekTests agora percorre App.OnFrameworkInitializationCompleted, lifetime concreto, evento Opened e MainWindow, com --data-directory temporário. Verifica o botão e a consulta por sobreposição no perfil escolhido; o segundo caso cria uma tarefa pelo formulário antes de conferir a mesma identidade na semana. Nenhum teste anterior foi modificado.

## Verificação e retomada

Os dois casos aprovados passaram e a suíte completa teve 1.016 aprovações (167 unitários e 849 integrações), sem falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Harness separado usa SetupWithClassicDesktopLifetime com o App real, sem criar AgendaViewModel manualmente: confirmou Semana visível/clicável e conteúdo sobreposto no perfil fictício; captura no relatório.

Para usar a correção no desenvolvimento, salvar o rascunho e fechar a janela já aberta antes de executar novamente tools/run-wsl.sh run. A instância existente não recebe automaticamente alterações na criação do ViewModel. Usar o mesmo perfil de desenvolvimento anterior; não criar outro para resolver o botão ausente. Não é necessário reiniciar o WSL.

Sem migração, dependência, alteração de dados pessoais, publicação Windows, Git ou manual/PDF. Validação de uso do responsável e revisão do diff continuam pendentes.

[Regressão aprovada](93-semana-ausente-na-inicializacao.md) · [Relatório](../reviews/101-agenda-startup-week-green.md).
