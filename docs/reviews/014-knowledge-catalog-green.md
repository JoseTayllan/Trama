# Catálogo de espaços, cadernos e páginas — GREEN

Data: 27 de outubro de 2026. Implementação após aprovação dos 13 casos do [RED](013-knowledge-catalog-red.md), disponível para revisão humana do diff.

## O que mudou

[SqliteKnowledgeCatalog](../../src/Trama.Infrastructure/Persistence/SqliteKnowledgeCatalog.cs) implementa o contrato aprovado: registra espaços e cadernos, lista seus filhos e páginas não organizadas e move páginas existentes entre cadernos. A localização fica separada do conteúdo; salvar uma página continua mantendo seu caderno e atualiza o título mostrado nas listagens.

A migração `20261028003249_KnowledgeCatalog` adiciona KnowledgeSpaces, Notebooks e PageLocations, com chaves estrangeiras e índices. PageId é chave primária da associação, garantindo um caderno por página. As duas migrações anteriores e os testes aprovados não foram alterados. Nenhuma dependência ou tela mudou.

## Verificação executada

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/library-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/library-build --no-build --logger "trx;LogFilePrefix=library-green" --results-directory artifacts/library-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**126 casos aprovados: 36 unitários e 90 de integração, sem ignorados.** Build com zero avisos e zero erros. Formatação aprovada, incluindo o arquivo gerado da migração. Evidências TRX em `artifacts/library-green/results`. A CI remota não foi executada.

Os 13 casos passaram sem alterações e verificam atualização de banco existente, espaços e cadernos independentes, isolamento de páginas, movimentação idempotente, corpo legado preservado, precedência de página estruturada, localização após nova edição, rejeição de página/destino/espaço inexistente, falha de esquema, cancelamento prévio e banco somente leitura.

## Revisão e riscos

A migração cria somente metadados e não modifica Notes, NotebookPages ou NotebookBlocks. O teste de atualização parte da segunda migração com registros antigos e estruturados já preenchidos. Não foi aberto nem migrado o banco pessoal pelas ferramentas desta entrega.

PlacePageAsync verifica a existência da página e altera sua associação dentro de uma transação. Um destino inválido falha pela integridade referencial sem confirmar a movimentação. As consultas unem as duas fontes com prioridade para a página estruturada, filtram a localização no banco e não regravam notas nem carregam seus blocos.

O repositório recebe contexto emprestado. Descartá-lo após uma falha de persistência; não reutilizar entidades rastreadas de uma tentativa que falhou. As listagens usam AsNoTracking. Cancelamento é propagado; falhas de banco são reportadas como NotePersistenceException, seguindo o padrão já existente.

PageId não tem FK para uma tabela de conteúdo única porque existem duas fontes compatíveis. A existência é verificada pelo repositório, mas SQL externo ou uma exclusão futura mal coordenada ainda pode produzir localização órfã. Não há exclusão exposta nesta fatia, nem teste de acesso concorrente, interrupção abrupta ou falha artificial no meio da transação.

A migração Down remove os metadados de organização, mantendo o conteúdo das páginas. Não executar rollback sobre dados pessoais sem autorização e backup recuperável. A próxima abertura do desktop aplica as migrações conhecidas pelo fluxo de inicialização existente.

## Escopo pendente

O catálogo ainda não foi injetado na composição do desktop e não aparece na barra lateral. A próxima fatia precisa de casos de uso e testes para criar espaços/cadernos, listar e abrir páginas, criar uma página no caderno selecionado e proteger o rascunho ao navegar. Não injetar diretamente este repositório em ViewModels.

Nomes vazios, renomeação, exclusão, ordenação conforme idioma, compartilhamento e hierarquia recursiva não receberam contratos nesta etapa. Não apresentar persistência de metadados como uma biblioteca visual pronta.

## Documentação e esforço

O [capítulo de estudo](../estudos/12-espacos-cadernos-e-paginas.md), README, CODEX e roadmap foram atualizados. Um agente, sem delegação ou novas dependências. Houve geração/revisão de uma migração, uma execução da suíte completa e uma verificação final de formatação. Medição de tokens por tarefa indisponível; limitar a próxima exploração à navegação e reutilizar este catálogo reduz retrabalho.
