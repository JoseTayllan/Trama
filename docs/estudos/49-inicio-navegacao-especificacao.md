# 49 · Início e navegação: testes para revisão

01/11/2026. A direção de integração e a imagem conceitual foram aprovadas. Esta primeira fatia implementará Início e navegação para os Cadernos, antes da Agenda funcional. Os testes foram preparados; código de produção ainda não foi alterado.

Atualização posterior: os 17 casos foram aprovados e implementados, sem alteração dos testes. Consulte o [capítulo 50](50-inicio-e-navegacao-implementados.md). O restante deste documento preserva a especificação e a fase RED.

## Escopo

Ao concluir a preparação do perfil, a janela deve apresentar Início. A barra superior identifica o módulo ativo. Os botões **Abrir acervo** e **Continuar página** levam à biblioteca existente; o segundo retoma o rascunho sem recarregar ou salvar automaticamente por causa do clique.

O módulo de Cadernos precisa conservar a instância do editor ao ficar oculto. Seleção, rolagem, texto/atributos/listas, histórico nativo, filtros e resultados de busca devem continuar ao retornar. O autosave continua com seu comportamento anterior; fechar o aplicativo a partir de Início ainda resolve alterações da página oculta.

Backup permanece acessível em Início. Durante a preparação do armazenamento, os comandos de navegação não podem tentar acessar um módulo ainda não inicializado. O caminho anterior de editor independente, sem biblioteca, continua compatível.

Agenda, métricas do dia e demais dados da imagem são proposta visual. Nesta fatia, a Agenda fica oculta ou desabilitada; não exibir tarefas, horas ou contagens fictícias como conteúdo do usuário. Os resumos reais serão outra etapa.

## Contrato visual usado pelos testes

| Controle | Uso esperado |
| --- | --- |
| `HomeWorkspace` | Área da tela Início |
| `HomeNavigationButton` | Alternar para Início |
| `NotebooksNavigationButton` | Alternar para Cadernos |
| `OpenNotebooksButton` | Acesso ao acervo pelo cartão |
| `ContinueNotebookButton` | Retomar o editor existente |
| `OpenBackupButton` | Ação global de backup já existente |
| Classe `module-active` | Identificar visualmente o módulo selecionado |

Esses nomes viabilizam as verificações sem acrescentar stubs de produção ou exigir tipos de tela que ainda não existem. A escolha do host deve preservar também os controles visuais; manter somente os ViewModels não preserva, por si, a pilha nativa de desfazer.

## Os 17 casos

Arquivo: [HomeModuleNavigationTests.cs](../../tests/Trama.IntegrationTests/Knowledge/HomeModuleNavigationTests.cs).

| Grupo | Casos | Proteções |
| --- | ---: | --- |
| Entrada e preparação | 2 | Início ativo após preparar o perfil; comandos indisponíveis antes disso |
| Navegação e continuidade | 7 | Acervo, mesma instância/seleção, estilos/listas/undo, rolagem/hierarquia, busca, continuar e salvar sem limpar histórico |
| Autosave oculto | 1 | Gravar após pausa mesmo com Início visível; manter histórico ao voltar |
| Superfície principal | 3 | Agenda/resumos não fictícios, janela compacta e backup global |
| Fechamento | 3 | Salvar, descartar ou cancelar considerando o caderno oculto |
| Compatibilidade | 1 | Editor independente anterior continua renderizando |
| Total | 17 | 16 falhas esperadas e uma proteção aprovada |

O cenário de autosave usa o mecanismo real e aguarda a conclusão, com limite de dez segundos. Os cenários de fechamento usam decisão controlada e SQLite real temporário. Nenhum teste acessa o perfil pessoal, microfone, rede ou Git.

## Resultado e limite do RED

Execução focada: 17 descobertos, um aprovado, 16 falhas esperadas, nenhum ignorado. Execução completa: 77 unitários aprovados; integrações com 436 aprovados e 16 falhas esperadas. Os 512 testes anteriores continuam passando; total atual de 529 casos.

As falhas correspondem à ausência de Início ou dos botões de navegação no aplicativo atual. Elas não provam 16 defeitos independentes: as asserções posteriores de histórico, persistência e fechamento só serão exercitadas quando esses pontos de entrada existirem. A proteção do editor independente já passa.

Uma referência ambígua ao tipo `Button` no helper do próprio teste foi corrigida antes desse RED. Erro de compilação não foi contado como falha comportamental. A versão atual compila e passa pela verificação de formatação.

## Próxima etapa

A seção 9 do [CODEX](../../CODEX.md) exige revisar e aprovar estes casos antes da implementação. Depois da aprovação, preservar os testes, implementar o host e a tela, conferir o visual amplo/compacto e apresentar o diff.

Detalhes: [relatório 056](../reviews/056-home-navigation-red.md). Escopo maior: [integração dos módulos](48-inicio-e-integracao-dos-modulos.md). O [painel do projeto](../../roadmap.html) acompanha a Agenda; não é a tela Início do aplicativo.
