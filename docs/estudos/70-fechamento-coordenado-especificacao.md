# 70 — Seleção de tarefas e fechamento coordenado

**Atualização de 04/11/2026:** os 35 casos foram aprovados e implementados, preservados. Suíte completa com 738 aprovados; build/formatação passaram. [Entrega GREEN](71-fechamento-coordenado-implementado.md). O texto abaixo preserva o histórico RED.

**Resultado final:** 35 casos descobertos, 33 falhas esperadas e duas guardas passando, zero ignorados. Vinte e seis falhas vêm das operações ausentes, quatro de controles ainda inexistentes e três do fechamento nativo ainda ignorar o coordenador. As asserções posteriores às entradas vazias não foram alcançadas; não declarar segurança/diálogo implementados. Separadamente, os 703 casos anteriores passaram (117 unitários e 586 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram. A suíte completa atual contém RED novo e não está GREEN.

Data: 04/11/2026. Especificação RED para revisão; o fluxo atual de fechamento não foi alterado.

## Decisões propostas

Consultar candidatas depois de resolver os rascunhos, incluindo uma tarefa da Agenda salva nessa confirmação. Sem candidatas, permitir sair sem um aviso vazio. Com candidatas, mostrar seleção manual inicialmente desmarcada e três ações: Encerrar selecionadas e sair, Sair mantendo abertas e Cancelar. A primeira exige pelo menos uma seleção válida; as outras não enviam IDs para concluir.

Cancelar deve ser o padrão seguro de teclado e o resultado de fechar o aviso pelo X. Selecionar todas e limpar seleção não alteram tarefas. Identificar cada linha pelos dados da tarefa e cada decisão pelos IDs, nunca pelo título. Copiar os IDs da seleção ao confirmar.

## Ordem e proteção dos rascunhos

1. Bloquear reentrada e pausar autosave.
2. Resolver o rascunho da Agenda e depois o dos Cadernos, sem destruir nenhum editor por uma aprovação provisória.
3. Consultar candidatas usando o dia local e apresentar o aviso.
4. Validar a decisão; ao concluir, capturar o instante real depois da confirmação e chamar o serviço aprovado.
5. Somente depois permitir o fechamento efetivo, sem novas confirmações de descarte posteriores à gravação das conclusões.

Qualquer cancelamento, falha de consulta/gravação ou seleção desatualizada mantém a janela e os editores utilizáveis. Descartar em uma confirmação intermediária não apaga o rascunho antes de toda a saída ser autorizada. Se a saída for cancelada, retomar autosave; se aprovada, manter a pausa até fechar/dispor o editor para não gravar o conteúdo que foi descartado.

A biblioteca atual descarta o editor em RequestCloseAsync. Será necessário separar preparação e descarte final no novo fluxo, preservando a semântica do método antigo para consumidores existentes. ApplicationCloseCoordinator terá ciclo de vida descartável para controlar essa preparação. A integração deve cuidar da liberação tanto em cancelamento quanto no fechamento real.

## Contratos e casos

Contratos compiláveis novos: AgendaCompletionDecision/Choice, IAgendaCompletionPrompt, ViewModels do diálogo/coordenador, propriedade Closing no host e janela/prompt vazios. Não estão ligados à inicialização nem ao fechamento. Operações de decisão, coordenação e renderização continuam ausentes; somente guardas estruturais e propriedades mínimas foram criadas para compilar os testes.

Arquivos: AgendaCloseDialogTests.cs e AgendaCloseWorkflowTests.cs. Cobrem seleção/contagem/cópia, ações nativas, padrão Cancelar, X, janela compacta, ausência de candidatas, preservação de tarefas, IDs inválidos, falhas, dois rascunhos, reentrada, tarefa salva antes de sair, relógio local após confirmação e virada de dia, alteração externa, autosave suspenso/retomado e janela real usando o coordenador inclusive pelo Início.

O instante de teste é 22:30 em UTC−3, já no dia seguinte em UTC, para distinguir dia local. O caso de confirmação avança o relógio por seis horas e exige capturar o instante novo. O teste de autosave aguarda a pausa de dois segundos com dispatcher Avalonia ativo; o rascunho não pode ser gravado enquanto o aviso aguarda decisão.

## Limites da especificação

Persistência, serviço e migração aprovados são reutilizados. Nada deve ser concluído antes de uma confirmação posterior que ainda possa cancelar a saída. A especificação trata o fechamento normal do Trama; não promete concluir tarefas durante queda de energia, encerramento forçado do processo ou falha do sistema.

Nenhum teste antigo, migração, banco pessoal, Git, README, manual/PDF ou pacote publicado foi alterado. Aceite estético do novo diálogo ocorrerá depois de implementar e inspecionar capturas, não pelo resultado dos testes.

```bash
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter 'FullyQualifiedName~AgendaCloseWorkflowTests|FullyQualifiedName~AgendaCloseDialogTests' --logger trx --results-directory artifacts/agenda-close-workflow-approval --verbosity quiet
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaCloseWorkflowTests&FullyQualifiedName!~AgendaCloseDialogTests' --logger trx --results-directory artifacts/agenda-close-workflow-baseline --verbosity quiet
```

Revisar os testes antes de implementar, conforme CODEX seção 9. Para estudar: por que autorização para fechar não equivale a já ter fechado? Por que liberar uma pausa de autosave cedo demais pode violar um descarte? Por que o horário de conclusão deve ser obtido depois da decisão?

[Relatório](../reviews/077-agenda-close-workflow-red.md).
