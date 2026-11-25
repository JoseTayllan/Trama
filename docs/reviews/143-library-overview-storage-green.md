# Painel do acervo · consulta em GREEN

Em 19/11/2026, o responsável aprovou explicitamente os 19 casos de `LibraryOverviewStorageTests.cs` e o contrato `ILibraryOverviewReader`. A implementação mantém esse arquivo de testes byte a byte; o SHA-256 anterior e posterior foi comparado.

## Implementação e revisão

- `SqliteKnowledgeDiscovery` implementa o leitor em um arquivo parcial; as operações de busca/tags existentes mantêm sua configuração. Somente a consulta do painel usa a conexão somente leitura.
- Uma transação adiada mantém um retrato consistente dos indicadores sem reservar um escritor na abertura.
- A união dos IDs evita duplicar a página estruturada e sua nota antiga. Os três níveis da lixeira são excluídos; espaços vazios e páginas não organizadas são preservados.
- Contagens e tags são agregadas no banco, sem materializar texto, código ou imagens. A ordenação ordinal desempata tags e limita as cinco primeiras.
- A composição resolve o leitor a partir do adaptador já criado para aquele perfil. Nenhum contexto é compartilhado entre operações.
- Falhas de SQLite viram `NotePersistenceException`; cancelamento é propagado. O banco ausente não é criado. Não foram adicionados logs nem credenciais.

## Verificações

No WSL: build Release sem advertências, `dotnet format --verify-no-changes --no-restore`, 19 casos focados e suíte completa com 1.377 aprovações (224 unitários e 1.153 de integração). Os testes usam perfis temporários. Nenhum teste anterior foi alterado. Nenhum teste nativo do Windows ou Verify remoto foi executado pelo agente nesta entrega.

Os comandos do foco e da base estão no [relatório RED](142-library-overview-storage-red.md). A suíte final usou `dotnet test Trama.slnx -c Release --no-build`. Nenhuma migração, dependência ou formato de backup mudou. Não há nova tela ou publicação do executável nesta etapa.

## Riscos e próxima entrega

A consulta pressupõe o perfil inicializado pelo fluxo existente do aplicativo. Um banco antigo deve passar pelas migrações de inicialização antes do painel, como os demais serviços. Não houve ensaio de desempenho com acervo de grande volume; as consultas trabalham com IDs e agregações, mas ainda leem todas as tags agregadas para aplicar a ordenação ordinal. O painel e seu comportamento em navegação/erros continuam por implementar.

O diff está disponível nos artefatos da entrega. Revisão final e commit/push são do responsável, um commit para esta implementação. A próxima etapa especificará o estado e a navegação do painel, preservando rascunhos e reutilizando IRecentPagesReader.

Um agente, sem subagentes; uma rodada de implementação e revisão, com foco e suíte completa. Esforço moderado; sem medição de tokens/custo. Economia sugerida: manter a próxima etapa restrita ao estado e à navegação, reutilizando o leitor validado.
