# Biblioteca visual implementada

## Como usar

A janela agora tem a biblioteca à esquerda e a página à direita. Os assuntos são livres: um espaço pode ser Curso de C#, Trabalho ou Pesquisa; dentro dele, cada caderno reúne suas páginas.

1. Digite um nome em Espaços e clique em +. O espaço novo fica selecionado.
2. Digite o nome de um caderno e clique em +. A seleção do caderno prepara uma página vazia.
3. Escreva um título e conteúdo, ou use + Nova para iniciar outra página.
4. Clique em Salvar página. Ela permanece aberta, e a lista de páginas é atualizada.
5. Clique numa página da lista para abri-la. Na próxima sessão, selecione o espaço, o caderno e a página novamente.

Não organizadas reúne páginas sem associação a um caderno, inclusive documentos anteriores. Nesse modo também é possível escrever e salvar uma página avulsa. Se houver um espaço selecionado sem caderno, o editor fica oculto até escolher ou criar o caderno, evitando confundir o destino da escrita.

Use Atualizar para reler a biblioteca sem trocar o rascunho atual. Os campos de criação mantêm o nome digitado quando a operação falha. Nomes em branco são rejeitados; nomes válidos são gravados sem espaços nas extremidades.

Para executar a versão desta entrega, a partir da raiz:

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/library-navigation-build
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --artifacts-path artifacts/library-navigation-build --no-build
```

Uma janela aberta anteriormente não recebe as mudanças compiladas. Feche a versão anterior depois de salvar seu conteúdo, ou execute a saída isolada correspondente.

## Rascunhos e navegação

O editor marca alterações de título, texto, linguagem e estrutura dos blocos. Inserir imagem também deixa a página pendente. A indicação Não salvo aparece ao lado de Salvamento manual.

Ao navegar com alterações pendentes, a confirmação oferece Salvar e continuar, Descartar ou Cancelar. Salvar aguarda a persistência e só permite sair se não restarem alterações pendentes. Cancelar mantém o editor e restaura a seleção da lista. Descartar só substitui o editor depois que o destino estiver disponível; se abrir o destino falha, o rascunho anterior continua acessível.

Esse comportamento começou protegendo a navegação dentro da biblioteca. A proteção posterior ao fechamento normal da janela está no [capítulo 15](15-cinco-passos-e-protecao-ao-fechar.md). Não há autosave. O fechamento do diálogo de confirmação sem escolher uma opção é tratado como Cancelar.

## Revisões do editor

[NotebookEditorViewModel](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.cs) mantém dois contadores: revisão atual e revisão confirmada. Cada alteração relevante incrementa a revisão atual. A página está pendente quando os contadores diferem.

Antes de salvar, o editor captura tanto o snapshot quanto a revisão correspondente. Ao terminar, confirma somente aquela revisão. Se outra alteração chegou enquanto a operação aguardava o banco, ela continua pendente, mesmo que o snapshot anterior tenha sido salvo com sucesso.

Ao carregar uma página, a atribuição dos dados ocorre sob uma proteção de restauração: trocar os objetos e seus campos não representa uma edição do usuário. O carregamento bem-sucedido estabelece uma página limpa. Alterações antigas em blocos removidos não devem continuar marcando o editor; por isso os eventos desses blocos são removidos.

TryLoadAsync informa se a abertura ocorreu. O método LoadAsync anterior permanece disponível, preservando seus consumidores. A View reinicia a rolagem ao trocar de editor.

Essa implementação conta alterações, não o histórico de undo. Voltar manualmente ao texto anterior não recalcula equivalência com o último snapshot: pode continuar existindo uma indicação pendente até salvar ou carregar novamente.

## Quem coordena a biblioteca

[LibraryViewModel](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs) expõe coleções somente de leitura, seleção e comandos. Cada editor recebe um adaptador que captura o caderno de destino ao ser criado. O salvamento não consulta uma seleção de lista mutável para decidir onde gravar a página.

O adaptador chama ILibraryActions.SaveInNotebookAsync e, depois do sucesso, atualiza a lista visível. Se apenas essa atualização falha, o salvamento continua sendo sucesso e a biblioteca informa a necessidade de usar Atualizar. Uma lista temporariamente desatualizada não deve ser reportada como perda da gravação.

As operações de navegação são serializadas pelo estado ocupado. O editor e as listas ficam desabilitados durante as operações relevantes. A seleção é confirmada somente depois das leituras necessárias; falhas não substituem o editor atual.

[LibraryWorkspaceView](../../src/Trama.Desktop/Views/LibraryWorkspaceView.axaml) fornece os bindings. Seu code-behind adapta eventos de seleção e restaura os itens selecionados após cancelar. Atualizações programáticas para uma seleção já confirmada não disparam outra navegação, evitando reiniciar a página ao construir a View.

[UnsavedChangesDialog](../../src/Trama.Desktop/Views/UnsavedChangesDialog.axaml) mostra as três escolhas. WindowUnsavedChangesPrompt associa o diálogo à janela principal. Sem um prompt visual configurado, a composição usa Cancelar como resposta segura; testes fornecem escolhas controladas.

## Casos de uso e transação

[CreateCatalogEntry](../../src/Trama.Application/Knowledge/CreateCatalogEntry.cs) valida os nomes, cria identidades e delega a persistência. [SqliteLibraryActions](../../src/Trama.Infrastructure/Persistence/SqliteLibraryActions.cs) mantém opções e adaptadores sem guardar um DbContext vivo; cria e descarta um contexto por operação.

SaveInNotebookAsync abre uma transação, chama SaveNotebook para validar/gravar a página e, se houver destino, associa a página ao caderno. Só confirma depois das duas etapas. Um destino inválido desfaz conteúdo e localização, inclusive atualizações de páginas já existentes.

Os repositórios de página e catálogo agora identificam uma transação já aberta. Quando participam dela, não a confirmam nem a descartam. Chamados independentemente, continuam criando e confirmando suas próprias transações. Os testes anteriores desse comportamento permaneceram intactos.

Importação de imagem continua separada da transação do SQLite. Arquivos importados para um rascunho abandonado podem permanecer sem referência; remover um bloco não apaga fisicamente o anexo.

## Evidências

Os **157 testes passaram: 36 unitários e 121 de integração**, sem alterar os 31 casos aprovados nesta fatia nem os anteriores. Build Release sem avisos e formatação aprovada. O [relatório GREEN](../reviews/016-library-navigation-green.md) registra comandos e limites.

Um roteiro externo renderizou a biblioteca com SQLite e dados sintéticos isolados. Conferiu criação pelos campos e botões da View, escrita, salvamento e atualização da lista; seleções já abertas ao anexar a View; Cancelar restaurando a seleção; Salvar e continuar persistindo antes de abrir o destino; Descartar sem gravar o rascunho anterior; fechamento do diálogo mantendo a página.

Foram inspecionadas prévias em 1200 × 950, 900 × 680 e 1200 × 900, além do diálogo de confirmação. Esse roteiro não foi acrescentado à suíte do produto. Teclado, leitores de tela, escala do Windows e seleção nativa de imagens ainda precisam de conferência interativa.

## O que ainda falta

Renomear/excluir espaços e cadernos, mover páginas pela interface, autosave, confirmação no fechamento da janela, recuperação após falha do processo e coleta de anexos órfãos permanecem futuros. Não há sincronização ou suporte a edições concorrentes com resolução de conflitos. O editor continua local e manual.

## Para estudar

Por que uma confirmação de saída precisa aguardar a gravação? Qual o papel do contador de revisão quando a edição muda durante um await? Por que um repositório não deve confirmar uma transação aberta por outro componente? Como restaurar a seleção visual sem disparar outra troca de página?
