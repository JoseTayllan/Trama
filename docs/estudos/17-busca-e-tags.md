# Busca e tags: implementação e estudo

28 de outubro de 2026. Passo 3 implementado após aprovação dos 30 testes. A correção posterior de tags foi aprovada com seis regressões adicionais; a suíte atual tem 270 casos passando, incluindo backup/restauração. Diff disponível para revisão.

## Como usar

Acima do editor, digite o trecho e clique em **Buscar** ou pressione Enter. O campo ao lado aceita uma tag exata opcional. Sem texto nem filtro, são listadas todas as páginas. Clique em um resultado para abrir; **Fechar busca** recolhe a lista e mantém a página atual. Os resultados são uma fotografia da consulta: execute Buscar novamente após mudanças para atualizar a lista.

Na página salva, clique em **Tags**, escreva uma tag por linha e use **Salvar tags**. Linhas vazias são ignoradas pelo diálogo; texto totalmente vazio remove todas as tags. Fechar o diálogo ou cancelar mantém a lista anterior. O campo exibe as tags normalizadas ao reabrir. O botão só fica disponível depois do primeiro salvamento da página.

Uma busca global na biblioteca encontra páginas **salvas** por trecho do título, texto, código, legenda de imagem ou nome das tags. Ignora diferenças entre maiúsculas/minúsculas e acentos: `VARIAVEIS` encontra `Variáveis`. Espaços nas extremidades da consulta são removidos; o restante é um trecho literal, incluindo pontuação como `%`, `_` e aspas. Não há interpretação de SQL, regex ou operadores especiais.

Ao relatar falha de caixa nos dois campos, o responsável ajudou a reproduzir uma lacuna: o filtro isolado já normalizava caixa, mas o campo geral não pesquisava nomes das tags. Preencher os dois com a tag também falhava porque o texto era um critério obrigatório sem correspondência. Após aprovação dos [seis casos de regressão](../../tests/Trama.IntegrationTests/Knowledge/TagCaseRegressionTests.cs), o campo geral passou a incluir tags. `so_tag` salvo encontra `SO_TAG` pesquisado e vice-versa, pelo campo grande, pelo filtro ou pelos dois juntos. O filtro continua exato; um texto diferente e uma tag continuam combinando com E.

Consulta vazia lista todas as páginas, e texto sem correspondência retorna uma lista vazia. Cada página aparece uma vez, mesmo com correspondências em vários blocos. Os resultados incluem título e localização (espaço/caderno); páginas não organizadas também aparecem, inclusive notas antigas. Se uma página estruturada e uma nota antiga compartilham ID, somente a versão estruturada participa da busca.

A busca não abandona o editor nem salva o rascunho. Abrir um resultado resolve alterações pendentes com Salvar/Descartar/Cancelar. A localização deve ser consultada novamente ao abrir: um resultado antigo pode indicar um caderno de onde a página já foi movida. A página aberta deve continuar salvando no destino real.

## Tags da página

Tags são metadados de uma página salva. É possível definir ou substituir a lista inteira e remover todas com uma lista vazia. A normalização usa trim, Unicode NFC e minúsculas invariantes; elimina duplicatas e devolve em ordem ordinal. ` C# ` e `c#` representam a mesma tag. Preserva acentos nas tags: `revisão` e `revisao` são diferentes, ao contrário da busca textual, que ignora acentos.

Uma tag em branco torna a substituição inválida e mantém toda a lista anterior. Um ID de página inexistente deve ser rejeitado, sem criar metadados órfãos. A atualização deve ser atômica, sem remoção parcial em falha/cancelamento. Notas antigas também podem receber tags sem reescrever seu corpo.

O filtro usa **uma tag exata**, sem correspondência por prefixo: `c#` não seleciona `c#-avançado`. Texto e tag combinam com E: ambos precisam corresponder. Tag de filtro vazia equivale a nenhum filtro.

Editar tags de uma página salva não modifica nem grava seu rascunho textual e não exige descartá-lo. Páginas novas precisam do primeiro salvamento antes de receber tags. Renomear, mover ou editar o conteúdo de uma página mantém suas tags, pois a associação usa o ID.

## Contratos e arquitetura

- [IKnowledgeDiscovery](../../src/Trama.Application/Knowledge/IKnowledgeDiscovery.cs): port com busca, leitura e substituição de tags; PageSearchResult contém identificação e localização.
- [PageTags](../../src/Trama.Application/Knowledge/PageTags.cs): normalização compartilhada pela interface de aplicação e pelo adaptador.
- [SqliteKnowledgeDiscovery](../../src/Trama.Infrastructure/Persistence/SqliteKnowledgeDiscovery.cs): busca no acervo salvo, leitura e substituição transacional de tags, contexto por operação e erros de persistência traduzidos.
- [LibraryViewModel.Discovery](../../src/Trama.Desktop/ViewModels/LibraryViewModel.Discovery.cs): busca, abertura segura, estado dos resultados e tags. A classe parcial separa o assunto sem criar outro estado da biblioteca; o port continua opcional para os consumidores anteriores.
- [Composição](../../src/Trama.Desktop/DesktopComposition.cs): registra o adaptador de busca/tags.
- [Biblioteca visual](../../src/Trama.Desktop/Views/LibraryWorkspaceView.axaml) e [diálogo de tags](../../src/Trama.Desktop/Views/PageTagsDialog.axaml): campos de consulta/filtro, resultados com localização e metadados da página.
- [Migração PageTags](../../src/Trama.Infrastructure/Persistence/Migrations/20261029011618_PageTags.cs): tabela com chave PageId/Name e índice por Name; as três migrações anteriores permanecem intactas.
- [Testes de SQLite](../../tests/Trama.IntegrationTests/Knowledge/SearchTagsStorageTests.cs): 19 casos, bancos temporários reais e leitura por adaptadores novos.
- [Testes do fluxo](../../tests/Trama.IntegrationTests/Knowledge/SearchTagsWorkflowTests.cs): 11 casos, ViewModel real e ações controladas para falha, espera e confirmação do rascunho.

O novo port não amplia ILibraryActions nem obriga os consumidores antigos a implementar busca. O adaptador usa contexto por operação, mensagens seguras e bloqueio visual de operações concorrentes. A associação aceita IDs de Notes e NotebookPages: por isso não possui FK exclusiva para uma das tabelas. A existência da página é verificada antes da leitura/escrita de tags. Uma futura exclusão de páginas deve também remover os metadados associados.

Na primeira versão, uma transação de leitura carrega páginas, blocos textuais, notas antigas e localização em memória; os arquivos de imagem não são abertos pela busca. O texto é decomposto para comparação sem acentos e sem distinção de caixa, mantendo o conteúdo original no banco. O filtro de tag utiliza o nome normalizado exato. Os resultados são ordenados por título/ID. Não existe índice textual duplicado para sincronizar ou mecanismo FTS.

Essa implementação consulta e percorre o acervo textual inteiro a cada busca; não há paginação nem benchmark de grandes acervos. A abertura de um resultado percorre o catálogo para localizar o ID atual em vez de confiar no caminho antigo exibido. Essa escolha privilegia simplicidade e consistência no MVP local; crescimento do acervo pode exigir FTS/paginação e uma consulta direta de localização, com especificação própria.

Ao substituir tags, normalizar todas antes de escrever. Em uma transação, validar o ID, remover o conjunto anterior e inserir o novo; confirmar somente ao terminar. Exceções/cancelamento antes do commit descartam a transação. O ViewModel prepara a lista normalizada antes da escrita e a publica depois do sucesso, sem recarga posterior que possa falhar e esconder uma escrita confirmada.

A busca mantém o editor e seu rascunho. A abertura de resultado usa a confirmação existente e prepara a página, catálogo e tags antes de publicar a seleção. As tags são carregadas ao abrir páginas pela biblioteca ou pelos resultados e são limpas da apresentação ao criar outra página. Editar tags mantém o mesmo editor, inclusive quando seu texto ainda não está salvo.

## Evidências e limites

São 230 casos aprovados: 36 unitários e 194 integrações, incluindo os 30 testes desta etapa. Build sem avisos e formatação aprovados. O [relatório RED](../reviews/021-search-tags-red.md) preserva a especificação inicial; o [relatório GREEN](../reviews/022-search-tags-green.md) registra execução, revisão e conferência externa dos controles com banco sintético, cliques simulados pelo Avalonia.Headless e reabertura real do SQLite.

Não inclui OCR, busca semântica, IA, conteúdo ainda não salvo, busca em anexos binários, pesquisa em agenda/finanças/cofre futuros, múltiplas tags simultâneas, renomeação global de tags ou ranking de relevância. Escala de grandes acervos e falhas de disco em todos os pontos da transação exigem avaliação posterior; a suíte não comprova todos esses cenários.

## Perguntas de estudo

1. Por que uma tag se liga ao ID e não ao título ou caminho do caderno?
2. Como diferenciar ausência de resultados de uma falha de armazenamento?
3. Por que o resultado deve ser revalidado antes de abrir uma página movida?
4. Como normalizar para busca sem alterar o texto original salvo?
5. Por que modificar a lista de tags precisa ser uma operação atômica?

Backup e restauração foram implementados depois no [capítulo 18](18-backup-e-restauracao.md). A correção de caixa/tags está registrada no [relatório da entrega conjunta](../reviews/024-backup-green-e-correcao-tags.md).
