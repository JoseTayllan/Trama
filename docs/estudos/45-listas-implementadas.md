# 45 · Listas implementadas

31/10/2026. Os 32 casos do [capítulo 44](44-listas-especificacao.md) e a versão 4 foram aprovados explicitamente antes da implementação. Os testes aprovados foram preservados. Estado: implementação disponível para revisão do diff e validação de uso.

## Como as partes se conectam

`ParagraphListDocument`, na Application, representa a lista de cada parágrafo por posição UTF-16 e tipo `bullet` ou `numbered`. Valida tipos, posições e duplicatas; aplica a seleção aos parágrafos e recalcula posições durante substituições. Os marcadores não são inseridos no texto original. Isso mantém pesquisa, seleção e intervalos de estilos independentes da apresentação.

`NotebookTextBlock.Lists` é aditivo; o construtor e os estilos anteriores continuam disponíveis. `NotebookDraft` valida essa estrutura antes de aceitar o bloco. No desktop, `NotebookBlockEditor` mantém os metadados e `NotebookEditorViewModel` inclui listas nos snapshots e nas edições, sem recriar os controles.

O seletor da barra compartilhada oferece **Sem lista**, **Marcadores** e **Numerada**. A seleção de texto afeta os parágrafos abrangidos; sem seleção, afeta o parágrafo do cursor. A barra continua recolhível e o seletor é desabilitado quando o foco muda para código.

`NotebookProseEditor` inclui listas nas operações de histórico já usadas pelos estilos. A edição nativa e seus metadados viajam juntos no mesmo grupo. Salvar e autosave não limpam essa pilha; reabrir uma página inicia outro histórico, conforme o comportamento anterior. Enter em um item vazio remove a lista daquele parágrafo, sem outra quebra de linha. Nas demais inserções de quebra, o modelo continua o tipo da lista.

## Renderização

`NotebookProseEditor.Lists.cs` desenha marcadores na margem interna do editor, usando as linhas visuais reais do AvaloniaEdit. A numeração reinicia ao encontrar um parágrafo comum ou um item com marcador. Linhas quebradas do mesmo parágrafo continuam alinhadas ao conteúdo. A largura reserva espaço para tamanho maior e quantidade de itens; o símbolo acompanha a linha de base do primeiro trecho visual, sem modificar offsets ou texto.

A margem existe somente em blocos que contêm listas. Sua cor usa o Foreground do editor, mantendo a ligação com o tema. Não foram acrescentadas cores fixas ou pacotes de dependência.

## Persistência e compatibilidade

`SqliteNotebookRepository` reutiliza a coluna `Styles` da tabela `NotebookTextStyles` como metadados por bloco. Para páginas sem listas, mantém exatamente a representação anterior: array de estilos. Blocos com listas usam um envelope com `Styles` e `Lists`, armazenado na mesma transação do conteúdo.

| Conteúdo | Versão da página |
| --- | ---: |
| Sem atributos de formatação | 1 |
| Somente negrito/itálico | 2 |
| Tamanho/cor/marca-texto explícitos | 3 |
| Qualquer lista | 4 |

Remover todas as listas volta à versão correspondente aos atributos restantes. A leitura valida metadados e a correspondência entre conteúdo e versão; não aceita listas disfarçadas como versão 1. Aplicativos antigos recusam versão 4 pelo limite de leitura já existente; não devem ser usados para editar páginas com listas.

Não há migração nova: permanecem as seis anteriores. O formato do pacote de backup continua 1, pois seu contêiner não mudou. A validação do backup reutiliza o leitor da página, inclusive na lixeira; os testes cobrem restauração com estilos, tags e imagem.

## Evidências e limites

Suíte completa no WSL: **486 aprovados**, nenhum ignorado, incluindo os 32 novos e os 454 anteriores. Build/formatação e detalhes de execução ficam no [relatório 053](../reviews/053-paragraph-lists-green.md).

Houve um timeout no novo teste de autosave na primeira execução completa. O caso passou isoladamente e na suíte seguinte, sem alterar seu limite ou suas asserções. A causa não foi determinada; registrar a ocorrência permite acompanhar recorrências, em vez de apagar uma execução malsucedida.

Capturas da interface real com SQLite/Skia, em 1380 e 860 unidades lógicas, foram usadas para conferir quebra de linha, barra recolhível e marcadores. O perfil é fictício. A conferência não substitui aprovação estética, testes interativos do pacote Windows ou auditoria de acessibilidade/DPI.

Checklist, sublistas, escolha do número inicial e estilos alfabéticos continuam fora desta fatia. Exportação ainda é a próxima proposta para fechar a primeira versão, conforme o [roadmap](../../rodmap.md). Manual/PDF não foram regenerados.

## Para estudar

1. Compare um item de lista com um intervalo de estilo: por que a posição inicial do parágrafo basta para um e não para o outro?
2. Acompanhe uma quebra de linha desde o documento nativo até a atualização dos metadados e a pilha de desfazer.
3. Examine por que a versão do conteúdo mudou, mas o esquema físico e a versão do pacote de backup não precisaram mudar.
