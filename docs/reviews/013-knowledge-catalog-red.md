# Catálogo de espaços, cadernos e páginas — RED

Data: 27 de outubro de 2026. Especificação disponível para aprovação antes da implementação.

## Escopo para revisar

- [KnowledgeCatalogTests](../../tests/Trama.IntegrationTests/Knowledge/KnowledgeCatalogTests.cs): 13 casos novos de persistência e compatibilidade.
- [IKnowledgeCatalogRepository](../../src/Trama.Application/Knowledge/IKnowledgeCatalogRepository.cs): contrato e records de espaço, caderno e resumo de página.
- [SqliteKnowledgeCatalog](../../src/Trama.Infrastructure/Persistence/SqliteKnowledgeCatalog.cs): adaptador vazio, sem ligação com o desktop.
- [Modelo de organização e explicações](../estudos/12-espacos-cadernos-e-paginas.md).

Os testes anteriores não foram modificados. Nenhuma migração, pacote ou mudança de interface foi adicionada.

## Execução final

```powershell
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/library-build --logger "trx;LogFilePrefix=library-red-final" --results-directory artifacts/library-red/final-results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**126 casos executados: 113 aprovados e 13 falhando, sem ignorados.** Passaram os 36 unitários e 77 de integração existentes. Os novos testes de integração falharam; a compilação terminou, e a formatação passou. Testes terminaram com código 1; formatação com código 0. Evidências TRX em `artifacts/library-red/final-results`. A CI remota não foi executada.

| Grupo | Casos | Motivo atual |
| --- | --- | --- |
| Migração preservando notas e páginas | 1 | Não existe terceira migração; a asserção pede evolução sem reescrever as anteriores |
| Espaços/cadernos, isolamento, movimentação, não organizadas, notas antigas e edição posterior | 6 | Métodos do catálogo ainda lançam NotImplementedException, inclusive na preparação |
| Página ou destino inexistente e caderno sem espaço | 3 | Métodos vazios lançam NotImplementedException antes de validar o comportamento esperado |
| Falha de esquema, cancelamento prévio e banco somente leitura | 3 | NotImplementedException no lugar da falha ou cancelamento especificado |

O RED comprova descoberta e execução dos cenários. As asserções posteriores de preservação e reabertura ainda não foram alcançadas na maioria dos casos; não anunciar catálogo funcional.

## Revisão e riscos

A hierarquia inicial é Espaço → Cadernos → Páginas. Assuntos são nomes livres; não criar uma entidade Curso para limitar todos os espaços a estudos. Cada página pertence a um caderno por vez, com itens sem localização consultáveis em Não organizadas.

NotebookSnapshot continua representando a página já existente. O contrato novo não substitui o formato de seus blocos nem seus leitores. A precedência entre registro estruturado e nota antiga deve continuar igual à leitura atual, sem duplicar o item na lista.

As migrações existentes não podem ser reescritas. A implementação deverá testar evolução em arquivo já preenchido, preservar corpo e identidade e garantir que uma movimentação inválida não perca o destino anterior. Usar contexto novo após erro de persistência.

Ainda não há testes de navegação, confirmação ao abandonar rascunho, criação e atribuição atômicas de página nova, exclusão, renomeação, concorrência ou ordenação visual. Nenhum desses comportamentos está autorizado como consequência automática desta especificação.

## Esforço e decisão pendente

Um agente, sem delegação ou dependências novas. Duas execuções da suíte porque foi acrescentado o cenário de salvar novamente uma página já organizada; uma verificação de formatação. Sem medição de tokens por tarefa. Reutilizar os adaptadores e limitar a próxima exploração ao catálogo evita reanalisar o editor inteiro.

Decisão pendente: aprovação dos 13 casos e da hierarquia inicial, conforme a seção 9 do CODEX.md, antes de implementar persistência e migração.
