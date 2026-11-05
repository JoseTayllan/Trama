# 61 — Agenda: tarefas abertas e cadastro em sequência

**Atualização de 04/11/2026:** nove casos e os dois ajustes de expectativas antigas aprovados; implementação com 640 testes passando. [Entrega GREEN](62-agenda-tarefas-abertas-implementadas.md). O texto abaixo preserva a especificação RED histórica.

Data: 04/11/2026. Especificação em RED; nenhuma correção de produto implementada nesta etapa.

## Comportamento solicitado

Permitir salvar tarefas Pendentes e Em andamento com início e sem fim. Gravar não conclui a tarefa. O botão Salvar tarefa deve confirmar a escrita e preparar um formulário novo e limpo para o próximo cadastro, inclusive após editar uma tarefa existente. Uma falha deve preservar o rascunho. O formulário não deve apagar edições novas surgidas durante uma gravação pendente.

A lista precisa distinguir um início sem fim de uma tarefa sem horário. Proposta de texto: `19:27 · sem fim`. Tarefas sem nenhum horário continuam permitidas; fim sem início e intervalos invertidos continuam inválidos. O horário planejado não equivale automaticamente a uma medição de trabalho realizado.

## Primeira fatia: testes para revisão

Arquivo: `tests/Trama.IntegrationTests/Knowledge/AgendaOpenEndedTaskTests.cs`.

| Casos | Cobertura | Resultado RED |
| --- | --- | --- |
| 2 | Gravar e reabrir tarefa Pendente/Em andamento sem fim, preservando identidade e estado | Falham na validação atual |
| 2 | Salvar pelo botão após criar/editar e preparar formulário limpo, preservando a tarefa na lista | Falham porque o formulário mantém a edição |
| 1 | Falha de validação preserva o rascunho | Passa |
| 1 | Exibir início sem fim sem alegar ausência de horário | Falha no conversor atual |
| 2 | Fim sem início e intervalo invertido continuam rejeitados | Passam |
| 1 | Backup/restauração preservam tarefa sem fim | Falha na gravação inicial |

Total: nove descobertos, seis falhas comportamentais e três proteções aprovadas; zero ignorados. As asserções posteriores à gravação ainda não são alcançadas nos casos rejeitados pela regra antiga. A suíte completa anterior de 632 aprovados é histórica e não foi repetida nesta especificação.

Há uma incompatibilidade explícita com `SaveAgendaTaskTests.IncompleteOrNonPositiveTimeRange_IsRejectedWithoutWriting`: a entrada `[InlineData(18, null)]` representa a regra anterior e deve ser removida somente após revisão explícita. Os demais casos e asserções continuam válidos. Não enfraquecer testes para esconder regressões.

## Fatias seguintes solicitadas

O responsável confirmou que, ao fechar, tarefas Pendentes e Em andamento devem ser consideradas no aviso. Escolhas: **Encerrar tarefas e sair**, **Sair mantendo abertas**, **Cancelar**. Somente a primeira preenche o fim e marca como concluídas. Não concluir silenciosamente ao fechar a janela.

A consulta não pode se limitar ao dia exibido. É preciso definir quais tarefas planejadas futuras entram no aviso e representar corretamente encerramento após a meia-noite: o modelo atual guarda apenas um dia e dois horários no mesmo dia. Não fabricar um fim anterior ao início nem registrar tarefas futuras como realizadas. Essa fatia terá especificação e testes próprios antes da implementação.

Edição já existe, mas seu fluxo de salvamento será ajustado nesta primeira fatia. Exclusão com confirmação e recuperação deve ser especificada depois; não reutilizar exclusão física silenciosa nem expandir a lixeira dos Cadernos sem testes.

## Comando executado

No espelho Linux, SDK configurado:

```bash
dotnet test tests/Trama.IntegrationTests -c Release --no-restore --filter FullyQualifiedName~AgendaOpenEndedTaskTests --logger trx --results-directory artifacts/agenda-open-ended-red --verbosity quiet
```

Nenhum arquivo de implementação, banco pessoal, dependência, migração, manual ou comando Git foi alterado. Revisão dos testes e da mudança na expectativa antiga pendente, conforme CODEX seção 9.
