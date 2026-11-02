# Roadmap do Trama — registro anterior às listas

Registro arquivado em 31/10/2026 antes da reorganização. Os estados abaixo são históricos; consulte o [roadmap atual](../../rodmap.md). Links relativos foram ajustados para preservar as referências.

Atualizado em 31 de outubro de 2026. Este arquivo acompanha o trabalho real e as próximas entregas; a visão detalhada está nos documentos de produto em `docs/`.

## Onde estamos

**Resumo atual: lixeira validada no uso pelo responsável; próxima funcionalidade solicitada: listas na formatação.** Última suíte com **454 testes aprovados**, preservados. Listas ainda não foram implementadas; proposta inicial de marcadores/numeração e critérios de fechamento estão no [capítulo 43](../../docs/estudos/43-listas-e-fechamento-do-modulo.md). A revisão do diff é registrada separadamente da validação de uso.

- [ ] Especificar e revisar testes de listas com marcadores e numeradas, com Enter, seleção, estilos e histórico.
- [ ] Implementar a fatia aprovada e validar persistência, autosave, lixeira/backup e janela compacta.
- [ ] Definir a primeira exportação acessível e executar o ciclo final do módulo; Markdown é a proposta inicial, PDF posterior.

- [x] Preparar 18 fluxos de lixeira dos três níveis, sem alterar a implementação.
- [x] Receber aprovação dos 18 casos antes de implementar.
- [x] Implementar e validar os 18 fluxos, preservando os 426 anteriores e os contratos existentes.
- [x] Preparar dez casos complementares de migração, backup, legado, cancelamento, rollback e autosave.
- [x] Receber aprovação dos dez casos complementares antes das três correções restantes.
- [x] Corrigir os casos aprovados, conferir 454 testes e o ciclo de backup/restauração; gerar pacote isolado e confirmar abertura/fechamento Windows.
- [x] Receber validação funcional/visual da lixeira pelo responsável.
- [ ] Registrar revisão humana do diff da lixeira.

- [x] Diagnosticar apresentação da janela WSLg: interface desenhada no X11, reinício autorizado do WSL e exibição confirmada; código e perfil preservados.

- [x] Especificar exclusão/restauração, migração, autosave e compatibilidade com backup; aprovar os testes antes de implementar.
- [x] Implementar lixeira dos três níveis, preservando conteúdo e anexos; revisão humana final pendente.
- [ ] Conferir o ciclo completo dos cadernos e os pacotes Windows/Linux antes de encerrar a primeira versão do módulo.

- [x] Padronizar campos e levar a criação ao “+” dos cabeçalhos Espaço/Caderno; 426 testes preservados. Ver [estudo](../../docs/estudos/37-campos-e-criacao-na-lateral.md).
- [x] Receber validação visual dos campos/cabeçalhos, incluindo TAG EXATA +.
- [ ] Registrar revisão do diff desta correção.

- [x] Especificar os ajustes solicitados de marcadores, lateral, filtros, formatação compacta e menu dos blocos em 12 casos novos. Ver [especificação](../../docs/estudos/35-refino-responsivo-especificacao.md).
- [x] Receber aprovação dos 12 testes do refino antes da implementação, incluindo pedido visual de destaque do espaço/caderno ativos.
- [x] Implementar margem, painéis, filtros, menu Bloco e seleção verde nos três níveis; conferir testes e capturas ampla/compacta. Ver [implementação](../../docs/estudos/36-refino-responsivo-implementado.md).
- [ ] Receber revisão humana do diff e do refino visual; conferir outros DPI e conforto da rolagem.

- [x] Corrigir a composição após o feedback sobre a referência: busca lateral, criação recolhida, contexto/Salvar unificados, superfície aberta e margem; 414 testes preservados e pacote Windows isolado conferido. Ver [estudo](../../docs/estudos/34-alinhamento-com-a-referencia-visual.md).
- [ ] Receber revisão humana do [diff e resultado visual](../../docs/reviews/044-reference-alignment.md); aparência em outros DPI e uso prolongado continuam pendentes.

**Prioridade atual: revisão da escrita rica e identidade dos cadernos.** Modelo, persistência, controles e composição com hierarquia explícita Espaço → Caderno → Página implementados após aprovação; 414 testes aprovados. Harness visual e pacote Windows isolado conferidos. Revisão estética/interativa pelo responsável permanece pendente. Ver [direção e etapas](../../docs/estudos/26-identidade-e-formatacao-dos-cadernos.md).

- [x] Mapear limitações do editor e impactos em histórico, SQLite, versões e backup.
- [x] Especificar a proposta de escrita por seleção e direção visual do caderno.
- [x] Preparar e executar 28 testes de retenção/validação de atributos; 27 falhas esperadas e um caso de compatibilidade aprovado. Os 325 anteriores continuam passando; build/format passaram.
- [x] Receber aprovação dos 28 casos antes de implementar. Ver [relatório RED](../../docs/reviews/036-rich-text-attributes-red.md).
- [x] Implementar representação, validação e preservação de tamanho/cor/destaque no modelo em memória, sem perder negrito/itálico ou conteúdo; 353 testes passaram. Ver [estudo](../../docs/estudos/27-atributos-de-texto-implementados.md).
- [ ] Revisar o diff do modelo de atributos; controles por seleção acrescentados na entrega abaixo.
- [x] Especificar e executar 21 casos de persistência/versões/backup; 13 falhas esperadas e oito proteções passando, com os 353 anteriores preservados. Ver [especificação](../../docs/estudos/28-persistencia-de-atributos-especificacao.md).
- [x] Receber aprovação dos 21 casos e da regra de versões antes de implementar persistência dos novos atributos.
- [x] Implementar gravação/leitura compatível e validação de backup/restauração após a aprovação; 374 casos passaram. Confirmar esquema sem tabela de estilos e atualização posterior em checagem isolada. Ver [estudo](../../docs/estudos/29-persistencia-de-atributos-implementada.md).
- [ ] Revisar o diff da persistência de atributos; sem nova publicação Windows nesta fatia.
- [x] Preparar e executar 25 casos da barra, seleção/foco, atributos/limpeza, histórico, autosave e renderização: 23 falhas esperadas, duas proteções passando e os 374 anteriores preservados. Ver [especificação](../../docs/estudos/30-controles-de-formatacao-especificacao.md).
- [x] Receber aprovação dos 25 casos antes de implementar os controles/renderização.
- [x] Conectar barra compartilhada, estados mistos, foco, limpeza e histórico completo de atributos; 399 casos passaram sem modificar os testes aprovados.
- [x] Corrigir sobreposição com tamanhos mistos atualizando AvaloniaEdit para 11.4.1; preservar Avalonia 11.3.19 e grafos Windows dos locks.
- [x] Conferir popup, seleção/foco, autosave/reabertura SQLite, paleta dinâmica, altura e toolbar em janelas 1340/860; gerar pacote Windows isolado e confirmar abertura/fechamento nativos.
- [ ] Revisar o diff final e conferir interativamente a escrita rica no novo pacote. Ver [estudo](../../docs/estudos/31-controles-de-formatacao-implementados.md).
- [x] Aplicar contexto explícito, lateral com recuos e página contínua com margem; conferir janela ampla/estreita, nomes longos, teclado e conteúdo com anexo.
- [x] Preparar e executar 15 casos de contexto hierárquico, transições e janela compacta; 13 falhas esperadas, duas proteções passando e os 399 anteriores preservados. Ver [especificação](../../docs/estudos/32-identidade-e-hierarquia-especificacao.md).
- [x] Receber aprovação dos 15 casos antes de implementar o contexto e a composição visual.
- [x] Implementar contexto e notificações sem gravação/recriação do editor; 414 testes aprovados. Ver [estudo](../../docs/estudos/33-identidade-e-hierarquia-implementadas.md).
- [x] Gerar pacote Windows separado e confirmar inicialização/fechamento nativos em perfil sintético; apresentar diff e capturas.
- [ ] Receber revisão humana do diff e validação funcional/visual do novo caderno.

O plano geral permanece: concluir organização básica e depois seguir para rotina e assistente. Manual/PDF só serão atualizados quando solicitados.

- [x] Validar autosave no uso pelo responsável e escolher desfazer/refazer como próxima etapa.
- [x] Preparar treze testes de histórico de texto/estilos, atalhos e compatibilidade com salvamento; RED com dez falhas esperadas e três proteções passando.
- [x] Aprovar os treze casos antes de implementar. Ver [especificação](../../docs/estudos/24-desfazer-refazer-especificacao.md).
- [x] Implementar histórico coerente de prosa, conferir autosave/isolamento e obter 325 testes aprovados. Ver [estudo](../../docs/estudos/25-desfazer-refazer-implementado.md).
- [ ] Revisar o diff final e testar interativamente os atalhos no novo pacote Windows.

- [x] Definir a próxima etapa: salvar automaticamente a própria página após pausa, conforme preferência do responsável.
- [x] Preparar oito testes de autosave com relógio controlado e executar RED.
- [x] Aprovar os oito casos antes de implementar agendamento, composição e interface. Ver [especificação](../../docs/estudos/22-salvamento-automatico-especificacao.md).
- [x] Implementar autosave, preservar foco e revisões, conferir navegação/descarte e validar banco real; 312 testes passaram. Ver [estudo](../../docs/estudos/23-salvamento-automatico-implementado.md).
- [x] Conferir interativamente o pacote com autosave: validado pelo responsável.
- [ ] Revisar o diff técnico do autosave; confirmação funcional e revisão de código são etapas distintas.

- [x] Receber validação funcional do exe pelo responsável; conferência independente do colega permanece pendente.
- [x] Refinar cores do código após aprovação dos seis casos: funções e tipos separados, recursos dinâmicos e 304 testes aprovados. Ver [relatório GREEN](../../docs/reviews/030-code-highlighting-green.md).
- [ ] Revisar o diff e a aparência do novo realce; teste independente do colega permanece pendente.
- [x] Diagnosticar formatos do print Windows/WSLg: Windows Bitmap/PNG, Wayland image/bmp, X11 somente texto; reinício autorizado não resolveu a diferença.
- [ ] Baixa prioridade por decisão do responsável: leitura Wayland/BMP em novo ciclo TDD; funciona no Windows e não bloqueia as próximas etapas. Não considerar clipboard WSL validado.

- [x] Gerar pacote Windows win-x64 self-contained e confirmar abertura nativa com perfil isolado; usuário confirmou Pronto. Ver [relatório](../../docs/reviews/028-windows-exe-smoke-test.md). Assinatura, distribuição e conferência funcional completa do pacote permanecem pendentes.

### Cinco próximos passos solicitados

1. **Proteção ao fechar — implementada, para revisão do diff:** 171 testes aprovados; confirmação, espera pela gravação e cancelamento preservando a janela.
2. **Gestão da biblioteca — implementada, para revisão do diff:** renomear espaços/cadernos/páginas e mover páginas entre cadernos/espaços; 200 testes aprovados e conferência visual com SQLite isolado.
3. **Busca e tags — implementadas, para revisão do diff:** busca global por título/texto/código/legenda, tags persistentes e filtro exato; 230 testes aprovados e controles conferidos com SQLite isolado.
4. **Backup e restauração — implementados, para revisão do diff:** snapshot SQLite/imagens, manifesto e restauração em pasta nova; abertura explícita do perfil. Correção adicional de tags nos dois campos; 270 testes aprovados.
5. **Escrita e imagens — implementado e validado no WSL, para revisão do diff:** negrito/itálico por seleção, clipboard/drop e backups compatíveis. Build/formatação e 298 testes aprovados; harness final com duas colagens passou. Execução Windows nativa e integração interativa continuam pendentes.

- [x] Registrar o plano dos cinco passos e preparar os testes do primeiro.
- [x] Revisar e aprovar os 14 casos de fechamento antes de implementar.
- [x] Implementar a proteção, obter GREEN e conferir a janela com dados sintéticos.
- [x] Preparar e executar os 29 casos RED de gestão da biblioteca; os 171 anteriores continuam passando.
- [x] Aprovar os 29 testes do passo 2 antes da implementação.
- [x] Implementar renomeação e movimentação, conectar os controles e conferir a interface com dados sintéticos.
- [ ] Revisar o diff final do passo 2.
- [x] Preparar e executar os 30 testes de busca/tags em RED.
- [x] Aprovar os 30 testes do passo 3 antes de implementar.
- [x] Criar migração de tags compatível, implementar a busca e conectar os controles.
- [x] Conferir rascunho, resultados antigos, destino real, tags e reabertura pelo fluxo visual isolado.
- [ ] Revisar o diff final do passo 3.
- [x] Preparar e executar os 34 testes de backup/restauração em RED.
- [x] Aprovar o formato do pacote, limites iniciais e 34 testes do passo 4.
- [x] Implementar snapshot SQLite, pacote de imagens e restauração em pasta nova.
- [x] Conectar controles e abertura explícita do perfil restaurado; comprovar o fluxo com dados sintéticos.
- [x] Reproduzir a falha de tags, aprovar seis regressões e corrigir busca geral/filtro com caixa indiferente.
- [ ] Revisar o diff final do passo 4 e correção de tags.
- [x] Confirmar a preferência por negrito/itálico na seleção de texto.
- [x] Preparar e executar os 28 casos RED do passo 5.
- [x] Aprovar os 28 testes antes de implementar.
- [x] Implementar entrada de imagens, editor com estilos por seleção e migração compatível.
- [x] Conferir seleção visual, clipboard/drop e restauração com estilos em harness isolado; testes de backup anterior passaram.
- [x] Preparar ambiente Linux autorizado e repetir a suíte/harness finais.
- [ ] Resolver a distribuição/execução nativa Windows sem desativar a proteção.
- [ ] Conferir clipboard/drag do Windows nativo e escala em uso interativo.
- [ ] Revisar o diff final e encerrar a validação do passo 5.

Consulte o [plano dos cinco passos](../../docs/estudos/15-cinco-passos-e-protecao-ao-fechar.md), o [estudo de escrita/imagens](../../docs/estudos/19-imagens-e-formatacao.md) e o [relatório atual](../../docs/reviews/026-writing-implementation-review.md). O passo 5 passou validação no WSL; integração nativa e revisão do diff continuam abertas. Ver também o [relatório WSL](../../docs/reviews/027-wsl-development-green.md).

**Histórico: biblioteca visual e rascunhos.** Os 157 testes passaram naquela entrega sem alterações. A janela cria espaços/cadernos, lista e abre páginas e protege alterações durante navegação. Conteúdo e destino são confirmados em uma transação. Consulte o [material de estudo](../../docs/estudos/14-biblioteca-visual-implementada.md) e o [relatório GREEN](../../docs/reviews/016-library-navigation-green.md).

- [x] Especificar criação de espaços/cadernos, listagens, abertura e página nova no caderno selecionado.
- [x] Especificar confirmação de alterações não salvas: salvar, descartar ou cancelar.
- [x] Especificar rollback conjunto de conteúdo e associação quando o destino é inválido.
- [x] Aprovar os 31 casos antes de implementar.
- [x] Implementar port de aplicação, coordenação transacional, estado de alterações e navegação.
- [x] Conectar a biblioteca visual e conferir criação, salvamento, reabertura e confirmação com dados sintéticos.
- [x] Proteger o rascunho no fechamento normal da janela principal.
- [x] Implementar autosave da própria página após pausa, preservando o botão Salvar.
- [ ] Definir recuperação das edições posteriores ao último salvamento após interrupção forçada.
- [x] Implementar renomeação visual e mover páginas pela interface com testes próprios.
- [ ] Especificar exclusão de itens em uma etapa própria.

**Histórico: catálogo da biblioteca.** A persistência de espaços/cadernos/páginas passou com 126 testes naquela entrega. A navegação visual veio depois, conforme a seção atual acima. Veja a [explicação](../../docs/estudos/12-espacos-cadernos-e-paginas.md) e o [relatório dessa etapa](../../docs/reviews/014-knowledge-catalog-green.md).

- [x] Especificar vários espaços e cadernos, páginas não organizadas e preservação das notas existentes.
- [x] Especificar movimentação sem duplicação e localização preservada após editar/salvar uma página.
- [x] Aprovar a hierarquia inicial e os 13 casos do catálogo.
- [x] Implementar catálogo e migração compatível e obter GREEN.
- [x] Especificar e conectar criação, listagem e abertura de páginas na biblioteca visual.
- [x] Proteger o rascunho ao trocar de página e gravar a página no caderno selecionado.

**Histórico: editor contínuo.** A página com texto, código e imagens passou com 113 testes naquela entrega e depois foi integrada à biblioteca. Consulte a [explicação](../../docs/estudos/11-interface-do-caderno-implementada.md) e o [relatório dessa etapa](../../docs/reviews/012-notebook-editor-green.md).

- [x] Especificar página longa, inserção/edição/movimentação de blocos e salvamento sem limpar a página.
- [x] Especificar preservação do rascunho, bloqueio durante operações e composição com armazenamento real.
- [x] Aprovar os 23 casos e o comportamento de manter a página aberta após salvar.
- [x] Implementar casos de uso, ViewModel, composição e página contínua e obter GREEN.
- [x] Conferir prévias de código, imagens, rolagem e paleta alternativa.
- [ ] Conferir seleção nativa de arquivos, teclado e escala do Windows em uso interativo.

**Histórico: persistência do caderno e anexos.** Os 18 casos aprovados elevaram a suíte para 90 naquela entrega, antes da conexão com a nova interface. Veja a [explicação](../../docs/estudos/09-persistencia-do-caderno-e-anexos.md) e o [relatório dessa etapa](../../docs/reviews/010-notebook-storage-green.md).

- [x] Preparar testes de reabertura dos blocos, atualização, notas antigas, isolamento entre páginas e falhas de armazenamento.
- [x] Preparar testes de cópia e reabertura de PNG/JPEG, limite proposto de 10 MiB e falhas sem arquivos abandonados.
- [x] Revisar e aprovar os 18 casos e o limite de importação antes de implementar.
- [x] Implementar persistência e anexos e obter GREEN sem modificar os testes aprovados.
- [ ] Especificar a coordenação entre importação de arquivo e gravação da página antes de conectar o editor.

**Histórico: rascunho em blocos.** A estrutura em memória passou com 72 testes naquela entrega. Persistência, editor e biblioteca foram conectados nas etapas posteriores. Consulte a [explicação](../../docs/estudos/08-caderno-em-blocos.md) e o [relatório dessa etapa](../../docs/reviews/008-notebook-draft-green.md).

- [x] Especificar ordem de blocos, preservação de texto longo/código e referência de imagem em memória.
- [x] Revisar e aprovar os 15 casos novos antes de implementar.
- [x] Implementar o rascunho em blocos e obter GREEN.
- [x] Especificar e implementar persistência compatível com notas antigas no adaptador.
- [x] Especificar e implementar importação de PNG/JPEG para anexos gerenciados no adaptador.
- [x] Substituir o formulário por página contínua, com ações compactas.
- [x] Exibir blocos de código com fonte monoespaçada e destaque de sintaxe.
- [x] Exibir imagens e legendas entre os textos; reabertura validada pela composição em teste.
- [x] Criar catálogo visual para escolher e reabrir páginas salvas.

**Etapa atual: organização básica, em andamento.** Biblioteca, editor, busca/tags e backup restaurável estão conectados. Colar prints, arrastar arquivos e demais entregas da etapa continuam no checklist correspondente.

Hoje o desktop prepara o banco padrão, apresenta a biblioteca e oferece páginas com texto, código e imagens. A navegação interna confirma alterações pendentes; salvar continua manual. Agenda e assistente continuam futuros.

### Refinamento visual e personalização

- [x] Refinar hierarquia, espaçamento, barra lateral e editor de notas.
- [x] Centralizar paleta e estilos em recursos semânticos dinâmicos.
- [x] Conferir prévias em janela ampla e compacta, com Salvar visível.
- [x] Validar uma paleta alternativa alterando recursos da janela existente.
- [x] Verificar build sem avisos, formatação e os 57 testes existentes.
- [ ] Criar editor de paletas para o usuário, com prévia e restauração do padrão.
- [ ] Salvar e carregar preferências, validar contraste e tratar configuração inválida.

Implementação visual disponível para revisão; a personalização pelo usuário ainda não está entregue. Veja o [capítulo de temas](../../docs/estudos/07-temas-e-interface.md).

Última verificação: **414/414 casos aprovados no Ubuntu/WSL**, build sem avisos/erros e formatação aprovada; 15 casos aprovados intactos. Capturas ampla/compacta/anexo/nomes longos inspecionadas, com SQLite e recursos dinâmicos conferidos. Contraste mínimo 5,882:1 nas 42 combinações avaliadas, incluindo a nova superfície da página. Pacote Windows isolado abriu, respondeu e fechou normalmente. Revisão humana/interativa e CI remota continuam pendentes; assinatura digital ainda não foi feita. Clipboard WSL permanece em baixa prioridade.

## Como ler os status

| Status | Significado |
| --- | --- |
| Concluída | Implementada no escopo indicado, com verificações aplicáveis registradas |
| Em andamento | Etapa iniciada com entregas parciais; não significa que todos os itens estejam sendo codificados |
| A iniciar | Planejada, sem implementação iniciada |
| Contínua | Atividade que acompanha as entregas, como documentação e estudos |

`[x]` identifica uma entrega concluída no escopo descrito. `[ ]` identifica trabalho pendente. Uma etapa só termina quando seus itens essenciais e seus critérios de conclusão forem atendidos. Revisão humana e evidências continuam necessárias, conforme o CODEX. Não usar percentuais de progresso sem estimativa de esforço por item.

## Visão por etapa

| Etapa | Objetivo | Status |
| --- | --- | --- |
| Preparação | Visão do produto, identidade e processo de trabalho | Concluída para o início; evolução contínua |
| 0 | Fundação desktop e ferramentas locais | Concluída no escopo local |
| 1 | Organização básica e estudos | Em andamento |
| 2 | Rotina, agenda e visão semanal | A iniciar |
| 3 | Assistente por texto e voz | A iniciar |
| 4 | Resumo do dia, clima e notícias | A iniciar |
| 5 | Finanças pessoais | A iniciar |
| 6 | Cofre de credenciais | A iniciar |
| 7 | Pesquisa científica e assistência avançada | A iniciar |
| Transversal | Qualidade, documentação e material de estudo | Contínua |

Esta é uma ordem de dependências, sem datas de entrega prometidas. Finanças e cofre podem mudar de ordem conforme prioridade. Machine Learning próprio é uma possibilidade futura, não pré-requisito das primeiras versões.

## Preparação e decisões

- [x] Escolher Trama como nome e execução inicialmente local e individual.
- [x] Registrar visão do produto e modelo de documentação em Word.
- [x] Criar proposta inicial de logo e direção visual futurista.
- [x] Definir base C#, Avalonia, MVVM e EF Core com SQLite.
- [x] Registrar convenções, limites de segurança e fluxo TDD no CODEX.
- [x] Criar central de estudos com arquitetura e primeira implementação.
- [ ] Validar disponibilidade de marca e preparar versões vetorial e de ícone quando a identidade for consolidada.

## Etapa 0 Fundação técnica

**Status: concluída no escopo local.** Regras de negócio futuras e execução remota da CI não fazem parte desta conclusão.

- [x] Criar solução .NET e projetos Domain, Application, Infrastructure e Desktop.
- [x] Criar projetos de testes unitários e de integração.
- [x] Fixar SDK, pacotes, ferramentas locais e dependências transitivas.
- [x] Configurar nullable, analisadores, formatação e tratamento de advertências.
- [x] Abrir janela Avalonia com logo, tema escuro e ViewModel.
- [x] Montar dependências da janela no ponto de entrada do desktop.
- [x] Validar SQLite real em memória, sem acessar banco pessoal.
- [x] Preparar contexto EF e fábrica design-time com caminho explícito.
- [x] Validar ferramentas EF sem criar banco ou migração.
- [x] Configurar workflow Windows para build, formatação e testes.
- [x] Verificar build e testes locais e registrar resultados.

Critério atendido: solução compilável, janela inicial e ferramentas técnicas funcionando localmente. **Ainda pendente fora desse escopo:** executar e validar a CI no GitHub quando houver repositório remoto configurado.

## Etapa 1 Organização básica

**Status: em andamento.** Biblioteca, editor, persistência, busca/tags e backup restaurável estão conectados; tipos/modelos, relações e melhorias de escrita ainda não encerram esta etapa.

### Primeira nota na aplicação

- [x] Especificar criação sem exigir projeto ou tags.
- [x] Escrever e executar testes antes da implementação.
- [x] Receber aprovação dos testes.
- [x] Validar que título ou corpo contém texto.
- [x] Preservar conteúdo, espaços e quebras de linha.
- [x] Gerar identificador e solicitar uma gravação pelo contrato.
- [x] Distinguir falha de validação, falha de persistência e sucesso.
- [x] Fazer os 14 testes aprovados passarem sem alterá-los.
- [x] Documentar o fluxo e as limitações para estudo.

### Persistência real Esquema e repositório concluídos no escopo testado

- [x] Especificar o esquema Notes e escrever testes SQLite em disco sem alterar produção.
- [x] Demonstrar o RED por ausência de migração e tabela e preservar os 18 testes anteriores.
- [x] Receber aprovação dos dez testes do esquema antes de implementar.

- [x] Especificar gravação, consulta por ID e recuperação pelo adaptador em testes de integração.
- [x] Preparar INoteReader sem alterar INoteRepository nem seus consumidores.
- [x] Receber aprovação dos 14 casos do repositório antes de implementar.
- [x] Definir política padrão LocalApplicationData/Trama, fora do checkout no uso padrão.
- [x] Preparar oito testes de caminho, migração, preservação, cancelamento e falha de diretório.
- [x] Receber aprovação dos testes de inicialização e implementar LocalDatabase.
- [x] Criar modelo de armazenamento NoteRecord, mapeamento EF e migração inicial.
- [x] Implementar repositório SQLite para gravação e consulta por ID.
- [x] Validar integração de CreateNote com o SQLite, falhas esperadas e cancelamento prévio.
- [x] Tratar falhas e coordenar as transações das operações implementadas.
- [x] Aplicar as migrações conhecidas pela inicialização local.
- [ ] Definir backup e recuperação para futuras atualizações do banco pessoal.
- [x] Testar gravação e leitura por SQL em SQLite temporário em disco.
- [x] Demonstrar recuperação por nova conexão nos testes de esquema.
- [x] Demonstrar recuperação em uma nova sessão pela composição e ViewModel da biblioteca.
- [ ] Conferir reinicialização nativa do aplicativo no Windows com o fluxo visual completo.

### Interface e organização

- [x] Preparar sete casos de editor, composição, feedback, preservação de rascunho e salvamento pendente.
- [x] Receber aprovação antes de implementar esse primeiro fluxo visual.

- [x] Criar editor de novas notas.
- [x] Criar lista de Não organizadas e abertura de páginas existentes.
- [x] Conectar a interface ao caso de uso e ao repositório real, com contexto por operação.
- [x] Exibir salvando, salvo, validação e erro; preservar rascunho nas falhas.
- [x] Editar e reabrir páginas existentes.
- [x] Separar áreas e assuntos por espaços e cadernos.
- [ ] Definir tipos de anotação e modelos específicos.
- [ ] Criar modelos de conceito, resumo, exercício, dúvida, fichamento e experimento.
- [ ] Organizar projetos, áreas, tags e relações entre registros.
- [x] Implementar busca textual de páginas e filtro por tag exata.
- [x] Associar e editar tags por página salva.
- [x] Adicionar blocos de código com sintaxe e imagens anexadas com legenda.
- [x] Implementar colagem de prints e drop de arquivos na página.
- [x] Concluir a validação automatizada do passo 5 no WSL.
- [ ] Revisar o diff e conferir integração nativa do passo 5.
- [x] Implementar lixeira e recuperação de espaços/cadernos/páginas; 454 testes aprovados e pacote isolado para revisão.
- [ ] Definir arquivamento e eventual exclusão permanente, fora da lixeira recuperável atual.
- [ ] Exportar notas e anexos em formatos acessíveis.
- [x] Implementar backup consistente e testar restauração em perfil limpo.

Critério de conclusão: criar, editar, relacionar e encontrar conteúdo; reiniciar sem perda; restaurar banco e anexos sem depender dos arquivos originais. OCR e busca semântica pertencem à expansão.

## Etapa 2 Rotina e visão semanal

**Status: a iniciar.**

- [ ] Planejar tarefas, compromissos, prazos e blocos de horário.
- [ ] Implementar recorrências, fuso e configuração de início da semana.
- [ ] Definir lembretes e comportamento com aplicativo fechado.
- [ ] Registrar trabalho e estudo efetivamente realizados por dia.
- [ ] Diferenciar áreas por cores, com rótulos e ícones complementares.
- [ ] Criar painel diário e retrospectiva semanal.
- [ ] Comparar planejado e realizado sem alterar o histórico ao reagendar.

Critério de conclusão: recorrências sem duplicação indevida, totais verificáveis e histórico preservado.

## Etapa 3 Marco de voz

**Status: a iniciar.**

- [ ] Levantar hardware e testar modelo local no computador de destino.
- [ ] Integrar chat com provedor substituível, inicialmente avaliando Ollama.
- [ ] Implementar botão de voz e avaliar whisper.cpp em português.
- [ ] Selecionar síntese de voz e permitir interromper a resposta.
- [ ] Exibir transcrição corrigível e estados reais do assistente.
- [ ] Consultar notas e agenda por conversa.
- [ ] Criar notas e tarefas por ações validadas.
- [ ] Abrir aplicativos por catálogo de programas cadastrados.
- [ ] Aplicar permissões e confirmações conforme impacto da ação.
- [ ] Manter texto e recursos locais utilizáveis quando a IA falhar.

Critério de conclusão: fluxos básicos em português, cancelamento e erros claros, sem execução arbitrária de comandos pelo modelo.

## Etapa 4 Resumo do dia e fontes externas

**Status: a iniciar.**

- [ ] Apresentar agenda, pendências e vencimentos ao abrir.
- [ ] Consultar clima por localização configurada.
- [ ] Selecionar temas, região e fontes de notícias.
- [ ] Exibir fonte, link e horário de atualização.
- [ ] Configurar saudação falada e comportamento offline.

Critério de conclusão: informações atuais ou explicitamente sinalizadas como indisponíveis/desatualizadas; falhas de rede não bloqueiam o acervo local.

## Etapa 5 Finanças pessoais

**Status: a iniciar.**

- [ ] Cadastrar receitas e despesas manuais, categorias e moedas.
- [ ] Separar valores previstos e realizados.
- [ ] Controlar contas, vencimentos, recorrências e pagamentos.
- [ ] Integrar lembretes e painel financeiro.
- [ ] Verificar precisão decimal e preservação do histórico.

Critério de conclusão: totais por período e moeda corretos, com pagamento e previsão separados. Não inclui processamento de pagamentos nem integração bancária.

## Etapa 6 Cofre de credenciais

**Status: a iniciar.**

- [ ] Definir criptografia, gestão de chaves e recuperação.
- [ ] Implementar desbloqueio e bloqueio por inatividade.
- [ ] Proteger cópia para a área de transferência e anexos sensíveis.
- [ ] Excluir segredos de logs, busca geral, prompts e exportações comuns.
- [ ] Implementar backup protegido e demonstrar restauração.
- [ ] Revisar segurança antes de usar credenciais reais.

Critério de conclusão: dados protegidos e recuperáveis conforme a política de chaves, com limites efetivos de acesso do assistente.

## Etapa 7 Pesquisa e assistência avançada

**Status: a iniciar.**

- [ ] Organizar pesquisa, perguntas, objetivos, métodos, resultados e limitações.
- [ ] Relacionar afirmações, citações, fontes e páginas.
- [ ] Avaliar importação e exportação BibTeX/RIS.
- [ ] Avaliar OCR de prints e busca semântica.
- [ ] Avaliar palavra de ativação, escuta contínua e memória de preferências.
- [ ] Criar avaliações do assistente antes de personalização ou fine-tuning.
- [ ] Avaliar componentes próprios de ML com dados autorizados e versionados.

Critério de conclusão: cada recurso aprovado possui avaliação própria, fontes verificáveis e controle de privacidade. Treinar um modelo de linguagem do zero permanece uma possibilidade de pesquisa, sem compromisso de implementação.

## Atividades contínuas e pendências técnicas

- [x] Manter [README](../../README.md), [CODEX](../../CODEX.md) e [central de estudos](../../docs/estudos/README.md) disponíveis.
- [ ] Atualizar este checklist e o capítulo de estudo a cada entrega.
- [ ] Executar testes e verificações aplicáveis a cada mudança de software.
- [ ] Apresentar diff, riscos e evidências para revisão humana.
- [ ] Validar CI remota quando houver repositório e conexão configurados.
- [ ] Corrigir o caminho antigo IDEA em `tools/render_with_word.ps1` antes de reutilizar esse utilitário.
- [ ] Verificar acessibilidade, instalador e distribuição nas etapas apropriadas.

Itens recorrentes ficam desmarcados por representarem obrigações futuras contínuas, não falhas da última entrega. Não remover registros históricos de verificações anteriores.

## Como executar o estado atual

Avalonia é o framework da interface. O projeto executável é `Trama.Desktop`; você pode iniciá-lo pelo terminal, sem abrir um navegador.

No PowerShell:

```powershell
Set-Location 'C:\Users\joset\www\Trama'
dotnet --version
dotnet restore Trama.slnx --locked-mode
dotnet build Trama.slnx --configuration Release --no-restore
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --no-build
```

O SDK precisa atender ao `global.json` (`10.0.401`, permitindo patches da mesma faixa). A primeira restauração precisa de internet para baixar pacotes. A ferramenta `dotnet-ef` não é necessária para simplesmente abrir a janela; use `dotnet tool restore` ao trabalhar com EF.

Após modificar código, compile novamente antes de usar `--no-build`, ou execute `dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release` para permitir compilação automática. Feche a janela anterior se o Windows indicar que o executável está em uso.

Para verificar a solução após o build:

```powershell
dotnet test Trama.slnx --configuration Release --no-build
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Ao abrir, você verá a logo e a tela inicial. A ausência de editor é o estado esperado desta versão, não uma falha de instalação.
