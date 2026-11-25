# Painel do acervo: consulta implementada

Após a aprovação dos 19 casos e do contrato, `SqliteKnowledgeDiscovery` passou a implementar `ILibraryOverviewReader`. O método fica em `SqliteKnowledgeDiscovery.Overview.cs`; a composição fornece o mesmo adaptador já associado ao perfil escolhido. O banco é inicializado pelo fluxo existente do aplicativo; o leitor não cria nem migra bancos.

## Como a leitura funciona

O construtor mantém a configuração existente das operações de busca/tags e prepara uma configuração adicional com SQLite em modo somente leitura. Cada chamada cria e descarta seu contexto. Um caminho inexistente falha sem criar arquivo, com erro convertido para `NotePersistenceException`. Cancelamento continua sendo cancelamento.

Uma transação de leitura adiada envolve todas as consultas, mantendo o mesmo retrato dos dados entre os totais e as listas. A transação não reserva antecipadamente um escritor. IDs de páginas atuais e notas antigas são unidos no banco; a união evita contar duas representações da mesma identidade. Junções com a localização e os cadernos identificam o espaço; a lixeira dos três níveis é excluída.

O SQLite agrupa as contagens por espaço e por tag sem materializar corpos de notas ou blocos. O resultado em memória contém títulos de espaços e agregações. As tags são ordenadas por quantidade decrescente e nome ordinal, limitadas às cinco primeiras. A normalização dos nomes continua no fluxo de gravação já existente.

Espaços vazios aparecem com zero. Páginas sem caderno entram no total e em Não organizadas. O resultado não grava datas de edição. A consulta existente de páginas recentes será usada na próxima integração, sem duplicação deste contrato.

## Evidência e limites

Os 19 casos aprovados passaram sem alterações; o SHA-256 do arquivo foi conferido. Build Release, formatação e os 1.377 testes da suíte passaram no WSL. A interface permanece igual nesta entrega: o painel visual, a abertura de destinos e a proteção de rascunhos ao navegar serão as próximas especificações. Sem migração, nova dependência ou alteração no formato do backup.

Veja a [especificação](118-painel-acervo-consulta-especificacao.md) e o [relatório GREEN](../reviews/143-library-overview-storage-green.md). A revisão do diff e os comandos Git continuam com o responsável.

Para estudar: por que uma transação de leitura é importante em várias consultas? Qual a diferença entre UNION e UNION ALL para notas antigas? Por que somar agregações evita carregar o conteúdo inteiro? Por que não reutilizar um DbContext como singleton no desktop?
