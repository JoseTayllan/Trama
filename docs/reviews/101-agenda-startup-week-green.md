# Inicialização real — Semana habilitada

Data: 07/11/2026. Após aprovação explícita dos dois casos de AgendaStartupWeekTests.cs, preservados por hashes.

Único arquivo de produto alterado: App.axaml.cs. Dois serviços existentes, IAgendaPeriodReader e IAgendaWeekProjector, são fornecidos à Agenda criada pelo evento Opened. Mantidos prompt específico, perfil, relógio, lixeira e fechamento coordenado. Nenhuma regra/contrato, migração ou dependência mudou.

Verificação: os dois casos aprovados passaram; suíte completa com 1.016 aprovados (167 unitários e 849 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Todos os testes e outros arquivos anteriores de src intactos. Inspeção não encontrou segredo/logging novo.

QA App/Opened/MainWindow/SQLite/Skia em perfil fictício: inicialização por SetupWithClassicDesktopLifetime, botão Semana visível/clicável e consulta por sobreposição no perfil informado. O harness não monta AgendaViewModel nem substitui a instância de App. Captura em trama-agenda-startup-week-green/qa/inicializacao-real-semana.png.

Responsável usa WSL/desenvolvimento. A janela anterior deve ser fechada após salvar e o comando de execução repetido para carregar a nova criação da Agenda. Sem reiniciar WSL, publicar exe ou trocar perfil. Aceite de uso e revisão humana do diff pendentes; nenhuma validação nativa Windows nova nesta fatia.

Esforço baixo, sem subagentes: correção localizada, dois casos focados, uma suíte completa e uma captura do ponto de entrada real. Reutilização dos serviços existentes reduz escopo e custo; futuras verificações devem incluir esse ponto de entrada para evitar repetir a lacuna. Tokens/custo exatos não medidos.

[Estudo](../estudos/94-inicializacao-semanal-corrigida.md) · [RED](100-agenda-startup-week-red.md). Evidências/diff: trama-agenda-startup-week-green.
