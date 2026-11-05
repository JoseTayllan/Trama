# 056 · Tela Início e navegação — RED para revisão

01/11/2026. O responsável aprovou a direção e a imagem conceitual. A implementação começa pela integração Início/Cadernos. Esta entrega contém testes e documentação; nenhuma alteração nos fontes de produção de `src/`.

## Alterações

- `HomeModuleNavigationTests.cs`: 17 casos de entrada, continuidade do editor, autosave, busca, layout, backup, fechamento e compatibilidade.
- Capítulo 49 e índice de estudos atualizados.
- Roadmap passa a acompanhar o módulo de Agenda, com primeira fatia em RED e demais etapas a iniciar. O aceite de uso dos Cadernos e a revisão formal ainda pendente continuam registrados separadamente.
- Gerador/painel deixam de fixar título, quantidade de marcos, links e checklist da exportação. O módulo exibido deriva do Markdown. Essa mudança é somente da ferramenta de acompanhamento, sem implementar Início ou Agenda no produto.

## Resultados

| Execução no WSL | Aprovados | Falhas | Ignorados |
| --- | ---: | ---: | ---: |
| Filtro HomeModuleNavigationTests | 1 | 16 | 0 |
| Unitários completos | 77 | 0 | 0 |
| Integração completa | 436 | 16 | 0 |
| Total completo | 513 | 16 | 0 |

Os **512 anteriores passam**, totalizando agora 529 casos. Restore locked, build Release sem avisos/erros e format verify passaram pelo helper `tools/run-wsl.sh test`.

O painel atualizado foi conferido offline em navegador headless: módulo Agenda correto, seis marcos, filtros de revisão/a iniciar e busca por título/critérios. Larguras de 1440, 900 e 390 pixels sem overflow horizontal, erros de script ou requisições externas. Os links locais e hashes dos fontes/testes anteriores também foram conferidos.

Motivos esperados: “A janela ainda não oferece a tela Início” ou “A navegação principal ainda não oferece o controle ...”. Os 16 casos param nesses pontos de entrada ausentes; as asserções seguintes de estado/editor ainda serão exercitadas no GREEN. Não representam 16 causas independentes já diagnosticadas. O editor independente, sem biblioteca, já funciona.

Comandos: filtro `FullyQualifiedName~HomeModuleNavigationTests` no projeto de integração e, para a suíte completa, `wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test`.

A primeira tentativa encontrou uma ambiguidade no helper de testes entre o método `Button` e o tipo do controle. A referência foi qualificada e o runner executou os casos. Essa tentativa de compilação não foi usada como RED nem exigiu alteração de produção.

## Revisão solicitada

Aprovar os 17 casos antes de implementar, conforme CODEX §9. Pontos centrais: não recriar o editor ao alternar módulos; manter autosave/histórico; resolver edições ocultas ao fechar; não ativar Agenda nem publicar métricas fictícias nesta fatia. O protótipo visual descreve a direção; os dados de exemplo não são requisitos para persistir tarefas agora.

Riscos: remontar controles e perder a pilha nativa de desfazer; reiniciar rolagem/seleção; ocultar alterações no fechamento; interferir no backup e na inicialização. A integração futura da Agenda exigirá sua própria proteção de formulário, persistência e revisão de testes.

Snapshot, diff e TRX ficam externamente em `trama-home-navigation-red`. Fontes de produção e testes anteriores são comparados por hash; não houve migração, dependência nova, alteração de banco pessoal ou publicação de executável. Manual/PDF e README de apresentação permanecem intactos.

## Esforço e limites

Sem novos subagentes ou comandos Git. Esforço moderado: preparação, correção de compilação no helper, uma execução focada comportamental e uma completa. Usar o filtro de navegação durante o GREEN e reutilizar o mapa existente para reduzir consumo; não repetir a exploração inteira. Sem medição de tokens ou custo monetário.
