# Regressão — botão Semana oculto na inicialização

Data: 07/11/2026. Dois casos em RED, aguardando aprovação conforme CODEX.md §9.

Causa encontrada em App.axaml.cs: a criação manual de AgendaViewModel continua usando quatro argumentos. HasWeekView fica falso; a composição atualizada e os testes/capturas anteriores usavam seis argumentos. A inicialização real não estava coberta por aqueles testes.

Somente AgendaStartupWeekTests.cs adicionado; produto e testes antigos intactos. Os casos percorrem App, lifetime concreto, evento Opened, perfil SQLite temporário e controles da MainWindow. As duas falhas exigem explicitamente Semana visível na janela real; a inicialização chega a Pronto e o segundo fluxo grava pelo formulário antes de falhar.

Verificar o ponto de entrada real corrige a lacuna de cobertura; não basta repetir a composição isolada. A correção proposta é conectar leitor/projetor nesse ponto, preservando prompt, perfil e fechamento. Sem migração, dependência ou alteração de contratos.

Inspeção não encontrou segredo/logging novo. Apenas dados fictícios. Sem publicação, Git, perfil pessoal ou manual/PDF. Esforço baixo a moderado, sem subagentes; dois casos focados e uma regressão completa. Reutilizar a inicialização real evita manter outro harness com composição diferente. Tokens/custo exatos não medidos.

**Verificação:** duas falhas nas asserções de Semana visível, build Release sem avisos/erros e formatação aprovados no WSL. Os 1.014 anteriores passaram separadamente (167 unitários e 847 integrações), sem falhas/ignorados. Hashes confirmam src e testes anteriores intactos; a suíte incluindo a nova classe permanece em RED.

[Estudo](../estudos/93-semana-ausente-na-inicializacao.md). Evidências/diff: trama-agenda-startup-week-red.
