# Imagens e formatação: implementação em validação

29 de outubro de 2026. Passo 5 implementado após aprovação dos 28 testes e da preferência por **negrito e itálico na seleção**. A rodada final no Ubuntu/WSL passou 298 testes, build, formatação e harness. O bloqueio da DLL Windows permanece; ver o [ambiente WSL](../operacao/wsl/README.md).

## Colar prints e arrastar imagens

A página oferece **Colar print**, também por **Ctrl+Shift+V**, para importar imagem da área de transferência. Imagens entram como novos blocos ao final da página, sem substituir texto, título ou identidade. Usam o armazenamento gerenciado existente: PNG/JPEG validados pelo conteúdo, até 10 MiB por imagem, com legendas editáveis e salvamento manual.

O adaptador do sistema entrega um stream de imagem ao editor; quem o recebe deve descartá-lo após a cópia, inclusive em erro. Se a área de transferência não contiver imagem, devolver NoImage sem mudar a página, permitindo preservar a colagem normal de texto. Falhas de leitura/importação não apagam rascunhos nem expõem detalhes internos.

Arrastar um conjunto de arquivos importa as imagens válidas na ordem recebida. Cada entrada abre um stream sob demanda. Um item inválido ou ilegível não impede os seguintes; os válidos permanecem na página e o feedback informa importados/rejeitados. Não interpretar texto, URLs ou HTML arrastados como código a executar ou como autorização para buscar arquivos externos.

Durante salvamento/importação, rejeitar outra operação antes de abrir fontes adicionais. Enquanto a leitura do clipboard estiver pendente, bloquear importação/salvamento concorrente; voltar a habilitar ao terminar. O lote não é atômico: aceita os itens válidos, sem desfazer os anteriores quando outro falha. Anexos importados continuam sem coleta automática caso o rascunho seja abandonado, conforme o comportamento existente.

Ctrl+V continua com texto; Ctrl+Shift+V importa imagens. O adaptador converte bitmap em PNG limitado a 10 MiB. Bitmaps do próprio processo permanecem pertencendo ao clipboard e não são descartados; bitmaps externos são descartados após conversão. O harness Avalonia.Headless passou duas colagens consecutivas da mesma imagem. A integração com o clipboard Windows nativo continua pendente.

## Negrito e itálico na seleção

Selecionar um trecho do bloco de texto e aplicar negrito ou itálico deve mudar sua apresentação sem inserir `**`, `_`, HTML ou outros marcadores no conteúdo. Código e legenda de imagem não recebem esses estilos nesta entrega. Seleção vazia não aplica estilo.

Use os botões **N** e **I** no próprio bloco, ou **Ctrl+B** e **Ctrl+I**. Os botões não roubam o foco da seleção. NotebookProseEditor usa o AvaloniaEdit já instalado com quebra de linha, sem números de linha e com altura ajustada ao documento. Seu StyleKey precisa apontar para TextEditor: sem isso o tema não criava a área editável/foco, problema identificado e corrigido durante o harness visual.

Os estilos podem coexistir: a interseção entre trechos em negrito e itálico mantém ambos. Aplicar o botão novamente quando toda a seleção já possui aquele estilo o remove somente nesse trecho, mantendo outros estilos. Se a seleção tem mistura daquele estilo, aplicá-lo cobre toda a seleção. Intervalos adjacentes com o mesmo estilo são unidos.

O modelo usa offsets/comprimentos UTF-16, como os controles .NET. O texto original, quebras de linha e Unicode permanecem intactos. Intervalos precisam estar dentro do texto e não podem se sobrepor na representação canônica: uma interseção é representada por um segmento com flags Bold | Italic, entre segmentos simples. Valores inválidos devem ser rejeitados antes de alterar o documento ou persistência.

Inserir antes de um intervalo desloca seu início. Inserir dentro de um trecho formatado herda esse estilo; substituir uma seleção herda o estilo no início da seleção. Apagar conteúdo encurta/remove intervalos e une sobreviventes equivalentes. Na borda inicial, a inserção herda o estilo à direita; no fim do documento herda o estilo anterior. Os testes cobrem inserção antes, dentro e substituição no começo do trecho. O harness foi alinhado a essa regra sem alterar os testes aprovados.

Modificar apenas a formatação já torna a página não salva. Salvar/reabrir deve preservar os intervalos; uma edição subsequente deve mantê-los vinculados ao conteúdo certo, sem modificar o bloco de código. A seleção visual, os botões e o realce precisam de conferência do controle de escrita real, não somente dos DTOs.

## Arquitetura e persistência

- [InlineTextDocument](../../src/Trama.Application/Knowledge/InlineTextDocument.cs): modelo de texto/intervalos, cópia defensiva, validação, composição/remoção e deslocamento de estilos.
- [NotebookTextBlock](../../src/Trama.Application/Knowledge/NotebookDraft.cs): preserva o construtor posicional existente e acrescenta Styles vazio por padrão. Os blocos anteriores continuam válidos.
- [Coordenação de escrita](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.Writing.cs): importação, descarte de streams, lote parcial, concorrência, seleção e edição; sem acesso direto a arquivos/SQL.
- [NotebookBlockEditor](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.cs): notifica mudanças nos estilos e inclui esses metadados no snapshot salvo.
- [NotebookProseEditor](../../src/Trama.Desktop/Views/NotebookProseEditor.cs): renderização por intervalo, seleção e sincronização de edições com o ViewModel.
- [Adaptador de clipboard](../../src/Trama.Desktop/Views/AvaloniaClipboardImageSource.cs): acesso de plataforma e conversão do bitmap para stream limitado.
- [NotebookTextStyleRecord](../../src/Trama.Infrastructure/Persistence/NotebookTextStyleRecord.cs): persistência separada por PageId/BlockId, com FK de exclusão em cascata ao bloco.

O renderer usa DocumentColorizingTransformer para atribuir Typeface aos trechos; o conteúdo permanece texto puro. Não foi adicionada biblioteca ou cor literal às Views. A altura inicial acompanha as linhas e depois é ajustada à altura medida do TextView, inclusive com quebra de linha. A suíte anterior de escrita longa voltou a passar após esse ajuste.

A [migração nova](../../src/Trama.Infrastructure/Persistence/Migrations/20261029201153_InlineTextStyles.cs) cria NotebookTextStyles, mantendo as quatro anteriores. Uma coluna na tabela antiga foi descartada antes da entrega porque quebrava os testes que ainda escrevem páginas simples em esquemas anteriores. A tabela separada só é consultada para páginas formatadas. Seu JSON guarda Offset/Length/Style numérico; conteúdo simples mantém versão 1 e páginas formatadas usam versão 2. Ambos os formatos são aceitos na leitura/escrita. Intervalos inválidos são rejeitados antes de gravar; dados malformados na leitura geram falha de persistência, sem descarte silencioso de estilo.

A restauração passou a aceitar um prefixo completo conhecido de migrações, aplicar as atualizações **somente ao staging** e publicar depois. A origem fica intacta. O caso de backup anterior com quatro migrações passou novamente na rodada final de persistência. Esquemas desconhecidos/futuros e estilos inválidos continuam rejeitados. O pacote permanece no formato 1 e a busca continua usando texto puro.

## Testes e resultados

- [InlineTextDocumentTests](../../tests/Trama.UnitTests/Knowledge/InlineTextDocumentTests.cs): 13 casos de aplicação/remoção, composição, união, edição, validação e proteção contra mutação da lista original.
- [WritingWorkflowTests](../../tests/Trama.IntegrationTests/Knowledge/WritingWorkflowTests.cs): dez casos de clipboard/drop, streams, falhas, concorrência, seleção, preservação de código e dirty tracking.
- [RichTextStorageTests](../../tests/Trama.IntegrationTests/Knowledge/RichTextStorageTests.cs): cinco casos de reabertura, migração, backup com estilos, atualização do backup anterior e rejeição de intervalos inválidos sem sobrescrever conteúdo.

Há 298 casos: 49 unitários e 249 integrações aprovados na rodada final Linux/WSL. Build/formatação passaram. O harness Skia/Headless passou formatação, edição, colagem de texto e duas de bitmap, drop, reabertura, backup/restauração e texto extenso. A janela real iniciou pelo WSLg. Isso não substitui validação interativa Windows. Ver [relatório atual](../reviews/027-wsl-development-green.md), [bloqueio histórico](../reviews/026-writing-implementation-review.md) e [RED](../reviews/025-writing-red.md).

Botões, clipboard, drop, estilos e migração estão conectados. A conferência em Avalonia.Headless utiliza input/clipboard/eventos de drop do ambiente de teste, sem controlar aplicativos Windows pessoais. Clipboard/drag do Windows nativo e escala ainda requerem conferência interativa. Desfazer/refazer integrado de estilos, HTML/RTF e estilo de digitação sem seleção não fazem parte desta entrega. O histórico nativo de texto do controle não deve ser apresentado como histórico completo da formatação.

Fora deste recorte: fontes/cores arbitrárias por trecho, tabelas, HTML/RTF, links enriquecidos, listas complexas e formatação do bloco de código. A base por intervalos deve permitir evolução posterior sem misturar apresentação com conteúdo puro.

## Perguntas de estudo

1. Por que guardar estilos por intervalos em vez de modificar o texto com marcadores?
2. Como representar a sobreposição de negrito e itálico sem intervalos sobrepostos?
3. Quem deve descartar o stream recebido do clipboard e por quê?
4. Por que atualizar o banco restaurado em staging preserva a origem?
5. O que uma suíte de ViewModels não consegue demonstrar sobre seleção e atalhos reais?
