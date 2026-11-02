# Trama — guia de arquitetura e desenvolvimento

Data da análise: 27 de outubro de 2026.

**Lixeira integrada, 31/10/2026, para revisão:** 18 fluxos e dez casos complementares aprovados antes de implementar, preservados, com 454 testes passando. Sexta migração adiciona marcações por lote sem remover conteúdo. Preservar exclusões anteriores ao restaurar pais, bloquear snapshots atrasados e validar também conteúdo excluído em backups. Interface, migração, backup/restauração e pacote Windows isolado conferidos em perfil fictício. Não aplicar migrações/testes ao perfil pessoal como verificação. Ver capítulo 42 e relatório 051. Revisão humana final pendente; manual/PDF intactos.

**Refino responsivo, 31/10/2026, para revisão:** os 12 casos aprovados e os 414 anteriores permanecem intactos, com 426 aprovados. Marcadores de todos os blocos compartilham a margem; lateral/formatação são recolhíveis; filtros e listas compartilham rolagem; menu Bloco reutiliza os comandos atuais. Espaço/caderno/página ativos têm destaque da paleta verde-água. Preservar a instância da prosa, seleção e histórico ao recolher ou redimensionar. Não persistir estados visuais como conteúdo da página. Ver `docs/estudos/36-refino-responsivo-implementado.md` e `docs/reviews/046-layout-refinement-green.md`; revisão estética/do diff pendente, manual/PDF intactos.

**Identidade e hierarquia implementadas, 30/10/2026, para revisão:** 15 casos aprovados intactos e suíte completa com 414 aprovados; build/formatação passaram. Contexto superior explícito Espaço → Caderno → Página, título vivo, estados vazios e tooltips; lateral com recuos, página contínua com margem e títulos/status adaptáveis. Contexto deriva da seleção/editor, sem gravação ou recriação do editor. Harness SQLite/Skia e capturas ampla/compacta/anexo/nomes longos conferidos. Pacote Windows isolado abriu, respondeu e fechou em perfil sintético. Sem migração ou mudança de dependências; manual/PDF intactos. Ver `docs/estudos/33-identidade-e-hierarquia-implementadas.md` e `docs/reviews/043-notebook-identity-green.md`.

**Escrita rica implementada, 30/10/2026, para revisão:** 25 casos aprovados e preservados; 399 testes, build e formatação passaram. Barra compartilhada de tamanho/cor/marca-texto/limpeza, estados mistos, seleção/foco, histórico e renderer conectados. Harness SQLite/Skia confirmou popup, autosave/reabertura, recursos dinâmicos e layout amplo/compacto. AvaloniaEdit atualizado para 11.4.1 para corrigir sobreposição com tamanhos mistos, mantendo Avalonia 11.3.19 e demais pacotes; locks Windows preservados. Pacote win-x64 isolado abriu nativamente e fechou normalmente em perfil sintético. Ver `docs/estudos/31-controles-de-formatacao-implementados.md` e `docs/reviews/041-rich-text-editor-green.md`. A composição visual completa com hierarquia Espaço → Caderno → Páginas continua como próxima etapa; manual/PDF congelado.

**Persistência de atributos implementada, 30/10/2026, para revisão:** 21 casos e regras de versões aprovados, preservados e GREEN; suíte com 374 aprovados, build sem avisos/erros e formatação aprovada. Repositório usa páginas simples 1, somente flags 2 e atributos novos 3; pacote de backup permanece 1. Leitura valida versão contra o conteúdo e rejeita atributos enriquecidos rotulados como 1/2; backup reutiliza essa validação. Sem migração nova. Checagem externa confirmou leitura/gravação sem tabela de estilos e atualização posterior preservando página. Controles visuais continuam pendentes. Ver `docs/estudos/29-persistencia-de-atributos-implementada.md` e `docs/reviews/039-rich-text-storage-green.md`.

**Atributos de texto implementados no modelo, 30/10/2026, para revisão:** os 28 casos aprovados passam intactos; suíte completa com 353 aprovados, build sem avisos/erros e formatação aprovada no WSL. `TextStyleSpan` recebe propriedades opcionais de tamanho/cor/destaque sem mudar construtor/deconstrução existentes; edição e canonicalização preservam todos os atributos, com validação. Campos nulos são omitidos no JSON para preservar o formato legado. Ainda sem controles visuais ou versionamento/persistência dos novos atributos; não disponibilizar edição rica na interface antes desses testes e proteções. Ver `docs/estudos/27-atributos-de-texto-implementados.md` e `docs/reviews/037-rich-text-attributes-green.md`. A direção visual mantém página contínua e hierarquia explícita Espaço → Caderno → Páginas. Manual/PDF permanecem congelados até solicitação.

**Desfazer/refazer implementado, para revisão, 29/10/2026:** treze casos aprovados e preservados; 325 testes, build, formatação e harness SQLite/Skia passaram. Prosa usa a pilha nativa para texto/estilos, com replay sem nova edição normal, isolamento por bloco/página, atalhos Ctrl+Z/Ctrl+Y/Ctrl+Shift+Z e limite de 200 grupos. Salvar/autosave mantém o histórico da página aberta; reabertura inicia pilha limpa. Sem migração, pacotes novos ou alteração de manual/PDF. Ver `docs/reviews/034-prose-undo-green.md` e `docs/estudos/25-desfazer-refazer-implementado.md`.

**Autosave implementado, para revisão, 29/10/2026:** oito casos aprovados e preservados; 312 testes, build, formatação e harness SQLite/Skia passaram. App ativa gravação após dois segundos de pausa via composição; botão Salvar permanece. Preservar revisão mais recente, foco, serialização, pausa durante confirmação, descarte/cancelamento dos editores antigos e destino após movimentação. Assinaturas anteriores mantêm o modo manual. Sem migração nova. Print Wayland/WSL permanece em baixa prioridade. Ver `docs/reviews/032-autosave-green.md` e `docs/estudos/23-salvamento-automatico-implementado.md`.

**Realce de código, 29/10/2026:** seis regressões aprovadas e preservadas; diferenciação de funções/tipos por brushes semânticos, observação de mutação e DynamicResource. Build, format, 304 testes e harness visual aprovados no WSL. O helper projeta somente o grafo genérico dos locks no espelho Linux, sem mudar versões/hashes nem locks Windows, e mantém --locked-mode; exige Python 3. Diagnóstico: print chega ao Wayland como image/bmp, mas X11 não anuncia imagem; correção pendente em novo TDD. Ver `docs/reviews/030-code-highlighting-green.md` e `docs/estudos/21-realce-de-codigo-e-paletas.md`.

**Publicação Windows posterior, 29/10/2026:** pacote Release win-x64 self-contained abriu nativamente, criou banco isolado com cinco migrações e o usuário confirmou Pronto. Não houve alteração de política; o bloqueio histórico abaixo não se reproduziu nesse pacote. Assinatura/distribuição e aceitação de builds futuros continuam pendentes. Ver `docs/reviews/028-windows-exe-smoke-test.md`.

**Implementação atual em revisão, 29/10/2026:** passo 5 implementado após aprovação dos 28 testes. Validação final no Ubuntu/WSL: 298/298 testes, build Release sem avisos/erros, formatação e harness de escrita/imagens aprovados. Janela iniciada pelo WSLg. A política Windows ainda bloqueia a DLL nativa; não desativar a proteção automaticamente. Ver `docs/reviews/027-wsl-development-green.md` e `docs/operacao/wsl/README.md`. Diff e integração nativa permanecem para revisão.

**Etapa anterior com GREEN confirmado:** passo 4, backup/restauração, após aprovação dos 34 testes e dos limites. Painel Backup conectado, snapshot SQLite com imagens referenciadas, manifesto/hashes e restauração em pasta nova. DataDirectoryOptions está ligado ao App para abrir explicitamente um perfil restaurado. Também corrigida a busca geral para incluir tags após aprovação de seis regressões de caixa. Naquela entrega, os 270 testes passaram intactos. Consultar `docs/reviews/024-backup-green-e-correcao-tags.md` e `docs/estudos/18-backup-e-restauracao.md`.

**Etapa anterior:** passo 3, busca e tags, no GREEN com 230 casos naquela entrega. `IKnowledgeDiscovery` está registrado na composição; `SqliteKnowledgeDiscovery` usa contextos por operação e a migração PageTags preserva as três anteriores. Consultar `docs/reviews/022-search-tags-green.md` e `docs/estudos/17-busca-e-tags.md`. A correção posterior inclui tags na consulta geral, mantendo o filtro exato.

**Etapa anterior:** passo 2, gestão da biblioteca, no GREEN com 200 casos naquela entrega. `ILibraryManagementActions` mantém a gestão separada de `ILibraryActions`; `SqliteLibraryManagement` usa contexto por operação. A biblioteca oferece renomear espaços/cadernos/páginas e mover páginas entre cadernos/espaços. Leituras e preparação do editor precedem a mutação; após mover, o novo editor salva no destino. Consultar `docs/reviews/020-library-management-green.md`.

**Etapa anterior:** proteção ao fechar, passo 1 dos cinco solicitados, no GREEN com 171 casos naquela entrega. MainWindow aguarda RequestCloseAsync, mantém edição bloqueada e só emite fechamento autorizado após a decisão. Cancelar ou falhar a gravação mantém a janela; uma edição posterior ao snapshot impede fechar. Consultar `docs/reviews/018-window-close-green.md`.

**Etapa anterior:** biblioteca visual conectada com 157 testes naquela entrega. Criação de espaços/cadernos, abertura de páginas e confirmação de navegação foram implementadas por ILibraryActions e coordenação transacional. Consultar `docs/reviews/016-library-navigation-green.md` para o histórico. A confirmação de fechamento foi acrescentada na etapa atual; salvamento continua manual.

**Etapa anterior:** catálogo persistente Espaço → Cadernos → Páginas no GREEN com 126 testes naquela entrega. KnowledgeCatalog adicionou os metadados. Consultar `docs/reviews/014-knowledge-catalog-green.md` para o histórico. O repositório não deve ser injetado diretamente em ViewModels; a navegação posterior usa ILibraryActions.

**Etapa anterior:** editor contínuo conectado à janela, com 113 testes naquela entrega. NotebookEditorViewModel usa INotebookActions; SqliteNotebookActions cria e descarta um contexto por operação. Salvar mantém a página aberta e a mesma identidade. Texto, código e imagens com legenda estão conectados. AvaloniaEdit 11.3.0 fornece realce de sintaxe com cores semânticas. Consultar `docs/reviews/012-notebook-editor-green.md`. Lista visual de páginas e autosave continuam pendentes.

**Etapa anterior:** persistência do caderno e anexos passou ao GREEN com 90 casos naquela entrega. NotebookPages adicionou páginas e blocos em tabelas próprias, preservando Notes. O armazenamento copia PNG/JPEG de até 10 MiB e reconhece cabeçalhos; a apresentação posterior decodifica as prévias. Consultar `docs/reviews/010-notebook-storage-green.md` para o histórico.

**Etapa anterior:** NotebookDraft passou ao GREEN após aprovação dos 15 casos novos, sem alterar os testes. Naquela entrega, a suíte tinha 72 aprovados. A estrutura gerencia blocos em memória; a persistência posterior está documentada acima. Consultar `docs/reviews/008-notebook-draft-green.md` para o histórico.

## 1. Estado do repositório

**Aceite de uso, 01/11/2026:** o responsável validou exportação e painel HTML, além das listas e da lixeira anteriormente. Registrar esses recursos como validados no uso; não confundir essa confirmação com revisão explícita do diff ou certificação de ambos os ambientes. A última suíte continua sendo a de 512 aprovados da implementação, sem nova execução nesta atualização documental. Ver roadmap e atualização do relatório 055.

**Confirmação dos ambientes, 01/11/2026:** o responsável confirmou uso validado em Windows e Linux/WSL. A pendência de validação nos dois ambientes foi atendida; somente a revisão explícita do diff impede o encerramento formal desta versão. Não apresentar a suíte histórica de 512 aprovados como uma nova execução nesta atualização de status.

**Exportação e painel, 01/11/2026, para revisão:** 26 casos aprovados previamente e preservados; 512 testes aprovados no WSL. `IMarkdownPageExporter` recebe snapshot e leitor de imagens; formatter na Application, escrita/staging na Infrastructure e estado/seleção no Desktop. Não gravar no banco nem sobrescrever destino para exportar. Preservar rascunho/histórico e descarte/cancelamento de streams. Sem migração/dependência nova; versões da página 1/2/3/4 e backup 1 mantidas. Composição também injeta o exportador nos editores recriados pela biblioteca. `roadmap.html` é gerado de `rodmap.md`: atualizar por `python tools/build_roadmap.py`, layout em `tools/roadmap.template.html`; não editar o gerado diretamente. Notas pessoais do painel não alteram status oficiais. Ver capítulo 47 e relatório 055. Uso Windows e revisão explícita do diff pendentes.

**Listas, 31/10/2026, para revisão:** 32 casos e versão 4 aprovados previamente; testes intactos, suíte com 486 aprovados no WSL. `ParagraphListDocument` valida e transforma metadados por início de parágrafo UTF-16; nunca inserir marcadores artificiais no texto salvo. Preservar listas em snapshots, histórico, edição, autosave e backup. `NotebookTextStyles.Styles` mantém arrays antigos sem listas e recebe envelope de Styles/Lists somente com listas; sem migração nova. Páginas com listas exigem 4; sem listas permanecem 1/2/3 segundo os atributos, pacote de backup continua 1. Leitura deve validar conteúdo versus versão. Ver capítulo 45 e relatório 053; revisão humana e uso nativo pendentes.

O desktop inicializa o banco em LocalApplicationData/Trama, ou no perfil absoluto selecionado por --data-directory, e apresenta biblioteca, editor contínuo, SQLite, imagens, busca/tags, autosave, listas, exportação Markdown e backup restaurável. Existem quatro projetos de produção e dois de testes; a suíte contém 512 casos aprovados no Linux/WSL. Listas foram validadas no uso; o pacote Windows da exportação foi preparado para validação separada. Assinatura/distribuição continuam pendentes. A biblioteca protege alterações ao navegar, abrir resultados, renomear/mover páginas e no fechamento normal. Não existem API HTTP, assistente ou cofre.

App prepara DesktopComposition após a janela abrir e apresenta falhas esperadas sem fallback de caminho. SqliteNoteService mantém somente opções e cria/descarta um contexto por operação; ViewModels recebem ICreateNote, nunca DbContext. Não usar um contexto singleton nem guardar entidade rastreada na interface.

Existem `global.json`, `.editorconfig`, versões centralizadas em `Directory.Packages.props`, locks NuGet e manifesto local de `dotnet-ef`. O workflow de CI foi configurado, mas não executado no GitHub. A pasta não contém `.git` nem `AGENTS.md`. Não inferir implementação funcional a partir de contratos ou pacotes instalados.

Evoluções de interface precisam de especificação e revisão TDD próprias. Preservar confirmação antes de abandonar rascunho na navegação, espera pela persistência, bloqueio durante operações e manutenção do conteúdo nas falhas. Os repositórios de página e catálogo participam de transação já aberta pelo coordenador; somente o dono da transação pode confirmá-la ou descartá-la.

Renomear altera somente o título e mantém a identidade. Páginas estruturadas prevalecem sobre Notes com o mesmo ID; não sobrescrever a nota histórica ao renomear a versão atual. Renomear espaço/caderno preserva o editor e o rascunho sem confirmação. Para renomear/mover a página persistida, resolver alterações pendentes antes; páginas novas precisam ser salvas primeiro. Depois de mover, nunca reutilizar um editor que ainda capture o caderno anterior. Preparar as leituras antes de gravar e publicar o novo estado visual somente após sucesso.

O contexto superior é informativo: derivar espaço/caderno da seleção por ID e página do título atual do editor, com notificações das propriedades derivadas. Atualizar contexto não grava, não inicia outra navegação nem substitui o editor; cancelamento/falha mantém o caminho do conteúdo realmente aberto. Preservar estados explícitos para não organizadas e espaço sem caderno. Manter nomes longos acessíveis por tooltip e reflow em janela compacta.

Na busca, normalizar somente para comparação, sem alterar o conteúdo original. A primeira versão carrega o acervo textual em memória e não tem benchmark de grandes volumes. Resultados são uma fotografia da consulta: revalidar a localização antes de abrir, respeitando o rascunho. Tags usam trim/NFC/minúsculas via PageTags e associação por ID; substituir a lista em transação. Como o ID também pode pertencer a Notes, a existência é validada pela aplicação de persistência, sem FK exclusiva para NotebookPages. Uma futura exclusão precisa tratar também essas associações. Não misturar cofre ou outros módulos futuros à busca geral.

A consulta geral inclui nomes das tags, além dos campos textuais. O filtro opcional permanece exato e ignora caixa; quando preenchido junto com o texto, ambos os critérios devem corresponder. Não regredir os seis casos TagCaseRegressionTests aprovados.

Estilos inline são intervalos UTF-16 canônicos de InlineTextDocument; preservar conteúdo puro, flags e atributos opcionais, validar antes de persistir e atualizar posições ao editar. NotebookTextStyles guarda metadados separados com FK ao bloco. Sem listas, páginas sem estilos usam conteúdo 1; somente negrito/itálico usam 2; tamanho/cor/destaque explícitos exigem 3. Qualquer lista exige 4. Validar a correspondência entre rótulo e conteúdo, inclusive metadados indevidos em páginas 1; preservar leitura/escrita simples nos esquemas sem tabela de estilos. Backup usa o mesmo leitor para validar páginas. O editor de prosa precisa herdar o StyleKey de TextEditor para obter tema/foco e crescer com texto/linhas quebradas. Não interceptar Ctrl+V textual para colagem de bitmap: imagem usa botão/Ctrl+Shift+V. Streams obtidos por clipboard/drop pertencem ao fluxo de importação e devem ser descartados; a seleção nativa de arquivos mantém o contrato anterior de propriedade.

Backups usam o mecanismo de backup do SQLite, nunca simples cópia do arquivo principal com WAL ativo. Derivar imagens do snapshot; publicar pacote/pasta somente após concluir e validar. Nunca sobrescrever saída existente, mesclar restauração ao perfil atual ou apagar WAL/SHM da origem manualmente. Extração usa nomes permitidos, orçamento expandido e staging próprio. A limpeza só pode excluir o diretório temporário criado pela operação. A restauração aceita um prefixo completo conhecido das migrações e atualiza somente o staging antes de publicar. Rejeitar lacunas, migrações desconhecidas/futuras e estilos inválidos. Formato 1 do pacote não é criptografado; não alegar autenticação por checksum.

### Estrutura observada

```text
Trama/
├── assets/brand/
│   ├── trama-logo.png
│   └── README.md                         # Identidade e prompt da logo
├── docs/
│   ├── Trama_Documentacao_do_Produto_v1.docx
│   ├── Trama_Modelo_de_Documentacao.docx
│   └── qa/                               # PDFs e imagens de revisão visual
├── Logo/                                 # Cópia de imagem da logo
├── tools/
│   ├── build_documentation.py             # Geração dos dois documentos Word
│   └── render_with_word.ps1               # Exportação para PDF via Word COM
├── src/                                  # Domain, Application, Infrastructure e Desktop
├── tests/                                # UnitTests e IntegrationTests
├── .config/dotnet-tools.json
├── .github/workflows/ci.yml
├── Trama.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── NuGet.Config
├── README.md
└── CODEX.md
```

`build_documentation.py` usa Python, `python-docx` e suas APIs OOXML; resolve a raiz a partir do próprio arquivo e utiliza funções em `snake_case`. O conteúdo dos documentos fica incorporado nesse script. `render_with_word.ps1` utiliza PowerShell e Word instalado, com variáveis em camelCase, `$ErrorActionPreference = 'Stop'` e encerramento dos objetos COM em `finally`. Não há módulos Python, padrão de serviços ou estratégia de testes de produto estabelecidos por esses scripts.

**Pendência identificada:** `render_with_word.ps1` ainda fixa a raiz em `C:\Users\joset\www\IDEA`. Após a renomeação para Trama, seu destino está incorreto. Corrigir futuramente para resolução relativa ao script, antes de utilizá-lo; não corrigido nesta análise. A seleção oficial entre a logo em `assets/brand/` e a cópia em `Logo/` também deve ser preservada ou esclarecida antes de remover arquivos.

## 2. Visão geral da arquitetura e da API

O produto é um aplicativo desktop local e individual, com evolução planejada para projetos, estudos, pesquisa, agenda, finanças e cofre. A base usa C#, Avalonia e CommunityToolkit.Mvvm. Biblioteca, páginas em blocos, persistência SQLite e anexos gerenciados estão implementados no escopo documentado. Windows é o primeiro alvo de distribuição; desenvolvimento e testes foram validados no Ubuntu/WSL, sem equivaler a certificação de outras distribuições.

Não existe API HTTP a descrever neste momento. A primeira interface de aplicação será uma **API interna de casos de uso**, consumida tanto pelos ViewModels quanto pelo assistente. A arquitetura proposta é modular, com dependências voltadas ao domínio: apresentação aciona casos de uso; aplicação coordena regras e contratos; infraestrutura implementa persistência, arquivos e integrações. Não adicionar um servidor ASP.NET Core somente para conectar telas às regras locais.

O assistente será um adaptador dessa mesma API interna. A proposta é integrar Ollama para modelos locais e avaliar whisper.cpp para transcrição; a síntese de voz permanece a selecionar. O modelo poderá sugerir ações estruturadas, mas permissões, validação e execução pertencem ao aplicativo. Clima e notícias usam adaptadores externos opcionais. Notas e agenda precisam continuar operando sem IA ou internet.

Se houver necessidade futura de API HTTP, criar um adaptador de transporte separado e registrar a decisão de arquitetura. Rotas/controllers validam o contrato de transporte, chamam a aplicação e traduzem os resultados; não concentram regras de negócio ou acesso ao banco. Definir versionamento, autenticação, erros e testes de contrato antes de expor endpoints.

### Camadas e limites de dependência

| Projeto | Responsabilidade | Referências permitidas entre projetos |
| --- | --- | --- |
| `Trama.Domain` | Entidades, valores e invariantes de negócio | Nenhuma dependência de outros projetos Trama |
| `Trama.Application` | Casos de uso, DTOs e contratos de persistência e integração | `Trama.Domain` |
| `Trama.Infrastructure` | EF Core, SQLite, anexos, backup e adaptadores do sistema | `Trama.Application`, `Trama.Domain` |
| `Trama.Assistant` | Integrações com modelos e voz; tradução de intenções para casos de uso autorizados | `Trama.Application`; domínio somente quando necessário |
| `Trama.Desktop` | Views Avalonia, ViewModels e composição das dependências | `Trama.Application`; infraestrutura e assistente apenas na composição |

Serviços de aplicação representam operações do usuário; serviços de domínio só existem quando uma regra não pertence naturalmente a uma entidade ou valor. Contratos de repositório ficam na aplicação e implementações na infraestrutura. Criar abstrações por necessidade concreta; evitar repositório genérico que apenas replique cada método do EF Core. Não devolver `IQueryable`, `DbContext` ou entidades rastreadas para a interface ou o assistente.

```text
# Estrutura criada; Assistant e decisions ainda são extensões futuras
src/
  Trama.Domain/
  Trama.Application/
  Trama.Infrastructure/
  Trama.Desktop/
tests/
  Trama.UnitTests/
  Trama.IntegrationTests/
docs/
  decisions/               # Criar decisões técnicas quando necessárias
```

Organizar funcionalidades dentro de cada camada por módulo, como Knowledge, Studies, Projects e Planning. Adicionar Finance e Vault nas etapas correspondentes. Não criar projetos vazios para todos os recursos futuros.

## 3. Convenções de código propostas

- Na interface, centralizar cores semânticas em `Themes/Palette.axaml` e estilos em `Themes/Controls.axaml`. Usar `DynamicResource` nas Views e nos pincéis; não espalhar cores literais nas telas. Preservar foco, estados de interação e contraste ao criar estilos. A base permite substituição dos recursos; editor de paletas e persistência de preferências ainda são futuros.

- Usar inglês nos identificadores técnicos e português na experiência inicial do usuário e na documentação do produto. Não renomear artefatos existentes apenas para uniformizar idioma.
- Em C#, usar `PascalCase` para tipos, métodos e propriedades, `camelCase` para parâmetros e variáveis locais, `_camelCase` para campos privados, prefixo `I` para interfaces e sufixo `Async` para operações assíncronas que retornam tarefas.
- Alinhar namespaces a projeto e pasta. Usar nomes que expressem o caso de uso, como `CreateNote`, `CreateNoteRequest` e `CreateNoteResult`. Views e ViewModels correspondentes usam nomes como `NotesView.axaml` e `NotesViewModel.cs`.
- Habilitar referências anuláveis e resolver advertências pertinentes. Não usar `!`, supressões de analisadores ou `catch` vazio para ocultar defeitos.
- Usar injeção de dependências por construtor, composição na entrada do desktop e evitar estado global mutável ou localização de serviços em regras de negócio.
- Manter regras fora de code-behind e ViewModels. Code-behind pode conter comportamento estritamente visual. ViewModels não acessam SQL, arquivos, processos ou provedores de IA diretamente.
- Usar `async`/`await` para operações de entrada e saída, propagar `CancellationToken` quando aplicável e não bloquear a thread da interface com `.Result`, `.Wait()` ou inferência pesada.
- Validar entradas nos limites da aplicação e preservar invariantes no domínio. Distinguir falha de validação, cancelamento e erro inesperado; apresentar mensagens úteis sem expor dados sensíveis.
- Usar `decimal` e moeda explícita para dinheiro. Separar previsão de pagamento. Separar tarefas planejadas de sessões efetivamente realizadas.
- Registrar instantes com referência temporal explícita; preservar datas locais e fuso das recorrências. Tornar o relógio substituível nos testes de calendário. Não transformar um dia sem horário em um instante arbitrário.
- Manter anexos em armazenamento gerenciado, validar nomes e caminhos e impedir acesso fora das raízes permitidas. Coordenar alterações no banco e nos arquivos com recuperação de falhas.
- Resolver caminhos de ferramentas a partir do script ou configuração. Dados do usuário ficam em diretório apropriado do sistema, fora do checkout; nunca fixar caminhos de uma máquina de desenvolvimento.
- Fixar SDK, pacotes e ferramentas na fundação. Registrar dependências diretas e suas licenças. Node e Python só entram no runtime do produto quando existir necessidade justificada.
- Antes de modificar documentos gerados, verificar o script gerador para evitar que a próxima execução elimine mudanças manuais. Manter documentação, código e comandos coerentes após cada entrega.
- Na fundação, criar `.editorconfig` com UTF-8, quatro espaços para C#, remoção de espaços finais e nova linha ao fim do arquivo. Configurar analisadores e formatação antes de declarar lint obrigatório em CI.

## 4. Estratégia de testes proposta

Usar xUnit 2.9.3 com VSTest. A fundação contém quatro testes técnicos de integração e 14 casos unitários da primeira nota, todos passando na execução local após o GREEN. Testar comportamento observável e falhas relevantes; evitar testes que apenas reproduzam a implementação ou fixem detalhes internos. O repositório de notas dos testes é controlado; a persistência SQLite funcional ainda requer sua própria cobertura.

Em `NoteSchemaTests`, dez casos adicionais verificam a migração de produção, as colunas obrigatórias e a durabilidade SQL em arquivo. Estão no GREEN após aprovação e implementação de `InitialNotes`. Os testes usam SQL direto e não comprovam um repositório concreto nem integração com o desktop.

Em `SqliteNoteRepositoryTests`, 14 casos verificam escrita e leitura independentes, reabertura, duplicidade, somente leitura, consulta ausente versus falha, cancelamento prévio e integração com CreateNote. O adaptador recebe um contexto emprestado: o chamador deve controlar seu escopo e descartá-lo após uma falha de armazenamento. Não compartilhar esse contexto entre operações concorrentes. Nenhum desses testes comprova a interface do desktop.

- **Domínio e aplicação:** invariantes, recorrências, datas, separação entre planejado e realizado, finanças e permissões do assistente. Toda correção de bug deve incluir um teste de regressão quando houver comportamento automatizável.
- **Persistência:** usar SQLite real em banco temporário isolado nos testes de integração. Não substituir o provedor por EF InMemory para comprovar comportamento SQL, transações ou migrações.
- **Arquivos e backup:** criar dados em diretórios temporários de teste. Demonstrar restauração do banco e anexos em perfil limpo e tratar backup incompleto ou inválido.
- **Assistente e integrações:** substituir rede, relógio, áudio e abertura de aplicativos por implementações controladas na suíte normal. Validar argumentos, permissões, cancelamento e prevenção de execução duplicada. Testes reais com modelos são opcionais e separados.
- **Interface:** conferir navegação por teclado, contraste, estados de erro e cancelamento, leitura de código e prints. Automatizar fluxos estáveis quando isso trouxer cobertura útil.
- **API HTTP futura:** todo endpoint novo precisa de teste de integração do contrato, fluxo principal, entrada inválida e permissões quando aplicáveis. Contratos já expostos precisam de proteção contra regressão.

Não exigir GPU, conexão externa, microfone ou aplicativo pessoal instalado para executar os testes normais. Não usar dados reais do usuário como fixtures. Não criar testes de endpoints ou contratos inexistentes apenas para simular cobertura. Em TDD, os testes precedem a implementação de uma funcionalidade especificada e autorizada, seguindo a seção 9.

## 5. Comandos essenciais

### Situação atual

O SDK está fixado em `10.0.401` com roll-forward de patch. Avalonia 11.3.19, AvaloniaEdit 11.4.1 (MIT), CommunityToolkit.Mvvm 8.4.0, EF Core e dotnet-ef 10.0.9 formam a base atual. O editor 11.4.1 corrige a altura com tamanhos mistos; não usar a altura fixa de 11.3.0 como base para escrita rica. A referência explícita a SQLitePCLRaw.bundle_e_sqlite3 3.0.5 substitui a dependência nativa vulnerável trazida pelo bundle 2.1.11, sem suprimir a auditoria NuGet. Arquivos de lock preservam as dependências transitivas.

Os scripts atuais são de documentação:

```powershell
# Requer Python com python-docx; regrava os dois documentos em docs/
python tools/build_documentation.py

# Requer Windows e Word instalado; SOMENTE após corrigir a raiz antiga IDEA
./tools/render_with_word.ps1
```

`python` representa o interpretador do ambiente preparado; não há ambiente virtual ou dependências fixadas no repositório. Na sessão de autoria, foi utilizado o Python fornecido pelo runtime de documentos. Não executar o gerador como uma verificação sem efeitos: ele sobrescreve documentos. Não há lint ou testes configurados para esses utilitários.

### Comandos da fundação

Executar a partir da raiz. A suíte atual tem 454 casos aprovados no Linux/WSL. Não excluir testes para ocultar regressões. Novo pacote Windows isolado abriu nativamente; a CI remota permanece não executada e a distribuição assinada ainda está pendente. Se o desktop estiver em uso, compilar/testar com a mesma saída isolada e restaurar pacotes nessa saída antes de --no-restore.

```powershell
# Restaurar pacotes e ferramentas declaradas no repositório
dotnet restore Trama.slnx --locked-mode
dotnet tool restore

# Build e execução desktop
dotnet build Trama.slnx --configuration Release --no-restore
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --no-build

# Testes: após build Release e com projetos de teste incluídos na solução
dotnet test Trama.slnx --configuration Release --no-build

# Validar separadamente a infraestrutura técnica
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --no-build

# Verificar formatação e diagnósticos configurados sem modificar arquivos
dotnet format Trama.slnx --verify-no-changes --no-restore
```

O build executa os analisadores habilitados, com advertências tratadas como erros. `dotnet format` não substitui testes ou toda análise estática. O workflow Windows executa restore com lock, build, formatação e a suíte completa. Sua execução remota ainda não foi validada.

### Ferramentas EF Core

O contexto mapeia `NoteRecord` e possui `InitialNotes`. A fábrica exige `--database` com caminho absoluto e não inicia interface, áudio ou IA. A listagem abaixo não conecta ao banco. O desktop usa LocalDatabase para aplicar as migrações conhecidas; futuras evoluções de esquema exigem testes próprios, proteção dos dados e revisão antes da distribuição.

```powershell
# Inspecionar sem conexão; $developmentDatabase deve ser absoluto
$developmentDatabase = Join-Path (Get-Location).Path 'artifacts/development.db'
dotnet ef migrations list --project src/Trama.Infrastructure --startup-project src/Trama.Desktop --configuration Release --no-build --no-connect -- --database "$developmentDatabase"

# Exemplos FUTUROS, somente na etapa autorizada de persistência
dotnet ef migrations add AddNotes --project src/Trama.Infrastructure --startup-project src/Trama.Desktop --output-dir Persistence/Migrations -- --database "$developmentDatabase"

# Aplicar SOMENTE ao banco de desenvolvimento configurado
dotnet ef database update --project src/Trama.Infrastructure --startup-project src/Trama.Desktop -- --database "$developmentDatabase"
```

Revisar migrações geradas e testar com dados existentes. `InitialNotes` foi criada e aplicada apenas em bancos temporários pelos testes. A estratégia de atualização do banco pessoal ainda precisa ser definida. Seu `Down` remove Notes: rollback com dados exige autorização e backup recuperável. Não usar `EnsureCreated` para substituir migrações nem reescrever uma migração já distribuída.

## 6. Regras de ouro

1. **Nunca apresentar proposta como implementação existente.** A primeira nota foi implementada após a aprovação dos testes, somente na aplicação. Não apresentar shell, contratos ou testes com repositório controlado como persistência real pronta.
2. **Nunca inverter as dependências das camadas.** Domínio não conhece Avalonia, EF Core, HTTP, Ollama ou sistema operacional. Interface e assistente não contornam a aplicação para acessar dados.
3. **Nunca quebrar contratos consumidos sem evolução explícita.** Para API HTTP pública futura, versionar alterações incompatíveis e definir transição. Para API interna, atualizar consumidores e testes em conjunto. Formatos de exportação e backup precisam de versão e compatibilidade documentadas.
4. **Nunca adicionar endpoint sem teste de integração.** Mudanças em regras críticas também exigem testes proporcionais e critérios de aceite verificáveis. Nenhuma mudança de software está pronta sem os testes aplicáveis passando e revisão humana do diff; verificações pendentes devem ser declaradas e impedem a conclusão.
5. **Nunca recriar o banco, apagar dados ou reescrever migração já distribuída para resolver incompatibilidade.** Evoluir com migração nova, proteção dos dados e teste de recuperação. Operações destrutivas sobre dados pessoais exigem autorização específica.
6. **Nunca considerar backup válido sem demonstrar restauração.** Incluir banco consistente, anexos e versão do formato; prever recuperação das chaves quando houver cofre.
7. **Nunca armazenar segredos em texto puro nem expô-los em logs, código-fonte, busca geral, prompts ou fixtures.** Chaves e tokens de integrações devem ser fornecidos por variáveis de ambiente em runtime, sem valores reais em arquivos versionados. SQLite comum não fornece criptografia automática. Credenciais pessoais do cofre exigem armazenamento criptografado e gestão de chaves consolidados; nunca exportar o cofre inteiro para variáveis de ambiente.
8. **Nunca executar texto do modelo como shell, SQL ou código arbitrário.** Usar catálogo de ações, validação de argumentos e permissões. Abrir aplicativos por entradas cadastradas; exigir confirmação apropriada para ações de maior impacto.
9. **Nunca permitir que conteúdo de notas, notícias ou arquivos conceda permissões ao assistente.** Tratar esse conteúdo como dados não confiáveis e confirmar execução pelo resultado real da ferramenta.
10. **Nunca tornar notas e agenda dependentes de internet ou inferência.** Falhas de IA, clima ou notícias devem ser isoladas; indicar fonte e horário de dados externos.
11. **Nunca treinar com dados pessoais por padrão.** ML e eventual fine-tuning são evoluções possíveis. Exigir seleção autorizada dos dados, exclusão de segredos, licença compatível, conjunto de avaliação e registro de modelo, dataset e métricas. Experimentos não podem alterar silenciosamente o comportamento do assistente principal.
12. **Nunca fabricar referências científicas, evidências ou resultados.** Preservar fonte, autoria e localização de citações e distinguir conteúdo do usuário de texto gerado.
13. **Nunca ampliar a stack ou o escopo sem necessidade registrada.** Priorizar organização básica, rotina e marco de voz; manter futuras pesquisas de ML desacopladas da persistência e dos casos de uso.
14. **Nunca alterar documentos, logos ou scripts incidentalmente ao revisar este guia.** Registrar pendências e mudanças de arquitetura de forma explícita, sem sobrescrever trabalho do usuário.

## 7. Ordem de implementação e decisões abertas

Seguir o plano documentado: fundação e protótipos; organização básica com notas, código, prints, projetos, tags, busca e backup restaurável; rotina e visão semanal; marco de voz; resumo do dia e fontes externas; finanças; cofre; pesquisa e assistência avançada. A ordem de finanças e cofre pode ser ajustada por prioridade. O conhecimento do responsável em Machine Learning permite explorar componentes próprios ou ajuste de modelos abertos em uma etapa futura, sem transformar treinamento do zero em requisito inicial.

A fundação definiu SDK, versões, MVVM, testes, ferramentas e locks. Biblioteca visual, editor, renomeação/movimentação, proteção ao fechar, autosave e lixeira recuperável estão implementados no escopo documentado. Permanecem exclusão permanente, lembretes, recuperação avançada, hardware e voz. A CI está configurada e ainda não foi executada remotamente.

Fontes locais da análise: os dois documentos em `docs/`, scripts em `tools/`, `assets/brand/README.md`, inventário completo da pasta e decisões do responsável nesta conversa. Os PDFs e PNGs em `docs/qa/` são artefatos de revisão dos documentos, não resultados de testes do aplicativo.

## 8. Exploração e planejamento com subagentes

As regras das seções 8 a 11 registram o fluxo permanente solicitado pelo responsável. Os exemplos recebidos sobre autenticação por API key em pagamentos são modelos de processo: **não representam uma funcionalidade do Trama**. Não criar endpoints, middleware, autenticação ou códigos HTTP 401/403 para aplicar esses exemplos. O módulo financeiro planejado é controle pessoal de receitas e contas; não implica uma API de processamento de pagamentos.

Antes de implementar uma funcionalidade com exploração paralela, o coordenador delimita o objetivo, os arquivos envolvidos e as perguntas de cada agente. Os exploradores trabalham somente em leitura:

1. **Arquitetura e fluxo:** mapear os pontos de entrada reais, os casos de uso, as camadas e as dependências afetadas. Só investigar rotas e middleware quando existirem.
2. **Segurança e padrões:** identificar regras reaproveitáveis, permissões, contratos, dados sensíveis e limites das integrações. Declarar explicitamente quando um padrão ainda não estiver implementado.
3. **Testes e compatibilidade:** localizar a cobertura existente e suas lacunas, os consumidores reais e os cenários de aceite. Ausência de testes deve ser relatada como ausência, sem fabricar resultados.

O coordenador consolida evidências com caminhos e linhas quando houver código, divergências entre propostas, riscos, decisões pendentes e plano incremental. Distinguir sempre observado, proposto e não verificado. Nenhum explorador instala dependências, cria projetos ou altera o produto.

A fundação e os testes da primeira nota foram autorizados; o caso de uso passou ao GREEN. Explorações futuras devem reutilizar esse mapa e concentrar-se no que mudou. Persistência real e interface seguem como novas fatias, sem autorização implícita para antecipá-las nesta entrega.

## 9. TDD com revisão humana obrigatória

### Pré condição de infraestrutura

Antes do primeiro ciclo, autorizar separadamente a fundação mínima: solução, runner, dependências de teste e contratos mínimos necessários à compilação. Não antecipar regras funcionais nessa preparação. Registrar o que foi criado e verificar que o runner realmente descobre e executa testes. Falta de projeto, dependência ou tipo não demonstra falha comportamental correta.

### Ciclo por funcionalidade

1. **Especificar:** combinar o comportamento esperado e os critérios de aceite. Preservar contratos existentes quando existirem consumidores; não afirmar compatibilidade com consumidores que ainda não existem.
2. **RED:** escrever apenas os testes do comportamento acordado. Não escrever a implementação da funcionalidade. Executar os testes e mostrar comando, testes descobertos, resultado e motivo de cada falha. A falha precisa corresponder ao comportamento ausente; erro de compilação ou infraestrutura não satisfaz esse critério.
3. **PARAR para revisão:** apresentar o diff dos testes e aguardar aprovação explícita do responsável. Silêncio e tempo decorrido não contam como aprovação. Esta pausa foi solicitada pelo próprio responsável no Prompt-3.
4. **GREEN:** somente após a aprovação, implementar o mínimo necessário para fazer os testes passarem, sem modificar ou enfraquecer os testes aprovados. Necessidade de alterar a especificação exige nova revisão antes de prosseguir.
5. **Refatorar e revisar:** depois do GREEN, melhorar a estrutura mantendo os testes aprovados intactos, executar as verificações pertinentes e submeter o diff final à revisão humana.

O agente de testes pode acrescentar cenários de risco compatíveis com a especificação. Esses testes também seguem RED e revisão antes da implementação correspondente; aprovação anterior só cobre os cenários explicitamente aprovados. Um novo contrato ou regra de negócio exige confirmação da especificação.

**Histórico do primeiro GREEN:** CreateNote usa ICreateNote/SqliteNoteService e continua testado para compatibilidade. A janela atual usa LibraryViewModel → ILibraryActions → casos de uso e adaptadores, com salvamento manual e páginas em blocos. Não remover o caminho anterior apenas para simplificar a composição.

## 10. Coordenação dos agentes especializados

Após a exploração, o coordenador distribui responsabilidades com escopo de arquivos e critério de entrega. A existência dos papéis não exige criar todos os agentes para toda alteração. A mesma pessoa ou agente pode exercer papéis sequenciais em trabalhos pequenos, mantendo a revisão proporcional; em tarefas com múltiplos agentes solicitadas, preservar revisão independente.

| Papel | Responsabilidade e limite |
| --- | --- |
| Implementador | Implementar após aprovação do plano e dos testes, respeitar este guia e corrigir achados do revisor. Não mudar testes aprovados para obter GREEN. |
| Revisor | Examinar diff e evidências; apontar bugs, riscos, casos de borda e violações de camadas, com impacto e justificativa. Não aprovar apenas porque os testes passaram. |
| Testes | Preparar a especificação executável e acrescentar testes dos riscos encontrados, sem alterar o comportamento contratado; respeitar a pausa do TDD. |
| Documentação | Atualizar comandos, decisões e documentação da funcionalidade. Atualizar README quando existir; documentar API HTTP somente se criada e autorizada. |
| Coordenador | Consolidar achados, controlar aprovações, integrar mudanças, executar verificações e apresentar diff, riscos e esforço ao responsável. |

Sequência: exploração paralela → plano consolidado → testes RED → aprovação humana → implementação GREEN → revisão crítica → correções e testes adicionais com os mesmos limites de TDD → documentação → verificações → diff e riscos → revisão humana final.

Não editar simultaneamente o mesmo arquivo. Designar propriedade temporária por arquivo, compartilhar contratos antes de tarefas dependentes e integrar mudanças de forma sequencial. O paralelismo se aplica a trabalho independente; aprovação, implementação dependente e correção de achados seguem sua ordem.

O implementador deve tratar os achados acionáveis antes de seguir para a entrega. Divergências de contrato, segurança, arquitetura ou escopo que não possam ser resolvidas dentro da autorização retornam ao responsável. Não ignorar achados para economizar iterações.

Ao final, informar o que cada agente realizou, os testes e revisões executados, os achados corrigidos, os riscos remanescentes e as decisões ainda necessárias. Não atribuir implementação ou testes executados a agentes que apenas analisaram documentos.

## 11. Guardrails permanentes de qualidade segurança e custo

### Controle do Git e do GitHub

Git e GitHub são operados pelo responsável. O agente deve somente fornecer os comandos para ele executar; não executar comandos git/gh, criar ou trocar branches, preparar staging, fazer commits, push, Pull Requests ou merges. Expressões como “vamos commitar” ou “criar a branch” significam orientar com os comandos, não autorização para executá-los. Editar somente os arquivos solicitados e entregar as alterações para revisão. Essa preferência permanece até o responsável mudá-la explicitamente.

Economia solicitada pelo responsável: preferir consultas focadas, respostas curtas, testes afetados e documentação sem duplicação. Para tarefas pequenas, o coordenador executa e revisa sem subagentes; delegar somente quando o risco ou o trabalho independente justificar. Preservar as pausas de TDD e não reduzir verificações necessárias para economizar tokens.

### Qualidade e revisão do diff

- Nenhuma mudança de software é considerada pronta sem testes aplicáveis passando e sem diff revisado pelo responsável. Entrega para revisão é um estado distinto de conclusão aprovada.
- Sempre apresentar o diff acessível e um resumo dos riscos antes de pedir revisão final. Informar comandos executados, resultados, verificações não realizadas e respectivas razões.
- Enquanto não existir `.git`, comparar a versão anterior e a nova com diff unificado, preservando a base fora dos arquivos do produto. Não inicializar Git ou criar commits apenas para produzir o diff sem autorização.
- Mudanças exclusivamente documentais usam verificações de conteúdo, consistência e links locais; não criar testes artificiais para documentos. Sem suíte de produto, registrar testes como **não aplicáveis nesta mudança**, e nunca como aprovados ou executados.
- Falhas relevantes, ausência de infraestrutura necessária ou revisão pendente impedem declarar a mudança de software concluída. O estado RED deve ser apresentado como especificação em revisão, sem alegar entrega funcional concluída.

### Segredos e dados sensíveis

- Verificar o diff e os arquivos textuais envolvidos antes de cada entrega, procurando credenciais, tokens, senhas e dados pessoais indevidos. Revisar também o código que registra logs; não basta procurar um segredo literal.
- Chaves e tokens de serviços externos entram por variáveis de ambiente em runtime. Exemplos contêm somente nomes de variáveis e placeholders; nunca valores reais. Arquivos `.env` locais não devem ser versionados; definir essa exclusão ao preparar o repositório Git.
- Não imprimir variáveis de ambiente, cabeçalhos de autenticação, conteúdo do cofre, prompts sensíveis ou achados completos do scanner em logs de desenvolvimento, CI ou ferramentas de agentes. Relatar somente tipo de achado, caminho e linha.
- A regra de variáveis de ambiente protege configuração sensível de integrações. Ela não substitui a criptografia do cofre nem exige colocar notas, dados financeiros ou todo o banco em variáveis de ambiente. Definir gestão das chaves do cofre antes de seu uso real.
- Se um segredo real for detectado, interromper sua divulgação, removê-lo da alteração e comunicar a necessidade de revogação ou rotação. A varredura é uma verificação limitada, não uma garantia absoluta de ausência de vazamentos; binários e histórico exigem avaliação própria quando aplicável.

### Esforço e custo de agentes

- Antes de delegar, definir uma pergunta específica por agente, acesso somente aos arquivos relevantes e condição clara de encerramento. Reaproveitar resultados existentes e evitar explorações duplicadas.
- Ajustar a quantidade de agentes à independência e ao risco do trabalho, respeitando a capacidade disponível. Executar os quatro papéis em ondas quando não houver vagas simultâneas.
- Como orçamento inicial, usar uma rodada de exploração/revisão e uma rodada de correções. Se forem insuficientes, resumir os achados e a razão antes de ampliar o esforço; nunca encerrar com falhas relevantes apenas para cumprir o orçamento.
- Não repetir testes, buscas ou revisões sem mudança, falha ou dúvida nova que justifique. Usar testes focados durante o ciclo e a suíte pertinente na validação da entrega.
- Ao final de tarefas com loops ou múltiplos agentes, informar número de agentes, rodadas e verificações, além de uma estimativa qualitativa de esforço. Só informar tokens ou valor monetário se houver medição disponível; não inventar precisão.
- Incluir uma sugestão concreta de economia para a próxima etapa, como reduzir contexto, limitar arquivos por agente ou reutilizar o mapa de arquitetura. Limites de custo não autorizam reduzir segurança ou omitir revisão.

Estas regras permanecem como convenções do projeto até alteração explícita pelo responsável. Atualizar os comandos e os limites de cada fase conforme a fundação for aprovada e implementada.

## 12. Central de estudos do projeto

Preferência explícita do responsável: atualizar o manual de uso/PDF e os guias para distribuir ao colega somente quando ele pedir. A documentação técnica do desenvolvimento, o roadmap e os relatórios continuam acompanhando as entregas; não regenerar o manual automaticamente a cada mudança de código.

Manter `docs/estudos/README.md` como índice permanente das explicações de arquitetura e implementação solicitadas pelo responsável. A cada entrega de código, criar ou atualizar o capítulo pertinente, com objetivo, conceitos, arquivos reais, fluxo, decisões, testes, limitações e perguntas de estudo. Vincular o material pelo README principal e atualizar o índice.

Manter também `rodmap.md` na raiz como checklist do produto. Atualizar o status e as evidências a cada entrega, distinguindo conclusão técnica local, recurso utilizável pelo usuário, etapa parcial e trabalho futuro. Não marcar persistência ou interface como concluídas com base apenas em contratos ou testes controlados.

Preferir Markdown versionável para acompanhar a evolução do código. Quando houver pedido de Word, produzir o documento correspondente sem substituir silenciosamente o acervo existente. Diferenciar material didático, plano do produto e relatório de execução: um exemplo não é prova de uma verificação que não ocorreu. Preservar relatórios antigos como histórico e indicar a versão atual em novos registros.

### Referência da interface do caderno

Direção permanente da interface: acervo e busca na lateral, hierarquia Espaço → Caderno → Página explícita, contexto/estado/Salvar reunidos e superfície contínua com ferramentas discretas. Evitar cabeçalhos e salvamentos duplicados. Correções de apresentação reutilizam comandos e preservam foco, seleção, histórico e autosave. Conferir capturas reais em largura ampla/compacta e nomes longos; testes funcionais não demonstram aprovação estética. Ver o capítulo 34 da central de estudos.

## Ambiente de continuidade no WSL

Editar fontes no repositório Windows. `tools/run-wsl.sh` sincroniza somente fontes para um espelho Linux identificado e preserva bin/obj e o perfil Linux separado. Não editar o espelho nem copiar bancos pessoais ativos. Executar pelo PowerShell: `wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test` (build, format, testes); trocar test por run para abrir. O SDK 10.0.401 fica em ~/.dotnet-trama. Recursos de clipboard do próprio processo continuam pertencendo ao clipboard; descartar apenas os recursos externos possuídos pelo adaptador. Preservar a normalização de IOException ao criar o diretório. Não alterar testes aprovados para acomodar diferenças de plataforma. Ver o capítulo 20 para preparação e limites.
