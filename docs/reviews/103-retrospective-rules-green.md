# Retrospectiva — cálculo implementado para revisão

Data: 07/11/2026. Após aprovação dos 32 casos e contratos, mantidos sem alterações.

AgendaRetrospectiveBuilder é o único arquivo novo de implementação. Reutiliza os limites de calendário da semana existente e a validação de snapshots. Valida entradas/identidades/instantes antes do recorte, exige semana encerrada no fuso explícito e materializa o resultado com coleções somente leitura.

Revisão: uma tarefa entra uma vez; planejamento na data inicial, continuidades anteriores em contador separado, conclusão por data real convertida ao fuso. Reabertas não contam como concluídas pelo timestamp antigo. Sem data e fora do período têm contadores próprios. Percentual decimal, uma casa, nulo quando vazio. Categorias e dias vazios são preservados; não há iteração sobre todo o intervalo de tarefas longas. Custo O(n log n) pela ordenação, com agregação linear.

Não há I/O, relógio global, inferência de horas, mutação do planejamento ou conclusão automática. Inspeção não encontrou segredo/logging novo. Contratos, persistência, composição, interface, migrações, dependências e testes anteriores intactos.

Limites para revisão: a população é planejamento sobreposto, não toda conclusão de qualquer tarefa no período. O percentual representa conclusões datadas dentro da semana, com avisos futuros para sem data/fora do período. Captura não reconstrói eventos anteriores sem histórico. O resultado ainda não é salvo em disco; persistência/backup, serviço, comparação e UI permanecem futuros.

Esforço baixo a moderado, sem subagentes: uma implementação, testes focados e uma suíte completa. Reutilizar calendário e validação reduz escopo e risco. Tokens/custo exatos não medidos. Revisão humana do diff pendente; não houve execução nativa Windows, publicação, Git ou manual/PDF.

**Verificação final:** 32 casos e contratos aprovados intactos por hashes; suíte completa com 1.048 aprovados (199 unitários e 849 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Todos os arquivos anteriores de src/tests intactos.

[Estudo](../estudos/96-retrospectiva-regras-implementadas.md) · [RED](102-retrospective-rules-red.md). Evidências/diff: trama-retrospective-rules-green.
