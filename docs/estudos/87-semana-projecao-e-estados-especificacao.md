# 87 — Semana, filtros e diferenciação dos estados

Data: 06/11/2026. Especificação RED, aguardando aprovação antes de implementar.

IAgendaWeekProjector propõe transformar snapshots existentes em sete dias de segunda a domingo. AgendaWeekSnapshot contém os dias e a contagem de tarefas distintas; AgendaWeekEntry mantém o snapshot original, a data exibida, horários pertinentes e indicadores de continuidade. Somente contratos e 36 casos novos foram adicionados. Não há implementação do projetor ou mudança na interface nesta etapa.

## Regras propostas

- Qualquer dia de referência determina a mesma semana, inclusive na mudança de mês/ano. Dias vazios permanecem visíveis.
- Cada tarefa aparece nos dias planejados inclusivos, recortados pela semana. Sem data final, aparece somente no dia inicial, mesmo pendente.
- Identidade, categoria, estado e planejamento originais são preservados. A contagem considera IDs distintos, sem contar cada cartão como tarefa nova.
- Horário inicial aparece somente na data inicial real do planejamento; horário final somente na data final. Dias intermediários usam continuidade, sem inventar horários. Fim à meia-noite mantém a data final inclusiva.
- Filtros opcionais de categoria e estado são combinados com AND. Sem resultados, continuam existindo sete dias vazios.
- Ordem: data inicial original, horário inicial (ausentes depois), título e ID. Tarefas iniciadas antes da semana mantêm essa precedência.
- Entrada nula, snapshots inválidos ou IDs duplicados são rejeitados antes dos filtros. Valores de filtro indefinidos são rejeitados. O resultado não depende de alterações posteriores na coleção de entrada.
- A primeira semana representável é válida. Referência que exigiria um domingo além de DateOnly.MaxValue é rejeitada, evitando semana parcial.

## Direção visual solicitada

Categoria e estado têm sinais independentes. A categoria conserva a faixa/cor; Pendente terá círculo contornado e texto, Em andamento indicador de progresso e texto, Concluída check e texto. Não depender apenas de cor. Os recursos de cor ficam na paleta dinâmica do Desktop, sem brushes ou ícones na camada Application.

Os testes atuais protegem a preservação dos estados/categorias e seus filtros, não a renderização desses sinais. Essa renderização terá especificação própria na fatia da interface. O [protótipo existente](../prototipos/agenda-semanal-v1.png) continua referência de composição, sem imagem nova nesta etapa.

## Verificação e próximos passos

36 casos falham na asserção de existência do projetor ainda ausente; compilação e formatação passam. Os 912 testes anteriores são executados separadamente, excluindo apenas esta classe. Resultados finais e evidências no [relatório](../reviews/094-agenda-week-model-red.md).

Após aprovação: implementar projeção pura, manter os testes intactos e verificar a suíte completa. Depois integrar leitor por período, navegação semanal, filtros e editor existente, incluindo sinais de estado acessíveis e janela compacta. Sem migração, gravação, publicação, Git ou manual/PDF nesta fatia.

Para estudar: por que separar consulta e projeção? Por que uma tarefa pode produzir vários cartões sem mudar de identidade? Por que categoria e estado precisam de sinais distintos?
