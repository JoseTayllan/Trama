# 79 — Campos compactos de datas e horários implementados

Data: 05/11/2026. Após aprovação dos 33 casos, das duas linhas antigas e do seletor adicional; diff para revisão.

Aceite posterior em 05/11: o responsável confirmou “validado”. Campos de datas e horários validados no uso; ambiente não informado. Revisão formal do diff continua separada. Nenhum código, teste ou executável foi alterado neste registro.

O formulário agora mostra dia inicial e dia final opcional, cada um acompanhado de uma entrada compacta HH:mm. Início/fim podem ser digitados sem abrir listas de horário. Datas continuam usando calendário. Sem dia final explícito, o intervalo permanece no mesmo dia; fim pode continuar em aberto. Há ações para limpar horários ou remover a data final.

AgendaTimeInput centraliza leitura estrita de HH:mm e apresentação. AgendaViewModel preserva StartTime/EndTime para os consumidores anteriores, sincronizando-os com StartTimeText/EndTimeText sem loops. Texto inválido não substitui o valor previamente interpretado e integra a captura do rascunho; salvar exige texto válido, sem tratar entrada parcial como campo vazio. A data final integra carga, captura e baseline, segue ao snapshot e chega ao SQLite já implementado.

Digitar 09: mantém esse texto e bloqueia a gravação. Apagar o campo remove a hora. Salvar pelo botão limpa o formulário somente após sucesso e sem alterações mais recentes/lista em erro. Cancelamento de navegação, falha de persistência e rascunhos continuam protegidos. O resumo de tarefas que atravessam dias mostra ambas as datas, sem alterar o filtro diário pelo início.

No aviso de saída, o padrão continua Agora. Marcar Informar data e hora revela calendário e entrada HH:mm. No modo manual, ambos são obrigatórios e valores incompletos desabilitam concluir; Cancelar/Manter abertas seguem disponíveis. CreateDecision compõe o instante preservando o offset da data selecionada. O coordenador mantém a validação contra futuro e dia inicial antes de gravar o lote. O instante real permanece separado do planejamento.

## Verificação e revisão

Os 33 casos aprovados passaram intactos. A troca de duas linhas em AgendaNavigationTests foi aplicada como aprovada. O helper de AgendaCloseDialogTests foi ajustado após aprovação para selecionar apenas checkboxes completion-task. O predicado foi passado ao overload de Assert.Single exigido pelo analisador xUnit2031; mesma seleção e exigência de exatamente uma tarefa, sem enfraquecer as verificações.

Suíte completa: **844 aprovados, 131 unitários e 713 integrações**, zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. A primeira rodada identificou largura mínima do calendário no diálogo compacto; corrigida sem mudar os testes. A suíte revelou a suposição antiga de um único checkbox no diálogo, corrigida após autorização.

QA SQLite/Skia externo, em perfil fictício: entradas reais, rejeição de texto parcial sem alterar o registro, intervalo entre dias, escolha manual no diálogo e gravação do instante real preservando planejamento. Capturas da agenda em 1100x760/760x650 e diálogo em 700x540/560x420 conferidas. Isso verifica renderização e interação automatizada, sem substituir seu aceite visual ou uso nativo no Windows.

```bash
dotnet build Trama.slnx -c Release --no-restore
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test Trama.slnx -c Release --no-build
```

Nenhuma migração, dependência ou formato novo. Sem perfil pessoal, Git, manual/PDF ou nova publicação Windows. Próximos passos: aceite visual/funcional, confirmação e tela da lixeira da Agenda e visão semanal. A consulta diária continua pelo dia inicial; tarefas de vários dias não são automaticamente duplicadas na lista dos outros dias.

Para estudar: como sincronizar texto parcial e horário tipado sem perder o rascunho? Por que a data final precisa integrar o baseline? Como um seletor de teste deve diferenciar tarefas de controles de configuração?

[Especificação](78-campos-de-datas-e-horarios-especificacao.md) · [Relatório](../reviews/086-agenda-date-controls-green.md).
