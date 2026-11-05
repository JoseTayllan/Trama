# 65 — Formulário compacto da Agenda

Data: 04/11/2026. Refino visual para revisão.

`AgendaWorkspaceView.axaml` reduz os seletores de data/horário de 44 para 32 px, usa fonte de 12 px e mês abreviado. Data e horários compartilham uma linha quando há largura; WrapPanel coloca o par de horários abaixo da data em janela menor. Os horários permanecem agrupados para evitar separar início e fim em linhas diferentes.

A caixa decorativa do planejamento foi removida. Cabeçalho, espaçamentos, título e descrição foram compactados; a descrição continua multilinha. Salvar permanece fixo fora da rolagem. Recursos semânticos, controles nativos, bindings e handlers foram preservados. Os placeholders nativos vazios ainda estão em inglês.

Verificação: build Release sem avisos/erros, formatação e **43 casos afetados aprovados** (AgendaFormTests, AgendaNavigationTests e AgendaOpenEndedTaskTests). Nenhum teste foi alterado. A suíte completa de 657 pertence à entrega anterior e não foi repetida nesta mudança visual.

QA SQLite/Skia em perfil fictício conferiu 1380×1000 e 860×680, linha de data/horários na janela ampla, foco e abertura dos seletores, preservação de valores, gravação com fim aberto, limpeza e edição. Capturas inspecionadas. Sem novo pacote ou execução nativa Windows.

O relato de bloqueio por título repetido foi retirado pelo responsável; investigação descartada, sem mudança funcional ou teste novo mantido. Não houve migração, mudança de dependência, acesso ao perfil pessoal, publicação, Git ou atualização de manual/PDF.

Próxima fatia funcional: SQLite/migração/backup do encerramento real, seguida pelo diálogo ao sair. Essa funcionalidade continua pendente; compactar o formulário não altera o fechamento.

Para estudar: por que agrupar os dois horários como um único item do WrapPanel? Qual é a diferença entre reduzir a altura de um campo e mudar a área rolável? Por que uma alteração visual pode ser verificada com os testes funcionais existentes e capturas reais?

[Relatório](../reviews/072-agenda-form-compact.md).
