# 052 · Listas por parágrafo — RED para revisão

Data: 31/10/2026. Implementação de listas ainda não autorizada; aguardando revisão dos 32 casos conforme CODEX, seção 9.

## Mudanças

- Novo `ParagraphListsTests.cs`, com 32 casos de contrato, editor e persistência.
- Especificação no capítulo 44 e índice de estudos atualizado.
- `rodmap.md` reorganizado em quadro atual, entrega em foco, recursos disponíveis, critérios de conclusão e futuro. O documento anterior foi preservado em `docs/historico/rodmap-antes-das-listas-2026-10-31.md`, com links relativos ajustados.
- Nenhuma alteração em código de produção, dependências, migrações ou perfis pessoais.

## Verificação

| Execução no WSL | Aprovados | Falhas | Ignorados |
| --- | ---: | ---: | ---: |
| Filtro ParagraphListsTests | 3 | 29 | 0 |
| Unitários completos | 77 | 0 | 0 |
| Integração completa | 380 | 29 | 0 |
| Total completo | 457 | 29 | 0 |

Os 454 casos anteriores continuam passando. Build Release terminou sem avisos/erros e a verificação de formatação passou pelo helper `tools/run-wsl.sh test`. A saída não zero da suíte é esperada nesta fase RED.

Motivos: metadados `Lists` descartados no JSON atual; posições/tipos ainda não validados; seletor `ProseListPicker` ausente; páginas continuam gravadas como versão 1 em vez de 4 e metadados de lista rotulados como versão 1 não são rejeitados. Não houve falha de compilação ou dependência ausente usada como RED.

Limite: os casos de interface param na ausência do seletor. As asserções seguintes de edição, histórico e foco precisarão passar após implementar; não foram individualmente comprovadas nesta execução.

## Revisão solicitada e riscos

Revisar os 32 casos e o contrato de página versão 4, mantendo leitura dos formatos 1/2/3 e pacote de backup 1. Riscos principais: recálculo de posições ao editar, interação com estilos/histórico, continuidade da numeração, recuos em linhas quebradas e compatibilidade de backup. A solução precisa validar conteúdo antes de gravar e restaurar.

O diff externo é gerado a partir do snapshot anterior, pois esta pasta não contém `.git`. Ele acompanha testes/documentação e não declara a funcionalidade pronta. Após aprovação: implementar o mínimo, manter estes casos intactos, conferir a suíte e revisar renderização ampla/compacta com perfil fictício.

Esforço: moderado, uma rodada focada e uma completa, sem novos subagentes. Para economizar, usar o filtro durante a implementação e repetir a suíte completa quando houver uma alteração integrada para revisar. Não foi calculado custo monetário ou número de tokens.
