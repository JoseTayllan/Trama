# Espaços, cadernos e páginas

## Como organizar conteúdos diferentes

A organização aprovada tem três níveis, com nomes livres:

```text
Trama
├── Curso de C#                         espaço
│   ├── Fundamentos                     caderno
│   │   ├── Variáveis e tipos            página
│   │   └── Condicionais                 página
│   ├── Orientação a Objetos            caderno
│   │   ├── Classes e objetos            página
│   │   └── Herança e polimorfismo       página
│   └── Projetos                        caderno
│       └── Minha primeira API           página
├── Machine Learning                    espaço
│   └── Experimentos                    caderno
│       └── Regressão linear             página
└── Trabalho                            espaço
    └── Reuniões                        caderno
        └── Planejamento da semana       página
```

Espaço é uma área de interesse, não um tipo fixo de curso. Pode representar trabalho, pesquisa, projetos pessoais ou qualquer assunto. Caderno reúne páginas relacionadas. Página é o documento de texto, código e imagens que já foi implementado.

Esta primeira versão não propõe subcadernos recursivos nem um limite artificial de quantidade de assuntos. Profundidade arbitrária pode ser avaliada depois; não confundir flexibilidade dos nomes e conteúdos com hierarquia infinita já disponível.

## O que existe e o que está em revisão

Após aprovação, o catálogo passou ao GREEN com **126 testes naquela entrega**, sem alterações dos testes. A migração `20261028003249_KnowledgeCatalog` adicionou a organização ao banco. A navegação visual foi implementada posteriormente, com 157 testes aprovados; veja o [capítulo 14](14-biblioteca-visual-implementada.md).

O nome técnico NotebookSnapshot, usado nas entregas anteriores, representa uma página. Não vamos renomeá-lo e quebrar seus consumidores só para uniformizar o vocabulário. No novo contrato, KnowledgeSpace representa espaço, NotebookInfo representa caderno e KnowledgePageSummary representa um item na lista de páginas.

O [contrato de catálogo](../../src/Trama.Application/Knowledge/IKnowledgeCatalogRepository.cs) separa organização de conteúdo. [SqliteKnowledgeCatalog](../../src/Trama.Infrastructure/Persistence/SqliteKnowledgeCatalog.cs) agora registra espaços e cadernos, consulta seus filhos, lista páginas não organizadas e atribui uma página existente a um caderno. O repositório mantém o contexto emprestado; não o registra como singleton nem o compartilha entre operações concorrentes.

## Como o catálogo foi implementado

Três tabelas novas guardam somente metadados: KnowledgeSpaces contém os espaços; Notebooks referencia seu espaço; PageLocations associa o ID de uma página ao ID de um caderno. A chave primária de PageLocations é o próprio PageId, garantindo uma localização por página. As chaves estrangeiras restringem cadernos sem espaço e localizações sem caderno.

PageId não recebe uma chave estrangeira direta para conteúdo porque pode representar tanto Notes quanto NotebookPages. PlacePageAsync verifica a existência em uma dessas fontes e grava a associação dentro de uma transação. Destino inválido falha sem confirmar a mudança. Não há API de exclusão nesta fatia; qualquer exclusão futura de páginas precisará coordenar seus metadados para não deixar associações órfãs.

As listagens de páginas usam uma única consulta SQL que reúne páginas estruturadas e notas antigas, excluindo a versão antiga quando existe uma página estruturada com o mesmo ID. O filtro de localização seleciona o caderno ou os itens sem associação. O resumo usa o título atual da fonte; não mantém uma cópia desatualizada no catálogo. Consultas não rastreiam entidades e retornam somente IDs e títulos, sem carregar blocos e imagens.

Há ordenação determinística por título e ID no adaptador, com a comparação padrão do SQLite. Ordenação visual conforme idioma e preferência ainda não foi especificada.

## Comportamentos especificados pelos testes

- Espaços e cadernos mantêm identidades próprias e títulos exatos após reabrir o banco.
- Um espaço pode conter vários cadernos. Espaços diferentes podem ter cadernos com o mesmo título, como Fundamentos.
- Cadernos vazios são permitidos; criar um caderno não exige uma página de exemplo.
- Listar um caderno mostra apenas suas páginas.
- Mover uma página muda sua localização, preservando identidade, título, texto, código e referências de imagens. Repetir o mesmo destino não duplica a entrada.
- Editar e salvar novamente a página mantém sua localização e atualiza o título apresentado no catálogo.
- Página inexistente não pode gerar uma entrada falsa. Destino inexistente não pode remover a localização anterior. Caderno sem espaço existente é rejeitado.
- Falha de banco deve ser reportada, não apresentada como biblioteca vazia. Cancelamento prévio e banco somente leitura não deixam registros novos.

Os testes estão em [KnowledgeCatalogTests](../../tests/Trama.IntegrationTests/Knowledge/KnowledgeCatalogTests.cs). Usam arquivos SQLite temporários e os adaptadores reais de página e nota já implementados. As referências de imagem dessa fatia são metadados; os testes não importam nem decodificam seus arquivos.

## Notas atuais e Não organizadas

A migração adiciona o catálogo sem alterar as duas migrações anteriores nem apagar ou reescrever notas e páginas. Os documentos existentes começam em **Não organizadas**, sem escolher automaticamente um curso ou criar cópias das páginas. Essa seção foi conectada à interface na etapa de navegação.

Uma nota antiga pode ser organizada sem regravar seu corpo. Se existe uma nota antiga e uma versão estruturada com a mesma identidade, o catálogo apresenta uma única página e usa o título da versão estruturada, seguindo a precedência do leitor atual. Os registros originais continuam preservados.

Colocar uma página em um caderno deve retirá-la da consulta de não organizadas. A atribuição é única nesta primeira versão: uma página pertence a um caderno por vez. Referências à mesma página em vários lugares, favoritos e tags não estão implementados por este contrato.

## Decisões técnicas e limites

O contrato atual é de repositório, não uma dependência para injetar diretamente em ViewModels. A próxima integração deverá passar por casos de uso de aplicação e manter um contexto por operação, como no editor existente. O repositório em revisão recebe contexto emprestado; o chamador deverá descartá-lo após falhas de persistência.

Esta fatia não determina datas de criação, validação de nomes vazios na interface ou ordenação conforme idioma. Os testes comparam identidades e relações, sem depender de uma ordenação específica. Também não incluem exclusão de espaços/cadernos, renomeação, compartilhamento ou mudança de um caderno entre espaços.

Não há ainda atomicidade especificada para criar uma página nova e atribuí-la ao caderno selecionado em uma única ação. Esse comportamento e a proteção do rascunho ao trocar de página precisam entrar nos testes da navegação, antes de conectar a interface. Mover uma página existente não autoriza excluir seu conteúdo ou os arquivos anexos.

## Sequência de implementação

1. Revisar a hierarquia proposta e os 13 testes desta fatia — aprovado.
2. Implementar catálogo persistente e migração nova sem alterar os testes aprovados — realizado, disponível para revisão do diff.
3. Especificar casos de uso e navegação: criar espaço, criar caderno, listar páginas, abrir, criar página no caderno selecionado e tratar alterações não salvas antes de trocar.
4. Conectar a biblioteca à barra lateral, reutilizando o editor contínuo e as cores semânticas.

Resultado final: **126 casos aprovados: 36 unitários e 90 de integração**, build sem avisos e formatação aprovada. Veja o [relatório GREEN](../reviews/014-knowledge-catalog-green.md) e o [histórico RED](../reviews/013-knowledge-catalog-red.md). Os testes usam bancos temporários; a migração não foi aplicada ao banco pessoal nesta entrega. A inicialização existente aplicará as migrações conhecidas na próxima abertura do aplicativo.

## Para estudar

Qual a diferença entre conteúdo e localização de uma página? Por que IDs devem ser independentes dos títulos? Como mover uma página sem recriar seus blocos? Por que duas fontes de conteúdo com o mesmo ID devem aparecer como uma página só? Por que uma consulta vazia é diferente de uma falha de banco?
