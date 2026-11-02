# SQLite migrações e durabilidade

[Índice de estudos](README.md) · [Capítulo anterior](02-primeira-nota-e-tdd.md)

## Objetivo e estágio atual

Entender a diferença entre testar a aplicação com um repositório controlado e comprovar que dados permanecem em um arquivo SQLite. Após sua aprovação dos dez testes, **o esquema passou ao GREEN**: modelo de armazenamento, mapeamento e migração inicial estão implementados. O repositório concreto ainda não existe.

O caso de uso da primeira nota continua implementado e com seus testes passando. O armazenamento avança por incrementos: esquema e migração concluídos; depois adaptador de repositório; por fim interface. Consulte o [relatório GREEN](../reviews/004-sqlite-schema-green.md) e o [histórico RED](../reviews/003-sqlite-schema-red.md).

## Memória e arquivo são verificações diferentes

O teste técnico inicial usa `Data Source=:memory:`. Ele confirma que o provedor nativo funciona, mas o banco desaparece ao encerrar sua conexão. Não demonstra que uma nota poderá ser recuperada amanhã.

O novo [NoteSchemaTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NoteSchemaTests.cs) cria um diretório temporário exclusivo e um arquivo `notes.db` por caso. Depois de inserir, descarta o contexto e sua conexão e consulta o mesmo arquivo por uma nova conexão. Isso impede que um resultado seja apenas um objeto ainda disponível na memória do contexto anterior.

Os testes agora comprovam reabertura de uma conexão, não tolerância a queda de energia nem uma reinicialização completa do desktop. Não ampliar a conclusão além da evidência.

## O que é uma migração

Uma migração descreve uma evolução versionada do esquema do banco. O EF Core registra quais migrações foram aplicadas e usa esse histórico para não aplicar novamente a mesma alteração.

O [TramaDbContext](../../src/Trama.Infrastructure/Persistence/TramaDbContext.cs) mapeia o modelo interno [NoteRecord](../../src/Trama.Infrastructure/Persistence/NoteRecord.cs). `Database.MigrateAsync()` instala o esquema definido pelas migrações de produção. Os testes não criam `Notes` por SQL e não chamam `EnsureCreated`: agora a tabela vem da migração real.

A migração foi gerada e revisada depois da aprovação. O esquema implementado é:

| Coluna | Representação SQLite | Restrição |
| --- | --- | --- |
| `Id` | TEXT | Chave primária e não nula |
| `Title` | TEXT | Não nula; pode ser string vazia |
| `Body` | TEXT | Não nula; pode ser string vazia |

Título ou corpo com texto é uma regra já validada na aplicação. Estes testes de armazenamento não adicionam um segundo validador de conteúdo e não exigem projeto ou tags. A representação textual do identificador é lida como `Guid`, sem exigir capitalização específica.

## Modelo mapeamento migração e snapshot

`NoteRecord` descreve o formato usado pela persistência, sem se tornar um contrato público da aplicação. Por isso ele é interno à Infrastructure. Não acrescentamos comportamento fictício à Domain para preencher uma camada.

`OnModelCreating` informa ao EF que esse tipo corresponde a Notes, que Id é chave e que todas as colunas são obrigatórias. `ValueGeneratedNever()` indica que a aplicação fornece o identificador; o banco não deve gerar outro. `HasColumnType("TEXT")` e `IsRequired()` expressam o contrato aprovado.

O mapeamento, sozinho, não modifica bancos existentes. A [migração InitialNotes](../../src/Trama.Infrastructure/Persistence/Migrations/20261027211013_InitialNotes.cs) contém operações de esquema: `Up` cria Notes e `Down` remove a tabela. Seu [modelo de destino](../../src/Trama.Infrastructure/Persistence/Migrations/20261027211013_InitialNotes.Designer.cs) registra o modelo dessa versão. O [snapshot](../../src/Trama.Infrastructure/Persistence/Migrations/TramaDbContextModelSnapshot.cs) serve de base para o EF comparar o modelo na geração da próxima migração.

Não confundir esses arquivos com backup. `Down` apaga a tabela e seu conteúdo: nunca executar rollback sobre dados pessoais sem autorização e backup recuperável. Uma migração já distribuída deve ser preservada; alterações futuras usam uma nova migração.

O comando de geração usado foi `dotnet ef migrations add InitialNotes`, com Infrastructure como projeto, Desktop como startup, configuração Release e caminho absoluto explícito para a factory. Gerar esses arquivos não aplicou a migração nesse caminho nem criou banco pessoal; os testes aplicaram `Up` somente em arquivos temporários.

## Por que repetir a migração

O teste de inicialização repetida primeiro prepara o banco, insere um registro e chama `MigrateAsync()` novamente. O conteúdo precisa continuar disponível. Isso protege a repetição da mesma inicialização; não demonstra compatibilidade com todas as futuras mudanças de versão. Migrações futuras precisarão de testes próprios com dados antigos.

## SQL parametrizado e teste de restrições

As inserções usam `ExecuteSqlInterpolatedAsync`, que recebe a interpolação como `FormattableString` e transforma valores em parâmetros. A consulta usa um parâmetro `$id`. Não concatenamos título ou corpo ao comando SQL.

O texto de teste contém acentos, emoji, espaços, código e aspas. O objetivo é preservar esses caracteres e a estrutura do conteúdo; isso não é uma auditoria completa de SQL injection.

Os testes de duplicidade e nulidade conferem códigos de erro SQLite, em vez de depender da mensagem traduzida. O caso de duplicidade também verifica que o conteúdo original não foi substituído. As restrições só poderão ser exercitadas quando a tabela existir.

## Isolamento e limpeza

Cada caso tem um caminho temporário aleatório e dados sintéticos. `SqliteConnectionStringBuilder` constrói a configuração sem concatenar uma connection string manual. `Pooling = false` evita que uma conexão fique retida no pool depois do descarte e mantenha o arquivo aberto.

Comandos, readers, contextos e conexões usam descarte determinístico. Na limpeza, são removidos apenas o banco criado pelo teste e seus arquivos auxiliares conhecidos; o diretório é removido sem exclusão recursiva. O teste não usa caminhos pessoais nem a configuração do desktop.

## Do RED ao GREEN desta etapa

No RED, dez casos novos falhavam e os 18 anteriores passavam. Os motivos eram ausência de migração aplicada e ausência de Notes após `MigrateAsync()`.

Os testes de duplicidade e campos nulos então paravam no pré-requisito de esquema. Um erro de tabela ausente não comprovava a restrição de nulidade. Depois da implementação, os dez casos passaram sem alterações e alcançaram suas asserções de chave primária, obrigatoriedade, reabertura e preservação. A suíte local completa agora tem 28 aprovados; a CI remota ainda não foi executada.

## Reproduzir

Depois de compilar a versão atual, na raiz:

```powershell
dotnet build Trama.slnx --configuration Release --no-restore
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --no-build --filter FullyQualifiedName~NoteSchemaTests
dotnet test tests/Trama.UnitTests/Trama.UnitTests.csproj --configuration Release --no-build
```

O comando filtrado agora deve passar os dez casos do esquema. Os resultados RED continuam no histórico; uma falha nova exige investigação. Erro de compilação, pacote ausente ou acesso negado ao temporário não é evidência comportamental de TDD.

## Perguntas para estudar

1. Por que usar uma conexão nova torna o teste mais forte do que consultar pelo mesmo contexto?
2. O que um teste perderia ao criar manualmente a tabela no setup?
3. Por que string vazia e valor nulo têm papéis diferentes neste esquema?
4. Por que testar SQL direto ainda não comprova o funcionamento de `INoteRepository`?
5. Qual é a diferença entre repetir a mesma migração e testar a atualização entre duas versões do banco?

O GREEN foi obtido sem alterar os testes aprovados. Editor, diretório pessoal e repositório ainda terão seus próprios recortes: a presença da tabela não liga automaticamente CreateNote ao banco nem cria uma tela de edição.
