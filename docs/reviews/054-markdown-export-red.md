# 054 · Exportação Markdown — RED para revisão

01/11/2026. Listas validadas no uso pelo responsável; a revisão explícita do diff da implementação permanece separada. Próxima entrega: página aberta em Markdown e imagens relativas. Nenhuma implementação de exportação nesta etapa.

## Alterações

- `MarkdownExportWorkflowTests.cs`: 26 casos, cobrindo fluxo da página, conteúdo, imagens, caminhos, falhas/cancelamento e descarte de recursos.
- Capítulo 46 com escopo/limites e índice de estudos atualizado.
- Roadmap: listas validadas no uso e exportação como entrega em foco, preservando o desenho enxuto.
- Código de produção, contratos existentes, migrações, dependências, manual/PDF e perfis pessoais preservados.

## Resultados

| Verificação | Aprovados | Falhas | Ignorados |
| --- | ---: | ---: | ---: |
| Execução focada | 2 | 24 | 0 |
| Unitários completos | 77 | 0 | 0 |
| Integração completa | 411 | 24 | 0 |
| Total completo | 488 | 24 | 0 |

Os **486 anteriores continuam passando**. Restore locked, build Release sem avisos/erros e formatação passaram pelo helper WSL. O comando focado selecionou `FullyQualifiedName~MarkdownExportWorkflowTests`; a suíte completa usou `tools/run-wsl.sh test`. A fixture foi ajustada antes da revisão para usar `ActivatorUtilities` e permitir dependências pela composição existente; a versão final foi novamente conferida.

Os 24 casos falham na asserção “A página aberta ainda não oferece exportação em Markdown.” Todos alcançam o editor atual e param no mesmo botão ausente. Não são 24 causas independentes nem prova de que as asserções posteriores de formatação/arquivos já foram exercitadas. As duas proteções atuais passam. Não houve alteração de produção para fabricar o RED nem tipo inexistente impedindo a compilação.

## Aprovação solicitada

Revisar os 26 casos e estes limites: exportar somente a página aberta, incluindo alterações não salvas; pasta nova com `pagina.md` e imagens relativas; preservar ênfase/listas/código; não exportar cor/tamanho/marca-texto específicos como HTML; não escrever no banco ou sobrescrever destino existente. Backup permanece como recuperação fiel, exportação como saída portátil.

Riscos: escapes e cercas de código, publicação/limpeza de arquivos, cancelamento durante I/O, streams e preservação do rascunho. A aparência e o ciclo integrado Windows/Linux serão conferidos após implementar. Não declarar o módulo concluído antes dessas etapas e da revisão do diff.

Snapshot/diff e TRX ficam externamente em `trama-markdown-export-red`, sem inicializar Git. Verificação de hashes protege fontes e testes anteriores; a varredura textual de segredos é limitada e não equivale a auditoria completa. O novo código de testes usa apenas conteúdo fictício.

Esforço moderado, sem novos subagentes: uma execução focada e verificações completas antes/depois do ajuste da fixture. Economia para o GREEN: manter o mapa existente e usar o filtro de exportação durante mudanças locais; ampliar para a suíte completa na integração. Sem estimativas de tokens ou custo monetário por falta de medição.
