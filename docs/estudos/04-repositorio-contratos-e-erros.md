# Repositório contratos e erros

[Central de estudos](README.md) · [Capítulo anterior](03-sqlite-migracoes-e-durabilidade.md)

## Objetivo e estado

Entender como o caso de uso da primeira nota é ligado ao banco já preparado pela migração. Após a aprovação dos 14 testes, **o adaptador foi implementado e passou ao GREEN**. A ligação está validada nos testes; o desktop ainda não a utiliza.

O [relatório GREEN](../reviews/006-sqlite-repository-green.md) registra os resultados e limites atuais. O [relatório RED](../reviews/005-sqlite-repository-red.md) preserva o planejamento e a execução anterior à sua aprovação.

## Gravação e consulta têm contratos distintos

[INoteRepository](../../src/Trama.Application/Knowledge/INoteRepository.cs) já expõe `AddAsync`. CreateNote precisa gravar, não consultar. Adicionar uma operação de leitura obrigatória nessa interface forçaria também os consumidores e os fakes atuais a conhecê-la.

Criamos [INoteReader](../../src/Trama.Application/Knowledge/INoteReader.cs), que retorna `Task<NoteSnapshot?>` em `GetByIdAsync`. Assim, cada consumidor poderá receber somente a capacidade necessária. É uma aplicação prática de segregação de interfaces, não uma obrigação de criar uma interface para cada método do produto.

O resultado nulo significa **consulta concluída sem encontrar aquele ID**. Falha real do armazenamento não pode virar nulo silenciosamente: a especificação exige `NotePersistenceException`. Isso permite distinguir “não existe” de “não consegui verificar”.

## Adaptador e dependências

[SqliteNoteRepository](../../src/Trama.Infrastructure/Persistence/SqliteNoteRepository.cs) implementa os dois contratos e recebe TramaDbContext pelo construtor. Antes do GREEN, seus métodos eram esqueletos que lançavam NotImplementedException. Agora executam gravação e consulta reais no contexto fornecido. O adaptador continua sem registro no desktop e não cria banco nem aplica migrações sozinho.

O fluxo implementado e verificado nos testes é:

```text
CreateNote → INoteRepository.AddAsync → adaptador → EF Core → SQLite
Consulta   → INoteReader.GetByIdAsync → adaptador → EF Core → SQLite
```

A aplicação recebe NoteSnapshot, não NoteRecord, DbContext ou IQueryable. O contexto é fornecido pelo chamador e descartado no escopo correspondente; a política de composição no desktop será tratada quando existir interface.

## Como a escrita foi implementada

O construtor guarda a referência do contexto em `_context`. AddAsync verifica argumentos e cancelamento e cria um NoteRecord com o mesmo identificador, título e corpo do snapshot, sem Trim ou geração de novo ID.

`_context.Set<NoteRecord>().Add(record)` registra a intenção de inserir no change tracker. Essa chamada ainda não grava no arquivo: a persistência ocorre em `await _context.SaveChangesAsync(cancellationToken)`. CreateNote só retorna Created depois que essa tarefa termina com sucesso.

Não repetimos a validação de conteúdo mínimo no adaptador: ela continua no caso de uso. Também não chamamos Migrate ou EnsureCreated; preparar o esquema é responsabilidade de inicialização, não de cada gravação.

## Como a consulta foi implementada

A leitura usa `Where(record => record.Id == id)` e projeta diretamente para NoteSnapshot. `SingleOrDefaultAsync` retorna nulo quando não existe registro. Como Id é chave primária, não deve haver duas linhas para o mesmo identificador.

`AsNoTracking()` explicita que a operação é de leitura e não precisa manter a entidade como objeto editável no contexto. A projeção de dados escalares para o snapshot também evita expor NoteRecord ao consumidor. Não há cache próprio nem consulta a uma lista em memória.

O token de cancelamento é verificado antes da consulta e repassado ao EF. O resultado vem do banco, como demonstram o seed SQL independente e a recuperação com outro contexto.

## Testes independentes de escrita e leitura

Um teste que grava e lê somente pelo mesmo adaptador pode deixar dois erros complementares se esconderem. A nova [suíte](../../tests/Trama.IntegrationTests/Knowledge/SqliteNoteRepositoryTests.cs) inclui:

- Gravação pelo adaptador, confirmada por SQL independente.
- Inserção por SQL parametrizado, consultada pelo adaptador.
- Gravação e consulta por contextos diferentes, preservando ID e conteúdo.

O seed e a consulta independentes passam o identificador como Guid parametrizado, usando a representação do provedor. Isso evita impor uma capitalização textual diferente da utilizada por ele.

As demais verificações cobrem ID ausente, duplicidade, banco somente leitura, falha de consulta, cancelamento e integração com CreateNote. Os arquivos temporários são exclusivos e usam pooling desativado; nenhum banco pessoal é acessado.

## Falhas de persistência e cancelamento

O teste de duplicidade usa contextos diferentes para o original e a tentativa duplicada. No mesmo contexto, o tracking poderia acusar dois objetos com a mesma chave antes de chegar à restrição do SQLite.

Para provocar falha de escrita de forma determinística, os testes migram o arquivo com acesso gravável e depois abrem um novo contexto em `ReadOnly`. Não dependem de atributos de arquivo ou permissões pessoais do sistema.

O erro técnico é preservado como causa interna, com mensagem própria sem o conteúdo da nota. A escrita usa um filtro de exceção para DbUpdateException ou DbException; a leitura traduz DbException. Assim, não há captura genérica de qualquer defeito como erro de banco. Isso não autoriza registrar indiscriminadamente a causa em logs: dados e parâmetros de consultas podem exigir tratamento próprio.

CreateNote reconhece NotePersistenceException e retorna PersistenceFailed sem nota de sucesso. Os testes agora verificam essa integração com SQLite, além do fake usado anteriormente.

Cancelamento é diferente de falha de armazenamento. Tokens previamente cancelados devem produzir OperationCanceledException ou uma derivada. Esses testes não demonstram interrupção imediata de qualquer comando SQLite já em execução.

O contexto que sofre uma gravação malsucedida é descartado nos testes. Recuperação do tracking e reutilização desse contexto ainda não fazem parte deste contrato inicial.

## Do RED ao GREEN

No RED havia 14 casos novos: um já passava porque CreateNote rejeitava entrada inválida antes de acessar o adaptador; 13 paravam nos esqueletos. Os 28 anteriores continuavam passando.

Depois da aprovação e implementação, os 14 casos passaram sem alterações. Agora as verificações de armazenamento, consulta, falhas esperadas e cancelamento prévio são alcançadas. A suíte completa possui 42 aprovados. Esse GREEN não comprova interface, composição do desktop, recuperação após falha de energia ou cancelamento instantâneo de um comando já em execução.

## Como executar sem fechar a janela em uso

Nesta verificação o executável da janela estava aberto e bloqueou o build normal. Foi usada uma saída isolada, sem encerrar a janela do usuário:

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/repository-build
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --artifacts-path artifacts/repository-build --no-build --filter FullyQualifiedName~SqliteNoteRepositoryTests
dotnet test tests/Trama.UnitTests/Trama.UnitTests.csproj --configuration Release --artifacts-path artifacts/repository-build --no-build
```

O build e os testes precisam usar a mesma pasta de artefatos; caso contrário, você pode executar binários antigos. O teste filtrado agora deve passar os 14 casos. A opção de saída isolada não muda a arquitetura nem conecta o adaptador ao aplicativo aberto.

## Perguntas de revisão

1. Por que um erro de consulta não deve ser convertido em nota inexistente?
2. Como SQL independente ajuda a detectar erro no adaptador?
3. Por que duplicidade é testada com um contexto novo?
4. Qual consumidor atual precisaria mudar se adicionássemos leitura obrigatória em INoteRepository?
5. Qual parte da durabilidade já foi demonstrada pelo esquema e qual ainda depende do repositório?

Não há editor, busca, alteração de notas ou inicialização automática nesta fatia. A implementação cumpriu os contratos sem alterar os testes aprovados. O próximo recorte precisa definir diretório de dados, inicialização e duração dos contextos antes de ligar o primeiro fluxo à interface.
