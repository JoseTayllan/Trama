# Trama

Comecei o Trama porque minhas anotações de estudo, trechos de código e prints estavam espalhados entre arquivos de texto e outras ferramentas. Queria reunir esse material em um lugar meu, organizado por assunto e funcionando no meu computador.

A primeira parte do projeto é um caderno digital. A ideia é que ele cresça aos poucos para ajudar também na organização da rotina, do trabalho e dos estudos.

## O que já funciona

- Organização em espaços, cadernos e páginas.
- Texto com negrito, itálico, tamanhos, cores, marca-texto e listas.
- Blocos de código com destaque de sintaxe.
- Imagens e prints nas páginas, com legendas.
- Busca por conteúdo e tags.
- Salvamento automático e desfazer/refazer na edição de texto.
- Lixeira para espaços, cadernos e páginas, com restauração.
- Exportação da página aberta em Markdown com imagens.
- Backup e restauração dos dados locais.

Na exportação Markdown, o texto, as listas e a ênfase são preservados. Cores, tamanhos e marca-texto ficam no Trama. Para recuperar a biblioteca completa, uso o backup.

## Tecnologias

Escolhi C# como base do aplicativo, com:

- .NET 10 e Avalonia para o desktop.
- CommunityToolkit.Mvvm para os ViewModels.
- EF Core e SQLite para persistência local.
- AvaloniaEdit para os editores de texto e código.
- xUnit para testes.

Python aparece nas ferramentas de documentação e na geração do painel do projeto.

## Executar o projeto

É necessário ter o SDK do .NET indicado em [global.json](global.json). A versão configurada é a 10.0.401.

Na raiz do projeto:

```powershell
dotnet restore Trama.slnx --locked-mode
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release
```

O aplicativo prepara o banco ao iniciar e guarda os dados no diretório local do usuário, fora do código-fonte. Não é preciso configurar um servidor de banco.

Também uso Ubuntu/WSL no desenvolvimento. A preparação do ambiente está no [guia de WSL](docs/operacao/wsl/README.md). Depois de configurar o ambiente, execute na raiz do projeto, pelo terminal Linux:

```bash
bash tools/run-wsl.sh run
```

Os perfis de dados do Windows e do Linux são separados. O espelho de desenvolvimento do WSL também fica separado dos arquivos que edito no Windows.

Para gerar e compartilhar o executável, veja o [guia de publicação para Windows](docs/operacao/windows/README.md).

## Build, testes e formatação

Depois de restaurar as dependências:

```powershell
dotnet build Trama.slnx --configuration Release --no-restore
dotnet test Trama.slnx --configuration Release --no-build
dotnet format Trama.slnx --verify-no-changes --no-restore
```

No ambiente WSL preparado, o comando abaixo reúne essas verificações:

```bash
bash tools/run-wsl.sh test
```

Os testes incluem regras da aplicação, persistência em SQLite, arquivos e comportamento da interface em modo headless. Uso dados temporários nos testes, separados da biblioteca pessoal.

## Estrutura

```text
src/
  Trama.Domain/          Base para os tipos e regras do domínio
  Trama.Application/     Casos de uso, modelos e contratos
  Trama.Infrastructure/  SQLite, arquivos, imagens e backup
  Trama.Desktop/         Interface Avalonia e ViewModels
tests/                   Testes unitários e de integração
docs/                    Estudos, operação e histórico técnico
tools/                   Scripts de apoio
```

## Acompanhar o desenvolvimento

O [painel do projeto](roadmap.html) mostra as entregas e o que falta fazer. Ele abre no navegador, sem servidor, e é gerado a partir do [roadmap em Markdown](rodmap.md).

Para atualizar o HTML depois de alterar o roadmap:

```powershell
python tools/build_roadmap.py
```

Registro as explicações das implementações na [central de estudos](docs/estudos/README.md), para poder revisitar as decisões e estudar o código depois. Os comandos e as convenções de desenvolvimento ficam no [CODEX.md](CODEX.md).

Os cadernos e a exportação já foram validados no Windows e no Linux/WSL. O próximo módulo proposto é a agenda semanal, com tarefas e acompanhamento do planejado e do realizado. Mais à frente, quero explorar um assistente por voz local e aplicar meus estudos de Machine Learning. Essas partes ainda não estão implementadas.

---

Criado por **JoseTayllan**.
