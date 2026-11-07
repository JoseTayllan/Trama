# Projeção semanal e filtros — implementação para revisão

Data: 06/11/2026. Os 36 casos e contratos foram aprovados explicitamente antes de implementar.

AgendaWeekProjector é o único arquivo novo de implementação. Reutiliza a validação existente, valida a coleção antes de filtrar e distribui snapshots preservados pelos sete dias. Contagem única, continuidade pelas datas originais, horários nas extremidades e filtros AND. Ordenação determinística por início/horário/título ordinal/ID; cópias materializadas expostas como somente leitura.

Revisão: não há leitura/gravação, recursos de interface ou dependência invertida. O limite superior é conferido antes de calcular domingo; não há incremento além de MaxValue no laço. A construção visita somente sete dias, mesmo em tarefas com planejamento longo. Nenhum segredo ou logging novo encontrado na inspeção.

Limites relevantes: fim inclusivo inclui a data final à meia-noite; tarefa aberta não se expande sem data final; o leitor precisa fornecer tarefas ativas. Esta entrega não muda a interface nem certifica a renderização dos sinais de estado. Categoria e estado continuam independentes para a próxima fatia visual.

Esforço baixo a moderado, sem subagentes: uma implementação, uma rodada focada e uma suíte completa. Reutilização da validação, snapshots e leitura por período reduziu o escopo. Tokens/custo exatos não medidos. Próximo passo: especificar integração semanal e sinais de estado, sem antecipar regras novas antes da revisão dos testes.

**Verificação:** 36 casos aprovados intactos; suíte completa com 948 aprovados (167 unitários e 781 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram no WSL. Hashes confirmam todos os arquivos anteriores de src/tests e os contratos aprovados intactos.

Evidências e diff em trama-agenda-week-model-green; revisão humana do diff pendente. Sem publicação, execução nativa Windows, migração, Git, dependências ou manual/PDF.

[Estudo](../estudos/88-projecao-semanal-implementada.md) · [Especificação](094-agenda-week-model-red.md).
