# 51 — Rolagem e seleção nos blocos

Data: 02/11/2026.

## Problema

O ponteiro sobre um bloco impedia a rolagem vertical para baixo. Selecionar texto abaixo do início podia devolver a página ao topo. A causa envolve a composição de editores com rolagem própria dentro de uma página contínua.

O editor de prosa e o de código crescem com o conteúdo. Por isso, a rolagem vertical deve pertencer à página. O código mantém rolagem horizontal para linhas longas.

## Correção

`NotebookEditorView.Scrolling.cs` recebe a roda na fase de captura, antes do editor interno. Sem modificadores, uma intenção predominantemente vertical move a página em 50 unidades lógicas por unidade do evento. Eventos horizontais e com modificadores continuam no fluxo nativo.

`NotebookBlockScrolling.cs` adapta solicitações de visibilidade emitidas por `TextArea`. Na versão usada, `ScrollToLine` emite retângulos com números de linha. O contêiner externo físico não pode interpretar esses valores como pixels. A adaptação usa a geometria real do cursor em `TextView`, considerando o deslocamento interno. Focar um bloco parcialmente visível não exige revelar toda a sua altura.

Os dois editores registram essa adaptação no construtor. Fora de `NotebookEditorView`, o comportamento original continua. A correção não substitui documento, editor, seleção, histórico nem comandos de gravação.

Referência do comportamento adaptado: [TextArea.ScrollToLine na versão 11.4.1](https://github.com/AvaloniaUI/AvaloniaEdit/blob/11.4.1/src/AvaloniaEdit/Editing/TextArea.cs#L621).

## Evidência e limites

Os seis casos aprovados em `NotebookScrollingRegressionTests.cs` permanecem intactos. Antes da implementação, quatro falhavam e dois protegiam o sentido de rolagem já funcional. Depois, os seis passaram. A suíte completa terminou com 535 aprovados, sem ignorados; build sem avisos/erros e formatação passaram no WSL.

Uma demonstração externa com SQLite e Skia conferiu seleção por arraste, rolagem vertical sobre prosa/código, rolagem horizontal do código, histórico, autosave com Início visível e renderização ampla/compacta. Usou somente perfil fictício. A execução inicial teve falhas de prazo em testes anteriores de autosave; a execução completa final passou sem mudanças no mecanismo nem nos testes anteriores. A causa transitória não foi demonstrada.

Ainda é necessária validação interativa pelo responsável. Esta correção não representa refinamento visual final da tela Início. Ícones, composição dos cadernos e apresentação de backup continuam na revisão de fidelidade ao protótipo. Não inserir informações fictícias para imitar a imagem conceitual.

## Para estudar

- Por que dois contêineres de rolagem podem disputar o mesmo evento?
- Qual a diferença entre número de linha e coordenada em pixels?
- Por que bloquear toda solicitação de visibilidade prejudicaria navegação pelo teclado?
- Como corrigir a apresentação sem trocar o documento e perder o histórico?
