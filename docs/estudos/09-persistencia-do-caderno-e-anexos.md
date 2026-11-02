# Persistência do caderno e anexos

## Objetivo

Após aprovação dos testes, os adaptadores passaram a salvar uma página e recuperá-la com título, ordem, conteúdo e referências de imagens preservados. Esta etapa ficou GREEN com 90 testes, sem alterar os testes aprovados. A conexão posterior com a janela está explicada no [capítulo 11](11-interface-do-caderno-implementada.md), com 113 testes aprovados.

Os [testes](../../tests/Trama.IntegrationTests/Knowledge/NotebookStorageTests.cs) usam SQLite em arquivos temporários e imagens sintéticas de um pixel. Não acessam o banco ou os prints pessoais do usuário.

## Contratos propostos

[NotebookSnapshot](../../src/Trama.Application/Knowledge/NotebookSnapshot.cs) transporta identidade da página, título e blocos. `INotebookRepository.SaveAsync` salva uma página nova ou substitui o conteúdo de uma página com a mesma identidade. `GetByIdAsync` recupera o estado persistido; uma identidade ausente retorna null, enquanto uma falha de banco deve gerar `NotePersistenceException`.

O adaptador [SqliteNotebookRepository](../../src/Trama.Infrastructure/Persistence/SqliteNotebookRepository.cs) recebe um contexto emprestado, seguindo o repositório existente. Quem o utiliza controla o ciclo de vida do contexto. Não compartilhar esse contexto entre operações concorrentes nem manter contexto com erro para reutilização no desktop.

Os testes especificam comportamento observável, sem fixar os nomes das novas tabelas. A migração `20261027234457_NotebookPages` adiciona `NotebookPages` e `NotebookBlocks`, preservando `InitialNotes` e a tabela `Notes`. A página registra a versão do formato; os blocos registram posição, tipo, conteúdo, linguagem e referência opcional de imagem. Um índice único impede duas posições iguais na mesma página.

O repositório valida e prepara os blocos antes de escrever. Dentro de uma transação, atualiza o título e remove os blocos anteriores; depois insere o novo conjunto e confirma. Há duas chamadas de SaveChanges para evitar conflitos entre as posições antigas e as novas, mas um único commit. Em falhas, o descarte da transação desfaz as escritas; o chamador deve descartar o contexto com erro.

## Compatibilidade com notas existentes

A migração deve preservar exatamente título e corpo das notas antigas. O leitor de caderno deve apresentar uma nota antiga com corpo como um bloco de texto, sem interpretar Markdown, JSON ou cercas de código como um formato de armazenamento.

Ler uma nota antiga não a regrava nem normaliza espaços. O teste também consulta a nota pelo leitor anterior após a adaptação, conferindo que o conteúdo persistido continua igual. O bloco de texto legado usa a identidade da própria nota, mantendo sua identidade estável entre leituras. Essa escolha não tem uma asserção específica nos testes aprovados.

O leitor procura primeiro a página estruturada; se ela não existe, adapta a nota antiga. Salvar a mesma identidade no novo repositório cria uma página estruturada, preservando o registro antigo como estava. O leitor antigo continua vendo somente Notes, enquanto o leitor novo dá preferência à página estruturada. Essa transição de edição ainda não está na interface e exige teste próprio antes de ser exposta; não misturar consumidores antigos e novos como se fossem sincronizados.

Salvar uma página deve preservar as demais. Após substituir título e blocos de uma página existente, a reabertura não deve recuperar blocos removidos. Cancelamento anterior à escrita e falha de gravação por banco somente leitura devem preservar a versão anterior.

Esses casos não comprovam rollback de uma falha artificial no meio de uma transação. Escrita composta deve usar transação; antes de conectar a interface, acrescentar cobertura para os riscos concretos da implementação escolhida.

## Imagens gerenciadas

[IImageAttachmentStore](../../src/Trama.Application/Knowledge/IImageAttachmentStore.cs) recebe um stream e devolve identidade do anexo e tipo de mídia. Quem chama continua dono do stream de entrada. `OpenReadAsync` abre uma nova leitura por identidade; quem recebe deve descartar o stream devolvido. Anexo inexistente retorna null.

A primeira versão copia PNG e JPEG para `attachments/` dentro do diretório absoluto recebido pelo armazenamento. O limite aprovado é **10 MiB por arquivo**. Sem nome de arquivo ou caminho externo no contrato de importação, a referência da página fica desacoplada do arquivo original. O destino usa um GUID gerado pelo armazenamento, não um caminho vindo da nota.

A cópia ocorre em arquivo temporário na mesma pasta do destino. O adaptador conta os bytes recebidos, reconhece o cabeçalho, conclui e fecha o arquivo e só então o renomeia para a identidade final. O bloco finally remove o temporário que a operação criou se houver erro; o stream de origem permanece sob responsabilidade do chamador. Abrir um anexo usa sua identidade e devolve um novo stream de leitura.

Os testes exigem que bytes importados sejam recuperados por uma nova instância do armazenamento, mesmo depois de descartar a origem. Material que não seja imagem reconhecida, cabeçalho truncado e arquivo acima do limite devem ser rejeitados com `InvalidDataException`. Cancelamento prévio e falha de leitura não podem deixar arquivos incompletos publicados ou temporários abandonados. Uma falha de leitura deve continuar sendo reportada como erro de entrada e saída.

Há limites nessa cobertura: reconhecer assinaturas não comprova decodificação integral de imagem, limites de dimensões, remoção de metadados ou segurança de todo arquivo arbitrário. A decodificação e a apresentação terão verificação própria ao conectar o componente visual. Não foi escolhida nem adicionada biblioteca de imagem nesta fase.

## Evidência de execução

O [RED histórico](../reviews/009-notebook-storage-red.md) registrou 72 aprovados e 18 falhas esperadas nos contratos vazios e na migração ausente. Após a implementação, **os 90 casos passaram: 29 unitários e 61 de integração**, sem alterações nos testes. Build Release com zero avisos e zero erros; formatação aprovada.

O [relatório GREEN](../reviews/010-notebook-storage-green.md) registra comandos e limites. A migração só foi aplicada em bancos temporários nesta verificação; o aplicativo aplicará as migrações conhecidas na próxima inicialização pelo fluxo já existente. Não houve abertura nem alteração do banco pessoal nesta entrega.

## Etapas seguintes

1. Revisar esta especificação e os 18 casos antes da implementação — realizado.
2. Implementar migração, repositório e importação sem alterar os testes aprovados — realizado, disponível para revisão do diff.
3. Especificar a coordenação entre gravação da página e importação: falha no banco, anexos órfãos, cancelamento durante cópia e recuperação. Não há transação única entre SQLite e sistema de arquivos.
4. Conectar a página contínua, seleção de arquivo, imagens e realce de sintaxe, com testes próprios de comandos e estados do editor.

A importação e a gravação são contratos separados. Esta fase não inclui remoção física de anexos, sincronização, backup, execução de código, autosave, seleção de arquivos ou abertura visual de páginas.

## Perguntas de estudo

Por que falha de banco não pode virar null? Como reabrir uma imagem se o arquivo original foi removido? Por que não guardar um documento estruturado dentro do corpo de uma nota antiga sem um marcador de formato explícito? Como recuperar um arquivo importado quando a gravação da página falha?
