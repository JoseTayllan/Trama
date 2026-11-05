# Rolagem e seleção — implementação para revisão

Data: 02/11/2026. Os seis casos do relatório 058 foram aprovados pelo responsável antes da implementação e preservados.

**Aceite em 02/11/2026:** o responsável aprovou a entrega de rolagem/seleção e autorizou seguir para o refinamento visual. Esse aceite não encerra a revisão estética do Início nem implica certificação separada dos dois ambientes.

## Resultado

535 testes aprovados: 77 unitários e 458 de integração, nenhum ignorado. Build Release sem avisos/erros e `dotnet format --verify-no-changes` passaram pelo helper WSL. Seis regressões passaram intactas.

`NotebookEditorView.Scrolling.cs` encaminha a roda vertical à página antes de ser consumida pelos editores expandidos. `NotebookBlockScrolling.cs` impede que retângulos lógicos de `TextArea.ScrollToLine` sejam interpretados como posições físicas da página e usa o retângulo real do cursor. Preserva foco em blocos parcialmente visíveis e rolagem horizontal do código.

Composição real SQLite/Skia conferida em 1380×1000 e 860×680: seleção por ponteiro sem salto, roda nos blocos, eixo horizontal, histórico e autosave oculto. Perfil sintético fechado antes da cópia para o pacote Windows isolado.

Na primeira execução da base, dois casos de autosave falharam por prazo; um passou na repetição focada, e ambos passaram na suíte completa final. Sem alteração do autosave ou dos testes anteriores; causa transitória não confirmada. Não apagar esse registro com base no GREEN posterior.

## Revisão

Código concentrado em dois adaptadores de apresentação e registros nos editores. Sem migração, dependência nova, alteração de conteúdo, acesso ao perfil pessoal ou comandos Git. Sem instrumentação de diagnóstico na entrega. Todos os testes anteriores mantêm os hashes da entrega 057; o teste novo mantém o conteúdo aprovado.

Riscos restantes: conferir roda e seleção com mouse/trackpad reais nos ambientes do responsável; a verificação Skia usa entrada simulada. A fidelidade estética ao protótipo continua pendente. A captura recebida pode usar um pacote anterior à navegação global; validar no novo pacote isolado antes de comparar os cabeçalhos.

Material de estudo: [capítulo 51](../estudos/51-rolagem-e-selecao-nos-blocos.md). A revisão do diff e a validação interativa permanecem pendentes. Não atualizar manual/PDF nem publicar sobre a pasta normal automaticamente.
