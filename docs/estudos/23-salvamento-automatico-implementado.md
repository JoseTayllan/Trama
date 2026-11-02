# Salvamento automático implementado

29/10/2026. Implementação após aprovação dos oito testes. Build Release, formatação e **312 testes** passaram, com os casos aprovados intactos. A página agora é gravada automaticamente pelo aplicativo após dois segundos sem alterações, e o botão Salvar página continua disponível.

## Como usar

Edite uma página e aguarde dois segundos sem digitar. O cabeçalho informa Salvamento automático e o rodapé confirma Página salva neste computador quando a gravação termina. Escrever novamente reinicia o prazo. Salvar página permite antecipar a gravação. Abrir uma página ou deixar uma página nova intocada não gera gravação por si só.

Texto, estilos, código, linguagem, referência de imagens, legendas e ordem dos blocos seguem pelo mesmo fluxo de persistência usado pelo botão. A página mantém a identidade e aparece na lista do caderno após sucesso.

## Relógio, pausa e revisão

[NotebookEditorViewModel.AutoSave.cs](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.AutoSave.cs) concentra o agendamento. Uma edição cancela o atraso anterior e cria outro com Task.Delay e TimeProvider. O await preserva o contexto da interface: o timer não escreve diretamente nos controles a partir de uma thread de trabalho.

Quando o prazo termina, o editor só inicia a gravação se houver alterações e não existir operação ocupando o fluxo ou uma pausa de navegação. A gravação usa o método existente, que captura uma revisão e um snapshot antes de aguardar o banco. Se o usuário editar durante a gravação automática, a revisão mais recente permanece pendente. As gravações são serializadas; uma nova gravação não começa enquanto a anterior está em andamento.

CanEditContent mantém texto e formatação disponíveis durante autosave, preservando foco e digitação. Operações estruturais/importação continuam protegidas durante a gravação. Salvamento manual e outras operações ocupadas mantêm a proteção anterior. Se o snapshot salvo for anterior à edição atual, o feedback indica que ainda existem alterações pendentes.

Em falha, manter conteúdo e alterações pendentes. Não repetir indefinidamente sem nova edição; o usuário pode editar novamente ou usar Salvar página. A confirmação de fechar/navegar continua protegendo alterações não gravadas.

## Navegação e descarte

[LibraryViewModel](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs) pausa o autosave antes de aguardar a decisão Salvar/Descartar/Cancelar e durante a preparação da navegação. Assim, o timer não grava enquanto a pessoa decide descartar. Se a navegação for cancelada, o editor continua e o agendamento pode prosseguir. Se for concluída, o editor anterior é descartado e seu atraso é cancelado.

Fechamento aprovado descarta o editor antes do fechamento final da janela; isso evita uma gravação tardia após escolher Descartar. Ao mover uma página, a biblioteca prepara um novo editor com ações vinculadas ao caderno de destino; as próximas gravações automáticas usam esse destino.

## Ativação e compatibilidade

[App](../../src/Trama.Desktop/App.axaml.cs) ativa explicitamente o recurso por `DesktopComposition.CreateAsync(..., enableAutoSave: true, ...)`. A composição entrega o relógio System aos editores da biblioteca. Ela também descarta os editores quando os serviços são encerrados.

As assinaturas anteriores dos construtores e de CreateAsync foram preservadas. Chamadas antigas sem relógio ou sem a opção de autosave mantêm o comportamento manual, permitindo compatibilidade e testes determinísticos. Isso não é uma tela de preferências: o aplicativo normal ativa autosave; o caminho legado é destinado a consumidores anteriores e testes.

Não houve migração de banco ou alteração no formato de backup. O cabeçalho da [view](../../src/Trama.Desktop/Views/NotebookEditorView.axaml) usa SaveModeLabel, e os campos usam CanEditContent.

## Evidências

- Os [oito testes aprovados](../../tests/Trama.IntegrationTests/Knowledge/NotebookAutoSaveTests.cs) passaram sem alteração; suíte completa: 49 unitários e 263 de integração, zero falhas/ignorados.
- Harness Avalonia.Headless/Skia com SQLite temporário conferiu autosave de texto formatado/código/imagem, lista atualizada, nova sessão e destino correto após mover.
- O mesmo harness manteve uma decisão de navegação aberta por mais de dois segundos e comprovou que Descartar não gravou a edição, inclusive depois de trocar de editor e ao fechar.
- Uma gravação controlada em andamento manteve o campo de prosa habilitado e focado, aceitou nova digitação e depois gravou o snapshot mais recente sem modificar o anterior.
- Novo pacote Windows x64 self-contained publicado em pasta separada; janela Trama abriu respondendo e criou o banco de teste. A conferência humana desse novo pacote ainda está pendente.

## Limites

Autosave protege até a última gravação concluída. Uma queda antes dela ainda pode perder alterações; não implementamos rascunho separado, histórico de versões, sincronização ou recuperação integral após interrupção forçada. Backups continuam necessários. A conferência não cobre conflito entre duas instâncias editando o mesmo perfil. A colagem no WSL permanece em baixa prioridade conforme decisão do responsável.

## Para estudar

Por que salvar uma revisão antiga não pode limpar a revisão atual? Por que cancelar um atraso não cancela necessariamente uma gravação já iniciada? Qual problema a pausa durante a confirmação de descarte evita? Por que o modo automático precisa preservar o foco do campo de texto? Por que a reabertura em outra sessão é uma evidência mais forte que o feedback de sucesso?

Veja a [especificação aprovada](22-salvamento-automatico-especificacao.md) e o [relatório da entrega](../reviews/032-autosave-green.md).
