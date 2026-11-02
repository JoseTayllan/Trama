# Trama

**Exportação e painel validados no uso, 01/11/2026:** exportar a página aberta em Markdown com imagens relativas, incluindo o rascunho sem salvá-lo no banco. Os 26 casos aprovados permanecem intactos; a última suíte teve **512 testes aprovados**. Abra o [painel HTML do projeto](roadmap.html) no navegador para acompanhar os marcos; ele é gerado do [roadmap](rodmap.md) por `python tools/build_roadmap.py`. Ver [estudo](docs/estudos/47-exportacao-markdown-e-painel.md) e [relatório](docs/reviews/055-markdown-export-green.md). Revisão explícita do diff e aceite final do módulo permanecem pendentes.

**Listas implementadas para revisão, 31/10/2026:** a barra de formatação oferece lista com marcadores, numerada e texto sem lista por parágrafo. Enter, desfazer/refazer, autosave, reabertura e backup preservam os metadados; **486 testes aprovados**. Páginas com listas usam conteúdo versão 4, mantendo leitura/escrita dos formatos anteriores; sem migração nova. Ver [estudo](docs/estudos/45-listas-implementadas.md), [relatório e limites](docs/reviews/053-paragraph-lists-green.md) e [roadmap](rodmap.md). Revisão do diff e validação de uso pendentes; exportação ainda não implementada.

**Lixeira integrada para revisão, 31/10/2026:** espaços/cadernos/páginas podem ser excluídos e restaurados preservando sua organização e conteúdo. Os 28 casos novos foram aprovados antes de implementar; **454 testes passaram**. Backup/restauração e pacote Windows isolado conferidos. Ver [estudo](docs/estudos/42-lixeira-entrega-integrada.md) e [relatório](docs/reviews/051-library-trash-safety-green.md). Revisão humana do diff e do uso pendente; perfis pessoais e manual/PDF preservados.

**Campos da lateral refinados, para revisão:** criação no “+” dos cabeçalhos Espaço/Caderno e campos no padrão da busca, preservando comandos e recolhimento existentes. Ver [estudo](docs/estudos/37-campos-e-criacao-na-lateral.md) e [relatório](docs/reviews/047-sidebar-fields.md). 426 testes aprovados; revisão estética pendente.

**Refino responsivo disponível para revisão, 31/10/2026:** marcadores alinhados, lateral e formatação recolhíveis, filtros dentro da área rolável, menu Bloco e destaque verde para espaço/caderno/página selecionados. Os 12 casos aprovados e os 414 anteriores passam intactos: **426 testes**. Ver [estudo](docs/estudos/36-refino-responsivo-implementado.md) e [relatório](docs/reviews/046-layout-refinement-green.md). Manual/PDF intactos; revisão estética e do diff pendentes.

**Correção visual disponível para revisão:** a composição foi aproximada da referência apresentada: busca e acervo na lateral, criação recolhida, contexto/Salvar no topo e escrita contínua com margem. Build, formatação, 414 testes e conferência visual passaram; executável Windows isolado conferido. Veja o [estudo](docs/estudos/34-alinhamento-com-a-referencia-visual.md) e o [relatório](docs/reviews/044-reference-alignment.md). A aprovação estética permanece pendente.

Aplicativo desktop pessoal para conectar ideias, estudos, códigos, prints, projetos e rotina, com evolução planejada para um assistente por voz local.

**Identidade e hierarquia implementadas, para revisão:** contexto explícito Espaço → Caderno → Página no topo, lateral com recuos e uma página contínua com margem própria. Título em edição e mudanças de localização acompanham o contexto sem gravar nem recriar o editor; nomes longos têm tooltip e reorganização em janela compacta. Os 15 casos aprovados passam intactos; **414 testes**, build, formatação e conferência SQLite/Skia aprovados. Pacote Windows isolado abriu e fechou normalmente. Ver [estudo](docs/estudos/33-identidade-e-hierarquia-implementadas.md) e [relatório GREEN](docs/reviews/043-notebook-identity-green.md).

**Escrita rica disponível para revisão:** uma barra compartilhada aplica tamanho, cor da letra, marca-texto e limpeza à seleção de prosa, preservando histórico e autosave. Os 25 casos aprovados passam intactos; naquela entrega foram 399 testes aprovados. AvaloniaEdit 11.4.1 corrige a altura com tamanhos mistos. Ver [estudo](docs/estudos/31-controles-de-formatacao-implementados.md) e [relatório GREEN](docs/reviews/041-rich-text-editor-green.md). A composição foi acrescentada na entrega acima; manual/PDF intactos.

**Persistência da nova formatação implementada, para revisão:** tamanho, cor e marca-texto são gravados como conteúdo versão 3, reabertos e preservados em backup/restauração. Versões anteriores válidas continuam aceitas; rótulos inconsistentes são recusados. Os 21 casos aprovados passam intactos; naquela entrega foram 374 testes aprovados. Ver [estudo](docs/estudos/29-persistencia-de-atributos-implementada.md) e [relatório GREEN](docs/reviews/039-rich-text-storage-green.md). Os controles foram acrescentados na entrega acima.

**Modelo de formatação ampliado, para revisão:** tamanho de texto, cor e marca-texto são preservados e validados nos intervalos em memória. Os 28 casos aprovados passam intactos; naquela entrega foram 353 testes, build e formatação aprovados. Ver [implementação](docs/estudos/27-atributos-de-texto-implementados.md) e [relatório GREEN](docs/reviews/037-rich-text-attributes-green.md). Persistência e controles foram acrescentados nas etapas acima. A [direção visual](docs/estudos/26-identidade-e-formatacao-dos-cadernos.md) mantém a hierarquia Espaço → Caderno → Páginas.

**Desfazer/refazer de prosa implementado, para revisão:** Ctrl+Z desfaz texto e estilos; Ctrl+Y/Ctrl+Shift+Z refaz. Histórico separado por bloco e sessão, preservado ao salvar/autosalvar. Os 325 testes passaram; ver [estudo](docs/estudos/25-desfazer-refazer-implementado.md) e [relatório](docs/reviews/034-prose-undo-green.md).

**Salvamento automático implementado, para revisão:** a página é salva após dois segundos sem alterações, preservando o botão Salvar. Build, formatação, 312 testes e conferência com SQLite/Skia passaram. Ver [estudo da implementação](docs/estudos/23-salvamento-automatico-implementado.md) e [relatório](docs/reviews/032-autosave-green.md).

**Atualização de realce, 29/10/2026:** funções e tipos passaram a usar cores próprias, com paleta dinâmica preservada. Os 304 testes passaram no WSL; ver [estudo](docs/estudos/21-realce-de-codigo-e-paletas.md) e [relatório](docs/reviews/030-code-highlighting-green.md). Print Windows/Wayland ainda não chega ao clipboard X11 neste ambiente; correção pendente. O helper WSL preserva os locks Windows após publicação e mantém a restauração Linux em modo travado.

O Trama já possui uma **biblioteca de espaços, cadernos e páginas**, conectada ao SQLite e ao editor de texto, código com sintaxe e imagens com legenda. Você pode criar cadernos por assunto e reabrir páginas pela lista. API HTTP e assistente de IA continuam futuros.

Para estudar as decisões e as implementações, comece pela [central de estudos](docs/estudos/README.md). Ela reúne capítulos com explicações, links para código, exemplos e exercícios de leitura.

O [roadmap e checklist](rodmap.md) mostra as etapas concluídas, em andamento e a iniciar, com os limites reais de cada entrega.

**Estado atual: passo 5 validado no Ubuntu/WSL, disponível para revisão.** Os 298 testes, build, formatação e harness de escrita/imagens passaram. A janela foi iniciada pelo WSLg. A proteção do Windows permanece ativada e o bloqueio da DLL Windows continua pendente. Consulte o [ambiente e comandos](docs/operacao/wsl/README.md) e o [relatório de revisão](docs/reviews/027-wsl-development-green.md).

No editor novo, selecione um trecho e use **N/Ctrl+B** ou **I/Ctrl+I**. Para imagem do clipboard, use **Colar print** ou **Ctrl+Shift+V**; Ctrl+V continua colando texto normalmente. Também é possível arrastar arquivos PNG/JPEG para a página. Imagens entram ao final, com o limite existente de 10 MiB, e seguem o salvamento automático após pausa. Na prosa, Ctrl+Z/Ctrl+Y/Ctrl+Shift+Z restauram texto e estilos; o histórico tem limite de 200 grupos por bloco e reinicia ao reabrir a página.

O passo 4 implementou backup/restauração e correção da busca por tags nos dois campos. Consulte o [material de uso](docs/estudos/18-backup-e-restauracao.md) e o [relatório GREEN](docs/reviews/024-backup-green-e-correcao-tags.md).

O botão **Backup**, no rodapé lateral da biblioteca, cria um pacote `.trama-backup` com os dados salvos e imagens usadas nas páginas. Salve o rascunho antes se quiser incluí-lo. Para restaurar, escolha o pacote e uma pasta nova; a biblioteca atual permanece aberta. Arquivos/pastas existentes não são sobrescritos. Limites iniciais: 1 GiB expandido e 10.000 entradas; o pacote não é criptografado.

Para abrir uma biblioteca restaurada, inicie o Trama com `--data-directory` e seu caminho absoluto. Exemplo a partir da raiz, depois do build Release:

```powershell
dotnet run --project src/Trama.Desktop --configuration Release --no-build -- --data-directory "C:\Backups\Trama-Restaurado"
```

Sem o argumento, o aplicativo continua usando o perfil padrão. O indicador Armazenamento local mostra o caminho ativo no tooltip.

O passo 3 implementou busca/tags. Consulte o [material de estudo e uso](docs/estudos/17-busca-e-tags.md) e o [relatório GREEN](docs/reviews/022-search-tags-green.md).

Use **Buscar no acervo…**, na lateral, e pressione Enter para buscar nas páginas salvas: título, texto, código, legenda e nomes das tags, sem diferenciar maiúsculas ou acentos. Em **Filtros da busca**, o filtro opcional aceita uma tag exata, ignorando caixa. Se os dois campos estiverem preenchidos, ambos os critérios precisam corresponder. Tags salvas como `estudo` podem ser encontradas por `ESTUDO` e vice-versa. Clique num resultado para abrir; alterações pendentes usam Salvar/Descartar/Cancelar. **Fechar busca** recolhe os resultados e mantém a página. O botão **Tags**, junto às ações da página na lateral, abre a lista da página salva: uma tag por linha, vazio remove todas, sem gravar ou descartar seu rascunho. A busca inicial carrega o acervo textual em memória; desempenho em grandes volumes ainda não foi medido.

O passo 2 permite renomear espaços, cadernos e páginas e mover páginas entre cadernos de diferentes espaços. Veja o [material de estudo e uso](docs/estudos/16-gestao-da-biblioteca.md) e o [relatório GREEN](docs/reviews/020-library-management-green.md).

Use **Renomear** ao lado dos títulos de espaços/cadernos, ou **Renomear página** e **Mover para…** acima do editor. As ações de página ficam disponíveis depois do primeiro salvamento. Ao renomear/mover uma página com alterações, escolha salvar, descartar ou cancelar. Depois de mover, a mesma página abre no destino e continua sendo salva ali. Renomear um espaço/caderno mantém o rascunho atual. Excluir itens e mover cadernos inteiros continuam futuros.

A biblioteca visual já protege alterações na navegação e no fechamento normal da janela. O [material do passo 1](docs/estudos/15-cinco-passos-e-protecao-ao-fechar.md) e o [relatório GREEN](docs/reviews/018-window-close-green.md) registram essa implementação e seus limites.

A interface foi refinada e possui uma [base de temas com cores semânticas](docs/estudos/07-temas-e-interface.md). A troca dos recursos atualiza as cores da janela; a tela para o usuário escolher e salvar paletas será implementada futuramente.

A biblioteca começa em Não organizadas. Crie ou selecione um espaço e um caderno; escolha uma página da lista ou use + Nova. Salvar mantém a página aberta e atualiza sua identidade existente. A lista é atualizada após salvar, e páginas de sessões anteriores podem ser reabertas.

O editor anterior continua disponível na composição para compatibilidade. A inicialização da janela agora apresenta o caderno contínuo.

Espaços podem representar Curso de C#, Trabalho, Pesquisa ou qualquer assunto. Cada espaço contém cadernos, e cada caderno contém páginas. Documentos anteriores sem associação continuam em Não organizadas, sem reescrever seu conteúdo.

Se ainda houver alterações ao sair de uma página ou fechar a janela, escolha Salvar e continuar, Descartar ou Cancelar. O autosave fica pausado durante essa decisão. Falha ao salvar impede concluir a operação; falha ao abrir outro documento preserva o editor atual. Durante uma gravação em andamento, o fechamento é bloqueado e pode ser tentado novamente depois. Autosave protege até a última gravação concluída; não há recuperação integral das edições posteriores a ela após encerramento forçado.

**Plano dos cinco passos:** os cinco têm implementação; o quinto passou validação no WSL. Revisão do diff e integração nativa de clipboard/drag permanecem pendentes. Consulte o roadmap.

## Stack

| Componente | Versão configurada |
| --- | --- |
| SDK .NET | `10.0.401`, com `latestPatch` em `global.json` |
| Target framework | `net10.0` |
| Avalonia | `11.3.19` |
| AvaloniaEdit | `11.4.1`, [licença MIT](https://github.com/AvaloniaUI/AvaloniaEdit/blob/11.4.1/LICENSE) |
| CommunityToolkit.Mvvm | `8.4.0` |
| Entity Framework Core e dotnet-ef | `10.0.9` |
| xUnit | `2.9.3` |

As versões dos pacotes ficam centralizadas em `Directory.Packages.props`; locks NuGet fixam a resolução. Desenvolvimento/testes foram validados no Ubuntu-24.04/WSL. Windows continua alvo de distribuição, com execução nativa atualmente bloqueada pela política local.

## Estrutura e responsabilidades

```text
Trama.slnx
src/
  Trama.Domain/           # Limite do domínio, ainda sem regras funcionais
  Trama.Application/      # Contratos e especificação executável da primeira nota
  Trama.Infrastructure/   # Contexto EF Core e factory de design time
  Trama.Desktop/          # Janela Avalonia, ViewModel e composição de dependências
tests/
  Trama.UnitTests/        # Especificação aprovada da primeira nota
  Trama.IntegrationTests/ # Shell headless, recurso da logo e provedor SQLite
assets/brand/             # Identidade visual utilizada pelo desktop
docs/                     # Visão do produto e modelo de documentação
tools/                    # Utilitários de autoria dos documentos
```

`Domain` não depende dos demais projetos. `Application` referencia `Domain`; `Infrastructure` referencia `Application`; `Desktop` referencia a aplicação e a infraestrutura. A infraestrutura só deve ser acessada pelo desktop na composição, mantendo ViewModels separados de arquivos, SQL e provedores externos. A solução ainda não contém um projeto de assistente.

Consulte o [CODEX.md](CODEX.md) antes de alterar código. Ele registra convenções, limites de segurança e o fluxo de TDD com revisão humana.

## Preparar e executar

Instale o SDK .NET compatível com `global.json`. Execute os comandos a partir da raiz do repositório. A primeira restauração exige acesso ao feed de pacotes configurado em `NuGet.Config`.

```powershell
dotnet restore Trama.slnx --locked-mode
dotnet tool restore
dotnet build Trama.slnx --configuration Release --no-restore
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --no-build
```

A execução abre a biblioteca e prepara `%LOCALAPPDATA%\Trama\trama.db` no Windows. Digite o nome de um espaço e clique em +; dentro dele, crie um caderno da mesma forma. Use + Nova para uma página ou clique numa página salva. No editor, + Texto, + Código e + Imagem acrescentam blocos. As setas reorganizam os blocos e × remove o bloco da página. O campo de linguagem aceita `csharp`, `python`, `javascript`, `json` e `sql`; outras entradas ficam sem realce.

Após editar, aguarde dois segundos sem digitar e a confirmação de página salva, ou clique em Salvar página para antecipar. A página permanece aberta e a digitação continua disponível durante autosave; falhas preservam o conteúdo. PNG/JPEG de até 10 MiB são copiados para `%LOCALAPPDATA%\Trama\attachments`. Cancelar a seleção não altera a página. Não há sincronização ou criptografia do futuro cofre. Anexos importados antes de abandonar um rascunho ainda não são coletados automaticamente.

## Testes e estado do TDD

O caso de uso `CreateNote.ExecuteAsync` exige título ou corpo com texto, preserva o conteúdo, gera um identificador e aguarda `INoteRepository`. Falhas de validação e persistência têm estados distintos. O adaptador SQLite implementa a gravação e `INoteReader` implementa a consulta; essa composição foi validada em bancos temporários nos testes, mas não está conectada à interface.

A execução local passou para **GREEN: 14 casos unitários e quatro testes técnicos aprovados**, mantendo os testes aprovados intactos. A suíte usa um repositório controlado para as notas; esses resultados não comprovam gravação no SQLite ou recuperação após reiniciar. O [capítulo da primeira nota](docs/estudos/02-primeira-nota-e-tdd.md) explica cada passo.

Após aprovação dos dez testes do esquema, foi criada a migração `InitialNotes`. Essa entrega ficou **GREEN com 28 casos aprovados**: 14 da aplicação, quatro técnicos e dez do esquema SQLite. Os testes de esquema usam SQL direto e bancos temporários; não comprovam um repositório EF concreto ou criação de notas pela interface. Veja o [relatório dessa entrega](docs/reviews/004-sqlite-schema-green.md) e o [histórico RED](docs/reviews/003-sqlite-schema-red.md).

Após a aprovação dos 14 casos do repositório, o adaptador foi implementado sem alterar os testes. Essa entrega ficou **GREEN com 42 aprovados**; a inicialização posterior elevou a suíte para 50. Consulte o [relatório do repositório](docs/reviews/006-sqlite-repository-green.md), o [histórico RED](docs/reviews/005-sqlite-repository-red.md) e o [capítulo de estudos](docs/estudos/04-repositorio-contratos-e-erros.md).

Após o build Release, execute toda a solução:

```powershell
dotnet test Trama.slnx --configuration Release --no-build
```

Esse comando cobre os 414 casos atuais, aprovados no WSL. Novo pacote Windows isolado abriu nativamente. Feche a janela antes de recompilar; build e testes devem usar a mesma saída.

Para verificar separadamente a fundação técnica:

```powershell
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --no-build
```

Os testes de integração verificam o shell Avalonia em modo headless e o recurso da logo, o acesso ao provedor SQLite em memória e a rejeição de argumentos ausentes ou caminhos relativos pela factory de design time. O SQLite em memória é o provedor real, não EF InMemory, e não cria um banco pessoal em disco. Essa cobertura não demonstra persistência de notas, restauração de backup nem validação visual completa da janela no sistema operacional.

Novas funcionalidades seguem planejamento e testes próprios. Não alterar os testes aprovados para acomodar regressões. O material de estudo e o roadmap distinguem entregas atuais e recursos ainda pendentes.

## Formatação e analisadores

```powershell
dotnet format Trama.slnx --verify-no-changes --no-restore
```

O comando verifica a formatação e os diagnósticos configurados sem alterar arquivos. O build também executa os analisadores habilitados; advertências são tratadas como erros em `Directory.Build.props`. Formatação não substitui testes.

## EF Core sem criar banco pessoal

O `TramaDbContext` mapeia `NoteRecord` para `Notes`, com `Id` como chave primária e as três colunas obrigatórias. A migração `InitialNotes`, seu modelo de destino e o snapshot estão em `src/Trama.Infrastructure/Persistence/Migrations`. A factory exige exatamente `--database` seguido de caminho absoluto, evitando escolher silenciosamente o banco do usuário.

Para listar migrações **sem conectar ao banco**, depois de restaurar a ferramenta local e compilar em Release:

```powershell
dotnet tool restore
$developmentDatabase = Join-Path (Get-Location).Path 'artifacts/development.db'
dotnet ef migrations list --no-connect --project src/Trama.Infrastructure --startup-project src/Trama.Desktop --configuration Release --no-build -- --database "$developmentDatabase"
```

Executado na raiz, `$developmentDatabase` é um caminho absoluto; os argumentos após `--` chegam à factory. A listagem com `--no-connect` não cria nem atualiza o banco. O desktop agora chama LocalDatabase e aplica as migrações conhecidas no caminho padrão. A primeira versão e sua reaplicação foram testadas; futuras migrações precisam de validação própria. O rollback `Down` remove a tabela e seus dados; não aplicá-lo sobre dados pessoais sem autorização e backup recuperável.

## Integração contínua

O arquivo `.github/workflows/ci.yml` configura restauração com lock, build, formatação, execução da suíte e publicação dos resultados de testes em Windows. **O workflow ainda não foi executado no GitHub.** A validação local final está impedida pela política Windows descrita acima.

## Segurança e limites desta entrega

- Não adicionar chaves, tokens ou senhas ao código, aos arquivos versionados ou aos logs. Integrações futuras recebem configuração sensível por variáveis de ambiente.
- A fundação não inclui cofre de credenciais nem criptografia de dados pessoais. SQLite comum não torna o banco criptografado automaticamente.
- Não usar dados reais do usuário em testes e não conectar banco pessoal ao tooling de desenvolvimento.
- Não considerar uma entrega de software concluída sem as verificações aplicáveis e revisão humana do diff. Resultados locais, falhas conhecidas e riscos devem acompanhar cada solicitação de revisão.

Os documentos Word em `docs/` preservam a visão mais ampla do produto, incluindo estudos, pesquisa científica, agenda, finanças, cofre e assistente. Esses módulos são planejamento, não funcionalidades já entregues pela fundação.

Os procedimentos de WSL e de criação/compartilhamento do exe estão na [pasta de operação](docs/operacao/README.md). O [manual PDF para o colega](output/pdf/Trama_Guia_de_Execucao_Windows.pdf) explica extração, abertura e atalho.

## Continuar agora pelo WSL

**Teste posterior no Windows:** o pacote Release self-contained abriu sem WSL e o usuário confirmou Pronto, com perfil de teste separado. Ver [publicação e roteiro do exe](docs/reviews/028-windows-exe-smoke-test.md). A assinatura e a distribuição ainda precisam de validação.

Se estiver no Git Bash (`MINGW64`), primeiro entre no Ubuntu com `wsl.exe -d Ubuntu-24.04 -u barberdev`. Dentro dele, execute `cd /mnt/c/Users/joset/www/Trama`, `code .` para abrir o VS Code conectado ao WSL e `bash tools/run-wsl.sh run` para iniciar. O [passo a passo completo](docs/operacao/wsl/README.md) explica a diferença entre os terminais e o erro de rsync.

No PowerShell, para abrir o Trama:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh run
```

Troque `run` por `test` para build, formatação e os 414 testes. Continue editando em `C:\Users\joset\www\Trama`. O perfil Linux é separado e começa sem as notas Windows; os dados originais não foram copiados nem alterados. Feche a janela antes de recompilar. Preparação e limites no [guia WSL](docs/operacao/wsl/README.md).
