# Primeira nota e o ciclo TDD

## Objetivo desta leitura

Entender como a especificação aprovada virou um caso de uso C#, por que ele depende de uma interface de armazenamento e o que os testes realmente demonstram. Leia primeiro [a arquitetura e a fundação](01-arquitetura-e-fundacao.md).

Ao terminar, você deverá conseguir acompanhar uma execução de `CreateNote`, explicar seus três resultados e distinguir criação na camada de aplicação de armazenamento real em disco.

## O problema resolvido

A captura rápida não deve obrigar você a escolher projeto ou tags. É possível registrar uma ideia só com título, só com corpo ou com ambos. Entretanto, uma nota inteiramente vazia não tem informação útil.

O contrato aprovado define:

- Pelo menos um dos campos precisa conter texto além de espaços, tabs ou quebras de linha.
- O texto informado deve ser preservado, inclusive indentação de código e espaços nas extremidades.
- No sucesso, retornar uma nota com identificador não vazio e solicitar uma única gravação ao repositório.
- Na entrada inválida, retornar falha de validação sem tentar gravar.
- Na falha de armazenamento contratada, retornar falha de persistência sem anunciar sucesso.

Esse recorte ainda não inclui um formulário de edição nem uma tabela de notas no SQLite. O desktop continua mostrando a tela inicial de fundação.

## Onde cada peça está

Os links apontam para os arquivos reais do projeto:

| Arquivo | Papel |
| --- | --- |
| [CreateNoteRequest.cs](../../src/Trama.Application/Knowledge/CreateNoteRequest.cs) | Entrada com título e corpo opcionais |
| [CreateNote.cs](../../src/Trama.Application/Knowledge/CreateNote.cs) | Coordenação do caso de uso |
| [NoteSnapshot.cs](../../src/Trama.Application/Knowledge/NoteSnapshot.cs) | Representação imutável da nota retornada |
| [CreateNoteResult.cs](../../src/Trama.Application/Knowledge/CreateNoteResult.cs) | Resultado e estados possíveis |
| [INoteRepository.cs](../../src/Trama.Application/Knowledge/INoteRepository.cs) | Contrato de gravação |
| [NotePersistenceException.cs](../../src/Trama.Application/Knowledge/NotePersistenceException.cs) | Falha de armazenamento reconhecida pela aplicação |
| [CreateNoteTests.cs](../../tests/Trama.UnitTests/Knowledge/CreateNoteTests.cs) | Especificação executável aprovada |

## Como a operação funciona

```text
CreateNoteRequest
    |
    v
Verificar request e cancelamento
    |
    v
Título e corpo estão ambos em branco?
    | sim                           | não
    v                               v
ValidationFailed                Criar NoteSnapshot
Sem tentativa de gravação            |
                                    v
                           await repository.AddAsync
                                    |
                     +--------------+----------------+
                     |                               |
              Gravação concluída           NotePersistenceException
                     |                               |
                     v                               v
                  Created                    PersistenceFailed
                 Com nota                      Sem nota
```

Outros erros e cancelamentos não são convertidos em sucesso nem em falhas de persistência genéricas. Eles propagam para quem chamou a operação.

### Guardas de entrada

`ArgumentNullException.ThrowIfNull(request)` trata um erro de programação: não foi fornecido o objeto da requisição. Isso é diferente de uma requisição válida como objeto, mas com ambos os campos vazios, que produz `ValidationFailed`.

`CancellationToken` permite que quem iniciou a operação solicite cancelamento. O caso de uso verifica o cancelamento antes de começar e repassa o token ao repositório. Não existe captura de `OperationCanceledException` que disfarce cancelamento como erro de armazenamento.

Essas guardas fazem parte do comportamento defensivo atual; os 14 testes aprovados não medem todos os caminhos de cancelamento ou argumentos nulos como objeto. Não confundir existência do código com cobertura comprovada de todos os ramos.

### Validar sem modificar o texto

A expressão central é:

```csharp
string.IsNullOrWhiteSpace(request.Title)
    && string.IsNullOrWhiteSpace(request.Body)
```

O operador `&&` exige que os dois campos estejam em branco para rejeitar a nota. Com `||`, uma nota com título e corpo vazio seria rejeitada, contrariando a captura rápida.

`IsNullOrWhiteSpace` decide se existe informação; ele não altera o conteúdo. Não usamos `Trim()` ao construir a nota porque removeria espaços que o usuário pode querer preservar, principalmente em código.

Como `NoteSnapshot` usa strings não nulas, um campo nulo é representado por `string.Empty` quando o outro campo contém texto. Esse detalhe atual ainda não possui teste específico de sucesso com um campo nulo. Valores textuais existentes são mantidos exatamente.

### Identidade e dados de saída

`Guid.NewGuid()` atribui uma identidade à nova nota sem consultar um banco. Identidade não é senha nem mecanismo de autorização. Gerar um ID também não comprova gravação.

`NoteSnapshot` é um `record`: nesta implementação, seus dados são inicializados por construtor e os testes podem comparar o conteúdo por valor. Ele representa os dados dessa operação; ainda não existe uma entidade de domínio com ciclo completo de edição, exclusão e histórico.

### Por que depender de um repositório

O construtor recebe `INoteRepository`, e o guarda em `_repository`. A aplicação sabe que precisa solicitar uma gravação; não precisa saber se a implementação usa SQLite ou outro mecanismo.

Nos testes, `RecordingNoteRepository` guarda as tentativas em uma lista. Essa lista é um recurso de teste, não o banco do produto. Ela permite verificar quantidade e conteúdo das chamadas sem rede, arquivos pessoais ou interface gráfica.

Na futura persistência, um adaptador em Infrastructure implementará esse contrato. Ele terá de traduzir falhas esperadas do armazenamento para `NotePersistenceException`, manter integridade e não vazar detalhes sensíveis.

### Esperar antes de anunciar sucesso

O resultado `Created` é retornado depois do `await _repository.AddAsync(...)`. Se retornássemos antes da conclusão, a interface poderia dizer que salvou mesmo quando a gravação falhasse.

O `catch` captura somente `NotePersistenceException`. Capturar qualquer `Exception` apagaria diferenças importantes entre uma falha esperada, cancelamento e um defeito de programação.

Uma falha retornada não garante, sozinha, rollback de qualquer efeito externo parcial. Atomicidade e recuperação precisam ser implementadas e testadas no adaptador real. O contrato atual de teste apenas garante que não retornamos uma nota criada quando a gravação contratada falha.

## O que aprendemos com o RED e o GREEN

No RED anterior, havia um esqueleto que lançava `NotImplementedException`. O runner encontrou e executou os 14 casos; todos paravam nessa exceção. Era evidência de operação ausente, sem alcançar as asserções específicas de cada regra.

Após sua aprovação, a implementação foi adicionada sem modificar os testes. No GREEN, os 14 casos passaram e passaram a alcançar suas verificações de resultado e chamadas ao repositório. Os quatro testes técnicos também continuaram passando.

| Etapa | Evidência | Limite |
| --- | --- | --- |
| RED | 14 casos executados e falhando na operação ausente | Não demonstrava cada regra individualmente |
| Aprovação | Autorização do responsável antes da implementação | Não autorizava antecipar interface ou persistência real |
| GREEN | 14 casos aprovados sem mudanças nos testes | Usa repositório controlado, sem armazenamento real |
| Regressão técnica | 4 testes de fundação aprovados | Não comprova experiência completa de criação pelo desktop |

## Como reproduzir a verificação

Na raiz do projeto, com o SDK de `global.json` instalado:

```powershell
dotnet restore Trama.slnx --locked-mode
dotnet build Trama.slnx --configuration Release --no-restore
dotnet test tests/Trama.UnitTests/Trama.UnitTests.csproj --configuration Release --no-build
dotnet test Trama.slnx --configuration Release --no-build
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Feche a janela do Trama antes de recompilar caso o Windows informe que o executável está em uso. Esse bloqueio é de arquivo, não falha de regra de negócio. `--no-build` só deve ser usado depois de compilar a versão atual; caso contrário, você pode testar binários antigos.

## Exercícios de leitura

1. Acompanhe uma requisição com título vazio e corpo preenchido. Qual condição permite continuar?
2. Localize nos testes a verificação que impede alterar a indentação de código.
3. Explique por que o repositório não é chamado para uma nota inteiramente vazia.
4. Identifique por que `Created` precisa vir depois do `await`.
5. Imagine que o repositório lançasse uma exceção inesperada. Por que a aplicação não deveria escondê-la?
6. Explique por que os testes atuais não comprovam que a nota sobreviverá à reabertura do aplicativo.

São exercícios opcionais de estudo. Não altere os testes aprovados para experimentar sem separar a mudança e manter uma base recuperável.

## Próxima aprendizagem

A próxima fatia deverá ligar o contrato à persistência SQLite, modelar os dados, definir o diretório pessoal e testar recuperação após reabertura. A interface de criação virá com seus próprios estados e critérios de aceite. Esses passos ainda dependem do planejamento e do ciclo de testes correspondentes.

Retorne ao [índice de estudos](README.md).
