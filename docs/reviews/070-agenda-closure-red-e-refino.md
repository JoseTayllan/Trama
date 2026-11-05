# Agenda — refino visual e encerramento em RED

**Atualização de 04/11/2026:** os 17 casos foram aprovados e implementados, preservados. Suíte completa com 657 aprovados; ajustes visuais finais conferidos com oito casos nativos. [Entrega GREEN](071-agenda-closure-rules-green.md). O texto abaixo preserva o histórico RED; SQLite e diálogo continuam pendentes.

Data: 04/11/2026. Refino para revisão; implementação funcional do encerramento não iniciada.

Um arquivo visual de produto alterado: AgendaWorkspaceView.axaml. Agrupamento dos campos, cabeçalho, explicação dos horários e rodapé fixo, mantendo bindings e comandos. QA SQLite/Skia em perfil sintético passou em 1380×1000 e 860×680; capturas inspecionadas. Cadastro com início sem fim, limpeza e edição conferidos. Aparência nativa Windows desta fatia não foi verificada.

Contratos mínimos novos em CompleteAgendaTasks.cs, sem registro no app; ExecuteAsync permanece NotImplementedException. 17 casos novos de regras: 16 falhas esperadas e uma guarda passando. Runner compila e descobre os casos; não apresentar como conclusão persistente, diálogo implementado ou lote SQLite verificado.

Base anterior executada separadamente: 640 aprovados (100 unitários e 540 integrações), zero falhas/ignorados. Build Release sem avisos/erros e dotnet format aprovados. A suíte completa tem casos RED novos; não declarar GREEN total.

## Decisões confirmadas

Preservar planejamento e registrar encerramento real. Considerar Pendentes/Em andamento até hoje, excluir datas futuras e permitir seleção. Aviso posterior: encerrar selecionadas e sair; manter abertas e sair; cancelar. As regras usam o dia local do instante de encerramento. Não inferir duração trabalhada nem alterar horários planejados ao encerrar tarefas de outro dia.

## Riscos e pendências

Persistência atômica, migração/backup, revalidação de seleção e diálogo/fechamento coordenado ainda precisam de testes próprios. Cancelamento posterior dos Cadernos não deve deixar tarefas concluídas por uma saída que não ocorreu. Nenhum teste controlado demonstra essas garantias de banco ou interface. Placeholders vazios dos TimePickers continuam nativos em inglês.

Nenhum teste antigo, migração, pacote ou formato alterado. Sem perfil pessoal, logs novos, chamadas externas, segredos literais ou comandos Git. A revisão de segredos se limita aos arquivos envolvidos, não ao histórico inteiro. README, manual/PDF e publicação anterior preservados.

Esforço moderado, sem novos subagentes: uma execução RED, uma rodada da base e conferência visual dirigida. Reaproveitar estes contratos e o mapa de fechamento na próxima fatia evita nova exploração completa. Tokens/custo monetário não foram medidos.

Revisão dos 17 casos e aceite visual/diff pendentes. [Estudo](../estudos/63-encerramento-e-refino-da-agenda.md).
