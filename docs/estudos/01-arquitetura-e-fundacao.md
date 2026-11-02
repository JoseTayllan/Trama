# Arquitetura e fundação do Trama

[Voltar à central](README.md) · [Próximo capítulo](02-primeira-nota-e-tdd.md)

## Objetivo

Entender como a fundação organiza a interface, os casos de uso e a infraestrutura sem misturar suas responsabilidades. Ao terminar, você deve conseguir explicar por que o Trama possui quatro projetos de produção, como a janela recebe seu ViewModel e o que a presença do SQLite comprova nesta fase.

O ponto de partida é um desktop local em C# e Avalonia. A fundação abre uma janela com a identidade do Trama. Persistência funcional de notas, editor e assistente são etapas distintas. A implementação da primeira nota é explicada no [capítulo de TDD](02-primeira-nota-e-tdd.md).

## Os quatro projetos

A [solução](../../Trama.slnx) reúne os projetos de produção e de teste. Um projeto define um limite de compilação e referências; uma pasta dentro dele ajuda a organizar funcionalidades.

| Projeto | Papel no Trama | Exemplo ou limite atual |
| --- | --- | --- |
| [Domain](../../src/Trama.Domain/Trama.Domain.csproj) | Regras que representam o negócio, independentes de interface e armazenamento | A fundação reserva esse limite; não cria regras artificiais apenas para ocupar a camada |
| [Application](../../src/Trama.Application/Trama.Application.csproj) | Operações solicitadas pelo usuário e contratos necessários para executá-las | Contratos da primeira nota em `Knowledge` |
| [Infrastructure](../../src/Trama.Infrastructure/Trama.Infrastructure.csproj) | Detalhes de armazenamento e integrações técnicas | Contexto EF Core e factory de design time |
| [Desktop](../../src/Trama.Desktop/Trama.Desktop.csproj) | Apresentação Avalonia e montagem das dependências | Janela inicial e seu ViewModel |

O grafo de referências declarado nos projetos é:

```text
Application → Domain
Infrastructure → Application
Desktop → Application e Infrastructure
Domain → nenhum outro projeto Trama
```

Uma referência permite usar tipos de outro projeto. Ela não obriga a utilizar esses tipos em qualquer parte. Por exemplo, o Desktop referencia Infrastructure para permitir a composição das implementações concretas; isso não autoriza um ViewModel a acessar banco ou arquivos diretamente.

Quando uma funcionalidade precisa ser testada sem abrir janela, ela normalmente deve estar na aplicação ou no domínio. Quando um detalhe pode mudar sem alterar a regra do usuário, como trocar um provedor de armazenamento, ele tende a pertencer à infraestrutura.

## MVVM na janela inicial

MVVM separa a apresentação visual do estado e das operações que ela apresenta:

- **View:** [MainWindow.axaml](../../src/Trama.Desktop/Views/MainWindow.axaml) declara layout, textos, cores, imagem e bindings.
- **ViewModel:** [MainWindowViewModel.cs](../../src/Trama.Desktop/ViewModels/MainWindowViewModel.cs) fornece `Heading`, `Description` e `Status`. Ele herda de `ObservableObject`, do CommunityToolkit.Mvvm.
- **Modelo e aplicação:** representam dados e operações. Não é necessário criar uma classe chamada `Model` para satisfazer o padrão.

Um binding como `{Binding Heading}` lê a propriedade correspondente no `DataContext`. O `x:DataType` da View identifica o tipo esperado para os bindings compilados. Na fundação, as propriedades são apenas de leitura; comportamentos de edição e comandos serão acrescentados quando a interface de notas for implementada.

O [code-behind da janela](../../src/Trama.Desktop/Views/MainWindow.axaml.cs) recebe o ViewModel e o coloca no `DataContext`. Seu papel é ligar a View à apresentação, sem concentrar regras de negócio.

## Injeção de dependências

Em vez de uma classe procurar ou construir todas as suas dependências, ela as recebe. Isso permite substituir implementações em testes e torna explícito o que uma operação precisa para funcionar.

O [App.axaml.cs](../../src/Trama.Desktop/App.axaml.cs) funciona como ponto de composição do desktop. Ele registra o ViewModel e a janela, constrói o provedor e solicita a janela principal:

```text
Inicialização do aplicativo
  → registro de MainWindowViewModel e MainWindow
  → criação do provedor de serviços
  → resolução de MainWindow com seu ViewModel
  → atribuição de desktop.MainWindow
```

O ViewModel está registrado como singleton: a mesma instância atende às resoluções dentro desse provedor. A janela está como transient: cada resolução pode criar uma instância. São escolhas da fundação; não significa que todo serviço futuro deve ser singleton ou transient.

O provedor é descartado quando o desktop encerra. `ValidateOnBuild` e `ValidateScopes` ajudam a detectar configurações inválidas, mas não substituem testes do comportamento. Não espalhar chamadas ao provedor pelos ViewModels: a composição pertence à entrada do aplicativo.

## Contrato não é persistência

Um contrato descreve o que uma operação recebe, devolve ou exige de outra parte. A aplicação já define [CreateNoteRequest](../../src/Trama.Application/Knowledge/CreateNoteRequest.cs), [CreateNoteResult](../../src/Trama.Application/Knowledge/CreateNoteResult.cs), [NoteSnapshot](../../src/Trama.Application/Knowledge/NoteSnapshot.cs) e [INoteRepository](../../src/Trama.Application/Knowledge/INoteRepository.cs).

`INoteRepository` permite que o caso de uso peça o armazenamento de uma nota sem conhecer SQL ou EF Core. Uma implementação controlada no teste pode registrar essa solicitação e simular falhas. Isso comprova a interação com o contrato; não demonstra que o conteúdo sobreviva ao encerramento do aplicativo.

Persistência real exige uma implementação concreta, mapeamento de dados e verificação de gravação e recuperação. O editor também precisa chamar o caso de uso e apresentar estados de sucesso ou erro. Essas partes não surgem automaticamente porque existe uma interface de repositório.

## API interna e HTTP

API também pode significar uma interface de programação dentro do próprio processo. No Trama, [CreateNote](../../src/Trama.Application/Knowledge/CreateNote.cs) expõe uma operação que outros componentes C# poderão chamar.

Isso é diferente de uma API HTTP: não há servidor web, rota, controller, middleware ou endpoint de pagamento nesta fundação. Um futuro assistente poderá consumir os mesmos casos de uso sem que seja necessário introduzir HTTP entre a janela e a aplicação.

## O papel do EF Core e do SQLite

O [TramaDbContext](../../src/Trama.Infrastructure/Persistence/TramaDbContext.cs) prepara o ponto de integração com EF Core, ainda sem o modelo de notas. A [factory de design time](../../src/Trama.Infrastructure/Persistence/TramaDbContextFactory.cs) permite às ferramentas criar o contexto sem abrir a interface. Ela exige `--database` seguido de caminho absoluto, evitando escolher silenciosamente um banco pessoal.

O [teste técnico do SQLite](../../tests/Trama.IntegrationTests/Foundation/SqliteProviderTests.cs) abre uma conexão SQLite em memória, executa SQL e verifica leitura e escrita de um valor. O provedor é SQLite real; o banco vive somente naquela conexão de teste. Isso confirma uma capacidade técnica do provedor, não o mapeamento ou a recuperação de notas do produto.

O [teste do shell](../../tests/Trama.IntegrationTests/Foundation/DesktopShellTests.cs) usa Avalonia headless para verificar construção da janela, vínculo do ViewModel, texto e carregamento da logo. Ele não comprova sozinho a aparência final em um monitor nem todas as interações do sistema operacional.

## Versões e comandos de referência

O [global.json](../../global.json) fixa SDK `10.0.401`, permitindo atualização de patch. O [Directory.Build.props](../../Directory.Build.props) define `net10.0`, nullable, analisadores e advertências como erros. O [Directory.Packages.props](../../Directory.Packages.props) centraliza Avalonia `11.3.19`, CommunityToolkit.Mvvm `8.4.0`, EF Core `10.0.9` e xUnit `2.9.3`.

Execute os comandos a partir da raiz. Eles são instruções para estudo; esta página não registra uma execução ou resultado de validação:

```powershell
dotnet restore Trama.slnx --locked-mode
dotnet tool restore
dotnet build Trama.slnx --configuration Release --no-restore
dotnet test Trama.slnx --configuration Release --no-build
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --no-build
```

`restore` obtém as dependências; `--locked-mode` exige que a resolução respeite os locks existentes. `build` compila. `test` executa os testes e `--no-build` pressupõe uma compilação compatível já atualizada. `format --verify-no-changes` verifica sem reescrever arquivos. `run` abre o desktop.

Para estudar apenas os testes técnicos da fundação:

```powershell
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --no-build
```

O [README principal](../../README.md) documenta também a consulta de migrações com `--no-connect` e caminho absoluto. Não usar `database update` ou criar migrações como exercício incidental: a persistência do produto exige uma etapa própria de implementação e revisão.

## Perguntas e exercícios opcionais

1. Acompanhe o caminho entre `App`, `MainWindow` e `MainWindowViewModel`. Onde cada objeto é construído e quem recebe a dependência?
2. Explique por que `Application` não referencia `Infrastructure`, mesmo quando precisa salvar uma nota.
3. Desenhe o fluxo futuro “clicar em salvar → caso de uso → repositório → resultado na interface”. Marque quais partes já existem e quais ainda faltam.
4. Liste três afirmações que o teste do SQLite comprova e três que ele não comprova.
5. Imagine trocar SQLite por outro armazenamento. Quais contratos deveriam permanecer e quais implementações poderiam mudar?
6. Explique a diferença entre a API interna do Trama e uma rota HTTP, sem acrescentar um servidor ao projeto.

Esses exercícios podem ser respondidos em anotações. Não é necessário alterar código para concluí-los. Para acompanhar a primeira regra funcional e sua especificação, siga para [Primeira nota e TDD](02-primeira-nota-e-tdd.md).
