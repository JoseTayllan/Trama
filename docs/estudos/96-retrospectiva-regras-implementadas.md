# 96 — Cálculos da retrospectiva implementados

Data: 07/11/2026. Implementação após aprovação explícita dos 32 casos e contratos, preservados. Diff para revisão.

AgendaRetrospectiveBuilder implementa IAgendaRetrospectiveBuilder na camada Application. É um cálculo puro: recebe os dados, instante de captura e fuso, sem acessar banco, relógio global ou interface. Não existe fechamento persistido ou painel funcional nesta entrega.

## Fluxo do cálculo

O builder reutiliza AgendaWeekProjector com entrada vazia para obter segunda/domingo e os limites representáveis da semana. Converte o instante da captura para o fuso informado e exige uma data posterior ao domingo. Copia e valida toda a entrada antes do recorte: snapshots, descrições, identidade única e data de conclusão não posterior à captura.

A população é o planejamento sobreposto à semana, uma entrada por ID. A ordenação é determinística por início, horário (ausentes depois), título ordinal e ID. Continuidades iniciadas antes da segunda-feira entram no total e em CarriedInCount; não recebem um início fictício nas barras diárias.

Cada tarefa selecionada incrementa sua categoria e sua data de início, quando esta está dentro da semana. Pendente e Em andamento são contados separadamente; um LastCompletedAt antigo de tarefa reaberta não vira conclusão atual. Concluída sem data vai para CompletedWithoutDateCount. Com data, a conversão ao fuso do relatório decide se incrementa o dia real e CompletedInWeekCount ou CompletedOutsideWeekCount.

As categorias e os sete dias são materializados com contadores zero quando vazios. O percentual usa as conclusões datadas na semana sobre o total planejado sobreposto; arredondamento decimal de uma casa e resultado nulo quando não há tarefas. Não calcula horas, não conclui tarefas e não altera o planejamento.

## Como as contagens se explicam

Total planejado = pendentes + em andamento + concluídas na semana + concluídas sem data + concluídas fora do período.

Total planejado = soma do planejamento nos sete dias + continuidades iniciadas antes.

Concluídas na semana = soma das conclusões diárias = soma das conclusões por categoria.

Os dados capturados e as coleções agregadas são expostos como somente leitura, independentes de substituições/limpeza da lista de entrada. Os records preservam os dados originais. A persistência futura precisará guardar essa captura sem sobrescrita; a cópia em memória não equivale a histórico durável.

## Evidências e limites

Os 32 casos aprovados passaram intactos; evidências finais de build/formatação, suíte e hashes no [relatório](../reviews/103-retrospective-rules-green.md). Apenas um arquivo novo de implementação; src/tests anteriores, contratos aprovados, migrações e locks intactos.

A primeira captura reflete os dados disponíveis naquele momento. LastCompletedAt não reconstrói reaberturas ou mudanças de planejamento anteriores. Não apresentar captura tardia como um estado exato registrado no domingo. Tarefas cujo planejamento não se sobrepõe à semana ficam fora desta população, mesmo que tenham conclusão no período.

Próxima fatia: leitura consistente de tarefa/data real e persistência atômica do fechamento, com proteção contra substituição, criação concorrente e backup compatível. Serviço, disparo automático, comparação histórica e tela terão testes próprios. Sem mudança visual, migração, publicação, Git ou manual/PDF nesta entrega.

Para estudar: por que validar antes de recortar a população? Por que excluir uma tarefa concluída sem data do numerador não significa classificá-la como pendente? Qual garantia ainda falta para a retrospectiva sobreviver ao reinício?

[Regras aprovadas](95-retrospectiva-semanal-regras.md) · [Relatório](../reviews/103-retrospective-rules-green.md).
