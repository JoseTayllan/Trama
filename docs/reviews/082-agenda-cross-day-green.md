# Agenda — regras de término implementadas

Data: 05/11/2026. Para revisão do diff.

Aprovação explícita dos 18 casos e regras recebida. Alterados somente AgendaTaskValidation e ApplicationCloseCoordinator na implementação: intervalos planejados consideram a data final; conclusão encaminha o instante informado ou usa o relógio, rejeitando futuro e dia anterior ao início planejado antes da gravação. Contratos e todos os testes aprovados intactos.

Verificação WSL: 18 focados aprovados; suíte completa com **787 aprovados (131 unitários e 656 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e format --verify-no-changes aprovados. Perfis fictícios; nenhuma nova execução/publicação Windows.

Revisão do código: mantidas as ações Cancelar/Manter abertas, rejeição de IDs, proteção de rascunhos, pausa de autosave e validação transacional existente. Futuro é comparado como instante; dia planejado, na data do offset informado. Não há nova escrita de log ou segredo literal nos arquivos alterados, em inspeção limitada. Sem novas dependências ou migração.

Limites: PlannedEndDate não é persistido pelo SQLite atual e ainda não deve ser exposto pelo formulário; CompletedAt não possui controle nativo no diálogo. A próxima fatia exige testes próprios de armazenamento/migração/backup, seguida dos campos visuais. Regras de hora inicial exata e semana visual não foram acrescentadas. Confirmação/tela da lixeira pendentes.

Esforço baixo a moderado, sem novos subagentes: uma rodada focada e uma completa. Reutilizar fixtures e manter o escopo de armazenamento separado da interface reduz iterações. Tokens/custo exato não medidos. Revisão humana do diff e aceite dos campos visuais futuros não estão implícitos no GREEN.

[Estudo](../estudos/75-datas-de-termino-regras-implementadas.md) · [RED](081-agenda-cross-day-red.md).
