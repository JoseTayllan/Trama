# Do formulário ao caderno

## A necessidade

O editor atual funciona como formulário: título, uma caixa de texto e salvar. Ele não representa bem uma página de estudos longa, com explicações, exemplos de código e prints intercalados. A melhoria de cores não resolve essa diferença de uso.

A direção proposta é um caderno com página contínua. O título aparece no topo como parte do documento; a escrita ocupa a área central, com rolagem na página. A barra de ações fica compacta e o painel de apresentação deixa de disputar espaço com o conteúdo.

```text
Meu caderno                         Salvar · estado
Título da página

Texto de introdução, livre para crescer...

[ Python                         Copiar ]
  def exemplo():
      return 42

[ imagem do experimento ]
Legenda opcional

Continuação da explicação...

+ Texto    + Código    + Imagem
```

Este esquema descreveu a direção da interface antes da implementação. O [capítulo 11](11-interface-do-caderno-implementada.md) mostra o caderno agora conectado à janela.

## Comportamento esperado

- Escrever conteúdo extenso sem limitar a página a uma caixa de altura fixa.
- Intercalar blocos de texto, código e imagem na ordem escolhida.
- Editar, mover e remover blocos, preservando o restante da página.
- Exibir código em superfície própria, fonte monoespaçada, seletor de linguagem e destaque de sintaxe. Destaque visual não significa executar código; execução como no Colab fica fora desta etapa.
- Importar inicialmente PNG e JPEG por seleção de arquivo, copiando-os para armazenamento gerenciado. Mostrar imagem e legenda dentro da página. Colar prints e arrastar arquivos ficam como evoluções posteriores.
- Manter as cores ligadas aos papéis semânticos do tema, incluindo os futuros papéis de sintaxe.
- Preservar as notas antigas de texto; a evolução não pode apagar ou reinterpretar silenciosamente o conteúdo existente.

## Primeira fatia: estrutura do rascunho

Os [testes de NotebookDraft](../../tests/Trama.UnitTests/Knowledge/NotebookDraftTests.cs) especificam somente o rascunho em memória. A [implementação](../../src/Trama.Application/Knowledge/NotebookDraft.cs) passou ao GREEN após aprovação, mantendo esses testes intactos. Ela ainda não está conectada ao editor.

Um bloco tem identidade própria, independente da posição. Texto preserva quebras de linha e Unicode. Código preserva espaços, indentação e linguagem. Imagem referencia um identificador de anexo gerenciado e uma legenda, sem armazenar um caminho externo fornecido pelo usuário como identidade do arquivo.

As posições de inserção vão de zero até a quantidade de blocos. O destino de uma movimentação representa o índice final. Uma atualização mantém identidade e posição. Remover uma imagem do rascunho não autoriza apagar fisicamente um arquivo: a política de anexos será definida na fatia de persistência.

Um rascunho tem conteúdo quando existe texto ou código não vazio, ou uma referência válida de imagem. Blocos contendo apenas espaços não contam como conteúdo. Isso não altera a validação do CreateNote atual: integração e salvamento do caderno exigirão testes próprios.

## Evidência e próximos passos

O [RED histórico](../reviews/007-notebook-draft-red.md) registrou 57 aprovados e 15 novos falhando nos contratos vazios. Após aprovação e implementação, **os 72 casos passaram: 29 unitários e 43 de integração**. O build não apresentou avisos e a formatação passou. Veja o [relatório GREEN](../reviews/008-notebook-draft-green.md).

Internamente, uma lista mantém a ordem dos blocos, enquanto `Blocks` oferece uma visão somente de leitura para impedir alterações que contornem os métodos do rascunho. Os blocos são records imutáveis; uma edição cria um novo valor e `Update` o substitui na mesma posição. Texto e código não passam por Trim ou normalização, preservando o material escrito.

`Insert` valida posição e identidade antes de adicionar. `Move` valida o destino antes de remover o bloco original, evitando perder conteúdo ao receber um índice inválido. A identidade permanece a mesma ao mover. `HasContent` é calculado a partir dos blocos atuais, sem um estado paralelo que possa ficar desatualizado.

Exemplo ilustrativo de uso em memória:

```csharp
var draft = new NotebookDraft();
var text = new NotebookTextBlock(Guid.NewGuid(), "Estudo de funções");
var code = new NotebookCodeBlock(Guid.NewGuid(), "def dobro(x):\n    return x * 2\n", "python");
draft.Insert(0, text);
draft.Insert(1, code);
draft.Update(code with { Code = "def dobro(x):\n    return 2 * x\n" });
```

Esses tipos pertencem a `Trama.Application.Knowledge`. O exemplo não salva uma nota nem executa o código armazenado.

1. Revisar e aprovar os testes da estrutura do caderno — realizado.
2. Implementar essa estrutura sem alterar os testes aprovados — realizado, disponível para revisão do diff.
3. Especificar salvamento e leitura de blocos, migração compatível e importação de anexos — os adaptadores foram implementados na [etapa seguinte](09-persistencia-do-caderno-e-anexos.md), com testes em arquivos e bancos temporários. A coordenação com o rascunho da interface ainda está pendente.
4. Construir a página contínua, inserção de blocos e renderização de código com sintaxe. Selecionar a dependência de edição após verificar compatibilidade e licença; ela ainda não está escolhida.
5. Validar escrita longa, rolagem, foco e teclado, destaque de código, exibição de imagens e reabertura com dados persistidos.

Os adaptadores e o editor posteriores já permitem importar e exibir imagens, escrever código com realce e salvar a página. A reabertura é testada pela composição, mas ainda não há catálogo visual ou autosave. Não confundir a estrutura de rascunho em memória com todas as entregas posteriores. O [roadmap](../../rodmap.md) registra essas etapas separadamente.

## Para estudar

Por que a identidade de um bloco não deve ser seu índice? Por que guardar somente o caminho original de um print poderia quebrar a nota? Quais informações são necessárias para salvar e reabrir um bloco de código sem perder sua formatação?
