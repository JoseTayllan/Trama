# Painel do acervo: especificação da consulta

O painel ocupará a área de conteúdo dos Cadernos quando não houver uma página aberta. Seu protótipo foi aprovado anteriormente. A primeira entrega prepara os indicadores; o editor e a navegação ainda permanecem iguais.

## Dados e limites

`ILibraryOverviewReader.ReadOverviewAsync` propõe um retrato somente leitura com quatro totais: espaços, cadernos, páginas e páginas não organizadas. Cada espaço também recebe sua contagem de cadernos e páginas. Espaços vazios aparecem; títulos iguais continuam entidades diferentes por ID.

Páginas estruturadas e notas antigas entram no total, uma vez por identidade. Uma versão estruturada substitui a representação antiga do mesmo ID. Notas sem caderno entram em Não organizadas. Itens na lixeira e seus descendentes ficam fora de todos os indicadores.

As cinco tags mais usadas são ordenadas pela quantidade de páginas distintas e, no empate, pelo nome ordinal normalizado. A normalização existente preserva a busca sem distinção de maiúsculas/minúsculas. Essa contagem mede páginas com a tag, não ocorrências de palavras no texto.

Os contratos ficam na Application. A proposta é o adaptador `SqliteKnowledgeDiscovery` implementar o leitor e a composição registrá-lo para o perfil escolhido. Os dados já existem nas tabelas: não há migração, dependência nova ou mudança no backup. Uma leitura deve usar seu próprio contexto, refletir alterações posteriores e propagar cancelamento. Caminho inexistente deve gerar erro sem criar banco vazio. O teste de leitura compara os bytes do banco antes/depois e verifica que notas antigas não recebem data de edição inventada.

Páginas recentes já usam `IRecentPagesReader`; essa consulta será reutilizada na integração visual. O contrato desta etapa não duplica datas recentes nem carrega conteúdo de notas no painel.

## Ordem das entregas

1. Revisar testes/contrato; implementar consulta SQLite e registro no aplicativo.
2. Especificar estado do painel e navegação para destinos, protegendo rascunhos.
3. Especificar e implementar a interface conforme o protótipo, incluindo atualização, vazio, erro e janela compacta.
4. Validar em Windows/WSL e revisar o diff. O responsável executa um commit por implementação.

Nesta etapa existem somente contratos e testes. Os 19 casos compilam e falham pelo leitor ausente ou registro ausente; a base anterior de 1.358 passou separadamente. Ver o [relatório RED](../reviews/142-library-overview-storage-red.md). A implementação aguarda aprovação conforme o CODEX §9.

Para estudar: por que o painel deve contar identidades em vez de títulos? Como a lixeira de um espaço afeta os filhos? Por que uma leitura não deve criar um perfil vazio? Por que os totais globais não podem depender do caderno selecionado na lateral?
