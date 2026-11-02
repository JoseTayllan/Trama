# Editor contínuo do caderno — histórico da especificação

Os 23 casos abaixo foram aprovados e implementados, com 113 testes passando. Este capítulo preserva o planejamento e a evidência do RED; veja [a interface implementada](11-interface-do-caderno-implementada.md) para estudar o estado atual.

## O que muda na experiência

A próxima interface será uma página de escrita: título no topo, área central ampla e rolagem do documento inteiro. O painel de apresentação deixa de ocupar a área principal. Texto cresce com o conteúdo; código aparece em bloco próprio, com linguagem, fonte monoespaçada e destaque visual; imagens aparecem entre as anotações, com legenda editável. As cores continuam baseadas nos recursos semânticos do tema.

A mudança principal de salvamento é intencional: **a página permanece aberta após salvar**. Continuar escrevendo e salvar de novo atualiza a mesma identidade, em vez de criar uma nova nota ou apagar o que está na tela.

Na revisão RED, a nova View estava vazia e o editor antigo continuava sendo exibido. Depois da aprovação, a janela passou a usar o caderno contínuo.

## Da tela aos casos de uso

```text
NotebookEditorView
  → NotebookEditorViewModel
    → INotebookActions
      → SaveNotebook / repositório / armazenamento de anexos
```

[INotebookActions](../../src/Trama.Application/Knowledge/INotebookActions.cs) é a entrada da aplicação para o editor. O ViewModel não recebe DbContext, não abre arquivos por caminhos e não instancia repositórios. O adaptador a ser composto deverá criar e descartar um contexto por operação, reaproveitando os adaptadores já implementados.

O [caso de uso SaveNotebook](../../src/Trama.Application/Knowledge/SaveNotebook.cs) será responsável por validar presença de título ou conteúdo, aguardar a persistência e distinguir sucesso, validação e falha de armazenamento. Página somente com título, somente com código ou somente com imagem é válida. Espaços em um bloco vazio não tornam uma página sem título válida.

O [ViewModel](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.cs) expõe blocos editáveis com identidade estável. Esses objetos pertencem à apresentação e notificam mudanças para os bindings. Ao salvar, devem ser convertidos para os records de aplicação, preservando ordem e conteúdo. A requisição já enviada não pode mudar se o usuário editar o objeto da interface posteriormente.

## Comportamentos especificados

- Começar uma página com um bloco de texto vazio, pronto para escrever.
- Adicionar texto e código, editar linguagem e conteúdo, mover e remover por identidade.
- Salvar título e blocos sem normalizar espaços, indentação ou quebras de linha.
- Manter página e identidade após salvar; um salvamento posterior atualiza a mesma página.
- Preservar o rascunho em falhas de validação, persistência e erros inesperados, sem expor detalhes técnicos nas mensagens.
- Bloquear salvamento duplicado e alterações estruturais enquanto salva. Durante importação, bloquear salvamento e inserção de texto/código; os campos visuais também deverão respeitar o estado ocupado.
- Importar uma imagem pelo caso de uso e adicionar sua referência somente após sucesso, sem descartar o stream de entrada que pertence ao chamador.
- Preservar texto anterior em importação inválida ou cancelada.
- Carregar título, conteúdo, linguagens e referências da página recuperada. Se ela não existe ou a leitura falha, manter o rascunho atual.
- Usar rolagem da página para conteúdo extenso, em vez de confinar a escrita num campo de altura fixa.

O teste de composição usa SQLite e PNG reais de teste, salva pelo editor, resolve outro editor e reabre os dados e os bytes da imagem. Ele também exige que o editor novo esteja registrado na composição de dependências.

## Interação visual prevista no GREEN

Uma barra compacta oferecerá Salvar e ações para adicionar texto, código e imagem. A seleção nativa de arquivo ficará na camada de apresentação; ela abrirá e descartará o stream selecionado em torno da chamada de importação. Cancelar a seleção não deve alterar a página.

O bloco de código terá linguagem editável e apresentação própria. A escolha da biblioteca e os detalhes de realce de sintaxe precisam de verificação de compatibilidade e licença antes da inclusão; não foi adicionada dependência nesta especificação. Os testes atuais protegem conteúdo e linguagem, não um motor de realce específico.

A renderização das imagens deve usar a referência gerenciada, fechar seus streams e liberar recursos visuais. Arquivo ausente ou falha de decodificação precisa de um estado visual de erro sem remover o bloco da nota. Os testes novos verificam importação, referência e recuperação dos bytes, não a decodificação nem o diálogo nativo de seleção. Esses pontos precisam de verificação visual e de testes adicionais conforme a implementação escolhida.

`LoadAsync` é uma operação programática nesta fatia. Ainda não há catálogo visual de páginas nem política de confirmação de descarte de alterações não salvas. Não abrir automaticamente outra página por cima de um rascunho; a navegação futura deverá especificar essa proteção.

## Arquivos e evidência

- [SaveNotebookTests](../../tests/Trama.UnitTests/Knowledge/SaveNotebookTests.cs): sete casos de validação, persistência e cancelamento.
- [NotebookEditorTests](../../tests/Trama.IntegrationTests/Knowledge/NotebookEditorTests.cs): 14 casos de estado e comandos, um teste headless da View e um fluxo de composição com armazenamento real.
- [Relatório RED](../reviews/011-notebook-editor-red.md): comandos, resultados, limites e decisão pendente.

Resultado da revisão RED: **113 casos executados, 90 anteriores aprovados e 23 novos falhando**, sem alteração dos testes anteriores. A compilação terminou e a formatação passou. As falhas refletiam métodos vazios, falta do estado inicial, controles ausentes e ausência do novo registro na composição. O GREEN posterior está no [relatório da implementação](../reviews/012-notebook-editor-green.md).

## O que fica para depois

O editor ainda não tem lista de páginas, autosave, execução de código, colagem de prints, arrastar arquivos, Markdown completo ou colaboração. Importar antes de salvar pode deixar anexos sem referência se o rascunho for abandonado; a política de coleta e recuperação continua pendente. Não apagar anexos automaticamente ao remover um bloco, pois o arquivo pode continuar sendo referenciado por outra página.

## Para estudar

Por que preservar a identidade após salvar? Como um snapshot evita que a edição da tela altere uma operação já enviada? Por que o seletor de arquivos pertence à apresentação, mas a cópia gerenciada pertence a um adaptador? Qual é a diferença entre testar o conteúdo de um bloco de código e testar seu realce visual?
