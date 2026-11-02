# Trama

Aplicativo desktop para organizar anotações, estudos e código, com armazenamento local.

[Documentação](docs/estudos/README.md) · [Roadmap](rodmap.md) · [Guias de execução](docs/operacao/README.md)

## Sobre

Criei o Trama para reunir minhas anotações e materiais de estudo em uma ferramenta própria. O projeto está em desenvolvimento, com o módulo de cadernos funcionando no Windows e no Linux/WSL.

A organização segue três níveis: **espaço → caderno → página**. Cada página pode combinar texto, código e imagens.

## Funcionalidades

- Editor com formatação de texto, listas e destaque de sintaxe para código.
- Imagens, prints e legendas dentro das páginas.
- Busca por conteúdo e tags.
- Salvamento automático e histórico de desfazer/refazer durante a edição.
- Lixeira com restauração de espaços, cadernos e páginas.
- Exportação em Markdown com imagens e backup da biblioteca.

## Stack

**C# / .NET 10** · **Avalonia** · **CommunityToolkit.Mvvm** · **EF Core** · **SQLite** · **AvaloniaEdit** · **xUnit**

## Como rodar

Instale o SDK definido no [global.json](global.json) e execute na raiz do projeto:

```powershell
dotnet restore Trama.slnx --locked-mode
dotnet run --project src/Trama.Desktop --configuration Release
```

O banco é criado na primeira execução. Os dados ficam no diretório local do usuário.

Para desenvolvimento pelo WSL ou geração do executável Windows, consulte os [guias de operação](docs/operacao/README.md).

## Verificações

Depois da restauração das dependências:

```powershell
dotnet build Trama.slnx --configuration Release --no-restore
dotnet test Trama.slnx --configuration Release --no-build
dotnet format Trama.slnx --verify-no-changes --no-restore
```

## Organização do código

```text
src/
├── Trama.Domain
├── Trama.Application
├── Trama.Infrastructure
└── Trama.Desktop
tests/
├── Trama.UnitTests
└── Trama.IntegrationTests
```

Casos de uso e contratos ficam na Application; persistência e arquivos, na Infrastructure; interface e ViewModels, no Desktop. A Domain é a base para as regras de domínio dos próximos módulos.

## Próximas etapas

O próximo módulo planejado é a agenda semanal, com tarefas de trabalho e estudo e comparação entre o planejado e o realizado.

O [roadmap](rodmap.md) reúne o escopo e o andamento das entregas. As decisões de implementação estão na [documentação de estudos](docs/estudos/README.md).

---

**JoseTayllan**
