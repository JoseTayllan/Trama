# Agenda — formulário compacto

Data: 04/11/2026. Para revisão do visual e do diff.

Um arquivo de produto alterado: AgendaWorkspaceView.axaml. Campos de 32 px, data/horários em linha com quebra por largura, mês abreviado, menos espaçamento e caixa decorativa removida. Controles, comandos e bindings preservados; Salvar permanece fixo.

Build Release sem avisos/erros e formatação passaram. **43 casos afetados passaram**, zero falhas/ignorados, sem mudar testes. Suíte completa de 657 é histórica, não repetida aqui. QA SQLite/Skia ampla/compacta passou: foco, popups, valores preservados, cadastro, limpeza e edição. Capturas inspecionadas.

Limites: seletores menores precisam de aceite no uso, sobretudo em dispositivos de toque; placeholders vazios continuam nativos em inglês. As partes internas do tema Avalonia ainda exigem revisão ao atualizar dependências. Sem verificação nativa Windows ou nova publicação nesta fatia.

Sem migração, alteração funcional, teste novo mantido, logs, chamadas externas ou segredo literal no diff. Relato de título duplicado retirado pelo responsável; nenhuma correção artificial aplicada. Perfil pessoal, Git, README e manual/PDF preservados.

Esforço baixo, sem novos subagentes: uma rodada afetada e QA dirigido. Reutilizar os cenários existentes mantém a verificação proporcional ao refino. Aceite visual e revisão humana do diff pendentes.

[Estudo](../estudos/65-formulario-compacto-da-agenda.md).
