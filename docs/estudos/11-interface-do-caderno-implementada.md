# Como funciona a interface do caderno

## O que já pode ser usado

A janela agora apresenta uma página contínua com título, texto, blocos de código e imagens com legenda. O painel de apresentação foi removido para liberar a área de escrita. A rolagem pertence à página; blocos de texto crescem conforme o conteúdo. Salvar mantém a página aberta e reutiliza sua identidade.

Na raiz do projeto, compile e execute:

```powershell
dotnet build Trama.slnx --configuration Release
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --no-build
```

Se estiver usando a saída isolada desta entrega, os comandos equivalentes são:

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/notebook-editor-build
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --artifacts-path artifacts/notebook-editor-build --no-build
```

Feche a versão anterior ou mantenha saídas distintas para evitar bloqueio do executável durante a compilação. Uma janela já aberta não recebe automaticamente as mudanças compiladas.

## Experimentando a página

1. Escreva o título e use o primeiro bloco para o texto.
2. Use + Código para inserir um exemplo. O campo acima do código indica sua linguagem: `csharp`, `python`, `javascript`, `json` ou `sql`; outras entradas ficam em texto simples. O padrão de um bloco novo é csharp.
3. Use + Imagem e selecione PNG/JPEG de até 10 MiB. O arquivo é copiado para o armazenamento local; uma legenda pode ser escrita abaixo da imagem.
4. Use ↑ e ↓ para reorganizar um bloco e × para removê-lo da página. Isso não exclui fisicamente o arquivo de uma imagem.
5. Clique em Salvar página e aguarde a confirmação. Continue editando e salve novamente quando necessário.

Na entrega descrita neste capítulo, a recuperação por identidade já funcionava, mas ainda não havia lista visual. A biblioteca com seleção e abertura foi implementada depois e está no [capítulo 14](14-biblioteca-visual-implementada.md). Autosave continua pendente.

## Caminho do salvamento

[NotebookEditorView](../../src/Trama.Desktop/Views/NotebookEditorView.axaml) contém os bindings e a página com ScrollViewer. [NotebookEditorViewModel](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.cs) mantém objetos editáveis com notificação de propriedades. Ao salvar, converte esses objetos para um NotebookSnapshot com records independentes; editar a tela depois não modifica a requisição anterior.

O port [INotebookActions](../../src/Trama.Application/Knowledge/INotebookActions.cs) evita que o ViewModel conheça SQL ou caminhos. [SqliteNotebookActions](../../src/Trama.Infrastructure/Persistence/SqliteNotebookActions.cs) abre um contexto por operação. [SaveNotebook](../../src/Trama.Application/Knowledge/SaveNotebook.cs) valida título ou conteúdo e aguarda a persistência antes de devolver sucesso.

Enquanto uma operação está em andamento, IsBusy desabilita os campos e os comandos relevantes. Falhas de validação, persistência, importação e abertura mantêm o rascunho. Mensagens da interface não incluem o texto bruto de exceções.

O editor antigo continua registrado na composição para preservar seus consumidores e testes. A janela passou a usar a propriedade Notebook de MainWindowViewModel; o código antigo não foi removido para forçar a migração.

## Código com sintaxe e paletas

[NotebookCodeEditor](../../src/Trama.Desktop/Views/NotebookCodeEditor.cs) adapta AvaloniaEdit 11.3.0. A dependência é fixada no gerenciamento central e nos locks. A licença é [MIT](https://github.com/AvaloniaUI/AvaloniaEdit/blob/11.3.0/LICENSE), e a integração segue o [projeto oficial](https://github.com/AvaloniaUI/AvaloniaEdit/tree/11.3.0).

CodeText conecta o documento do componente ao campo editável da página. Uma proteção contra atualizações recursivas mantém o binding sem substituir o documento durante cada tecla. A altura acompanha a quantidade de linhas, permitindo que a página role em vez de criar uma segunda rolagem vertical para o código.

As definições de linguagem vêm dos recursos embarcados do pacote, sem buscar gramáticas na internet e sem carregar XAML do usuário. O controle cria sua própria definição e adapta as cores aos papéis SyntaxKeyword, SyntaxString, SyntaxNumber e SyntaxComment da paleta do Trama. Alterar as cores dos pincéis atualiza o realce. As assinaturas dos eventos são removidas quando o controle sai da árvore visual.

Esses blocos armazenam e apresentam código; não executam Python, C# ou outra linguagem. Linguagens não reconhecidas continuam editáveis sem realce. O editor de paletas para o usuário ainda não foi implementado.

## Imagens e recursos visuais

O code-behind da View trata o diálogo nativo e o stream de origem. A cópia continua no adaptador de armazenamento. Cancelar o seletor não altera a página. O stream selecionado é descartado pelo chamador depois da importação.

[NotebookImagePreview](../../src/Trama.Desktop/Views/NotebookImagePreview.axaml.cs) abre a referência gerenciada por meio do port de aplicação e decodifica uma prévia de até 1200 pixels de largura em segundo plano. Fecha o stream, substitui e descarta o bitmap anterior e cancela carregamentos ao sair da tela. Imagem ausente ou inválida apresenta mensagem e mantém a referência e a legenda na página.

Blocos de código e imagem usam templates diferidos, evitando criar um editor de código para cada bloco de texto. Isso não equivale a virtualização completa do caderno; documentos muito grandes ainda precisam de avaliação de desempenho.

## Evidências e limites

Os **113 testes passaram**, sem alterar os aprovados: 36 unitários e 77 de integração. Build sem avisos, formatação e restauração com locks passaram. O teste headless exige que texto extenso aumente a área rolável da página; outro teste usa composição real para salvar e reabrir texto, código e bytes de imagem.

Uma prévia externa com dados sintéticos verificou edição de código chegando ao ViewModel, cinco definições de linguagem, decodificação e apresentação de imagem, rolagem, cores de sintaxe alteradas em runtime, comandos dos blocos e mensagens de imagem ausente/corrompida. Foram inspecionadas janelas de 1200 × 1000, 900 × 680 e uma página rolada de 900 × 800. Esse roteiro não foi adicionado à suíte do produto e não substitui teste interativo do diálogo nativo, teclado e escala do Windows.

O [relatório GREEN](../reviews/012-notebook-editor-green.md) contém comandos e riscos. Ainda faltam catálogo visual de páginas, proteção ao descartar rascunhos na navegação, autosave, coleta de anexos órfãos e limites de dimensões de imagens. O tamanho de 10 MiB e o reconhecimento de cabeçalho não limitam sozinhos o custo de decodificação de toda imagem possível.

## Para estudar

Por que os objetos editáveis da apresentação são diferentes dos records enviados à persistência? Como o snapshot protege uma gravação em andamento? Por que o diálogo de arquivo pertence à apresentação? O que precisa ser descartado ao remover a prévia de uma imagem? Como uma cor semântica pode mudar o realce sem alterar a definição de linguagem original do pacote?
