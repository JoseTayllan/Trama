# Desfazer/refazer de texto e estilos

29/10/2026. Implementação após aprovação dos treze casos. Os treze testes passaram intactos; a suíte completa passou **325 casos**, com build Release e formatação aprovados. O manual de uso, seu PDF e o gerador não foram alterados.

## Uso e recorte

No bloco de prosa em foco, Ctrl+Z desfaz; Ctrl+Y ou Ctrl+Shift+Z refaz. Digitação e negrito/itálico seguem a ordem das ações. Excluir ou substituir texto formatado e desfazer recupera letras e intervalos de estilos.

Salvar manualmente ou pelo autosave mantém o histórico da página aberta. A alteração restaurada passa a ser uma nova revisão do conteúdo e pode ser gravada pelo fluxo normal. Uma nova edição depois de desfazer abandona o ramo anterior de refazer.

O histórico fica separado por bloco e por sessão de edição. Carregar outra página inicia uma pilha limpa, mesmo que o texto seja igual. Título e código mantêm os históricos nativos de seus controles. Histórico de adicionar/remover/reordenar blocos e versionamento persistente de páginas ficam para etapas próprias.

## Por que usar a pilha nativa

[NotebookProseEditor](../../src/Trama.Desktop/Views/NotebookProseEditor.cs) já usava TextEditor do AvaloniaEdit, cuja pilha registra modificações textuais, agrupamento de digitação e seleção. Criar uma segunda pilha para estilos separaria a ordem cronológica: desfazer poderia retirar texto quando a última ação tinha sido aplicar negrito.

A implementação acrescenta operações IUndoableOperation à pilha existente. Ao formatar, registra cópias dos intervalos anteriores e posteriores, sem duplicar o texto. Durante uma edição textual, registra os metadados como operação opcional dentro do mesmo grupo da operação nativa de texto. Assim, texto e estilos viajam juntos no histórico.

## Restaurar sem criar outra edição normal

Em undo/redo de texto, o AvaloniaEdit pode adiar TextChanged até terminar todas as operações agrupadas. A operação de estilos prepara o estado esperado; quando o texto chega ao estado final, o editor aplica os intervalos preparados, sem recalculá-los pela regra normal de herança e sem acrescentar outra operação à pilha.

Isso também importa quando um grupo remove e insere texto e termina com a mesma string, mas com estilos diferentes: comparar somente a string não basta. O editor precisa consumir os metadados de replay mesmo nesse caso.

No histórico de formatação pura, a operação restaura diretamente os intervalos do bloco e a seleção original. As notificações normais de NotebookBlockEditor continuam chegando ao ViewModel, marcando uma revisão para persistência/autosave. A flag de sincronização impede ciclos de atualização da view.

## Isolamento e limites

Ao trocar a referência do bloco vinculado, limpar a pilha e qualquer metadado pendente. Cada operação verifica se ainda está vinculada ao bloco correto. Os atalhos respeitam CanEditContent, mantendo a proteção durante operações ocupadas.

A pilha da prosa foi limitada a 200 grupos nativos para conter o crescimento do histórico. Esse limite é por grupos, não por bytes nem necessariamente por caracteres. Páginas extensas e agrupamentos grandes ainda precisam de avaliação de memória; não há promessa de orçamento fixo de RAM.

Não houve migração de banco, alteração em backup ou nova dependência de biblioteca. O histórico é transitório; o conteúdo resultante é persistido normalmente.

## Evidências

- [Treze testes aprovados](../../tests/Trama.IntegrationTests/Knowledge/ProseUndoRedoTests.cs): estilos, texto, ordem, atalhos, salvamento, autosave, nova ramificação e isolamento.
- Suíte completa: 49 unitários e 276 de integração, zero falhas/ignorados; build sem avisos/erros e dotnet format verify aprovados.
- Harness Headless/Skia com SQLite real: atalhos, restauração de seleção, autosave de estilos desfeitos/refeitos, exclusão, grupo com texto final igual e estilos diferentes, reabertura em outra sessão e isolamento ao trocar página passaram.
- Pacote Windows x64 self-contained gerado separadamente para revisão. A conferência humana do novo pacote continua pendente.

O [relatório GREEN](../reviews/034-prose-undo-green.md) reúne mudanças e limites; a [especificação aprovada](24-desfazer-refazer-especificacao.md) preserva os requisitos anteriores.

## Para estudar

Por que o histórico nativo precisa agrupar texto e estilos? Por que recalcular os estilos ao inserir novamente um texto apagado é diferente de restaurá-los? Por que ações de replay não devem gerar novas entradas de histórico? Como uma operação opcional participa do grupo de digitação? Por que salvar não encerra a sessão de edição, mas reabrir a página inicia outra pilha?
