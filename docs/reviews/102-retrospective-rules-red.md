# Retrospectiva — regras em RED

Data: 07/11/2026. Aprovação dos 32 casos/contratos pendente conforme CODEX.md §9.

Responsável validou a agenda no uso e pediu continuidade no painel de fechamento semanal. Confirmou preservar o fechamento após edições/reaberturas, contagem única, planejamento na data inicial e conclusão na data real; legados sem data devem ser identificados.

Somente AgendaWeekRetrospective.cs, IAgendaRetrospectiveBuilder.cs e AgendaRetrospectiveRulesTests.cs adicionados ao produto/testes. Records de transporte e contrato mínimos; sem implementação do builder, SQLite, serviços, composição ou tela. Nenhum teste anterior, migração ou lock alterado.

32 casos: identificação de semana/fuso/captura, rejeição de semana aberta, meia-noite local, dias/categorias vazios, continuidade, contagem única, conclusão por data real/fuso, legados, conclusões fora do período, reabertura, categorias, percentual, cópia da entrada, conciliação das contagens e validação de entradas/datas. As falhas esperadas são asserções de existência da implementação ausente, sem stub funcional ou falha de compilação como substituto de RED.

Pontos para revisão: população inclui planejamento sobreposto à semana, com continuidades contadas uma vez; barras de planejamento usam início original e destacam iniciadas antes. Percentual usa somente conclusões datadas dentro da semana. Concluídas sem data ou fora do período não entram nesse numerador e não são apresentadas como abertas. Sem tarefas, percentual nulo. Arredondamento de uma casa decimal.

O fechamento é uma captura dos dados disponíveis, não reconstrução de eventos anteriores que o banco não guarda. A persistência futura deverá congelar o resultado e proteger criação concorrente/backup. Comparação histórica, reflexões, horários trabalhados, agendamento automático e UI não estão implementados nesta fatia.

Inspeção não encontrou segredo literal ou logging novo. Fixtures fictícias e fuso controlado; sem uso de relógio/rede nas regras puras. Esforço moderado, sem subagentes: exploração concentrada nos registros existentes, uma rodada focada e uma regressão. Reutilizar snapshots/validação e separar regras de persistência reduz retrabalho. Tokens/custo exatos não medidos.

**Verificação:** 32 falhas esperadas pela implementação ausente; build Release sem avisos/erros e formatação aprovados no WSL. Os 1.016 anteriores passaram separadamente (167 unitários e 849 integrações), sem falhas/ignorados. Hashes confirmam src/tests anteriores intactos; a suíte incluindo a classe nova permanece em RED.

[Estudo](../estudos/95-retrospectiva-semanal-regras.md). Evidências/diffs: trama-retrospective-rules-red.
