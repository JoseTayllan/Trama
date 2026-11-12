# Retrospectiva — Agenda/App integrados

Data: 09/11/2026. Implementação autorizada pelos 22 casos aprovados.

Novo AgendaViewModel.Retrospective.cs, duas saídas de modo em AgendaViewModel.Week.cs e conexão em DesktopComposition/App. Construtores anteriores preservados; novo carregador por instância e comparador sem estado. Sem alteração de testes, regra de captura, migração, dependência ou XAML.

Revisão: IsBusy cobre ambas as consultas e montagem do resumo, libera em finally e reutiliza proteções de comandos/navegação/fechamento. A mudança de modo ocorre somente após sucesso; o retorno à semana não desativa o painel antes da consulta concluir. Não há resolução ou gravação dos rascunhos durante a navegação. O App usa o serviço do perfil escolhido e mantém o prompt da janela. Nenhum segredo literal ou logging novo.

Limites: esta entrega valida a integração dos modelos e o bootstrap real, sem novo botão, gráfico ou QA estético. A UI futura deve passar por ShowRetrospectiveAsync/RefreshRetrospectiveAsync da Agenda. Cancelamento não desfaz captura confirmada no serviço. A política de abertura automática permanece pendente.

Esforço moderado, sem subagentes: rodada focada e suíte completa, reutilizando bloqueios e serviços existentes. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF; revisão humana do diff pendente.

[Estudo](../estudos/106-retrospectiva-integrada-a-agenda.md). Evidências: trama-retrospective-host-green.

Verificação final: 22 casos aprovados intactos por hashes. Suíte completa com 1.199 aprovados (199 unitários e 1.000 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Testes anteriores intactos; somente os três arquivos de produção previstos foram alterados, além do novo partial. Inclui App/Opened/MainWindow, perfil correto, rascunhos e bloqueio durante carga.
