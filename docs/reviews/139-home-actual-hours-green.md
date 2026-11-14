# Horas realizadas em Sua semana

Ao salvar uma tarefa como Em andamento, a Agenda registra o início real. Ao concluir, o Início soma o tempo corrido dos intervalos concluídos, limitado à semana local de segunda a domingo. Os horários planejados continuam em campos próprios. O cálculo inclui pausas e usa a última execução. Tarefas sem início real permanecem sem duração registrada.

A migração 15 adiciona AgendaTaskStarts. O registro do início participa da transação da tarefa. A migração não atribui horários a registros anteriores. A validação de backup confere o instante gravado e a integridade da referência à tarefa.

Build Release, formatação e verificação do modelo EF passaram no WSL. Os dois casos aprovados ficaram intactos. A suíte completa passou com 1.355 testes, sendo 224 unitários e 1.131 de integração. Em 13/11/2026, o responsável confirmou horas realizadas no uso e informou que gerou o executável Win64 atualizado. A revisão do diff e o commit 1 seguem pendentes.
