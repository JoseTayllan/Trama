# 62 — Agenda: tarefas sem fim e cadastro em sequência

Data: 04/11/2026. Implementado para revisão do diff e validação no uso.

## O que mudou

É possível salvar uma tarefa com início e sem fim, sem mudar seu estado. A lista apresenta, por exemplo, `19:27 · sem fim`. Tarefas sem horário continuam aceitas; fim sem início, fim igual ao início e intervalos invertidos continuam inválidos. A regra compartilhada é usada pela gravação, leitura e validação de backup.

O botão Salvar tarefa prepara um formulário limpo depois de criar ou editar com sucesso. O novo formulário usa o dia selecionado, novos dados de identidade e estado Pendente; a tarefa gravada mantém seu ID e estado na lista. Salvar não significa concluir.

## Como funciona

`AgendaTaskValidation` aceita ausência de fim e exige início quando há fim. `AgendaPresentationConverter` distingue intervalos abertos de tarefas sem horário. `AgendaViewModel.SaveCommand` chama `SaveAndPrepareNextAsync`: grava, verifica se há alterações mais recentes ou erro de atualização da lista e só então prepara o formulário seguinte.

`SaveAsync` e a gravação usada nas confirmações de navegação/fechamento mantêm o comportamento anterior de preservar a edição. Limpar é uma ação do botão, não um efeito obrigatório do serviço. Falha de escrita, cancelamento ou edição posterior ao snapshot mantém o rascunho. Se a escrita for confirmada e a releitura da lista falhar, a identidade é preservada para evitar duplicação; use Atualizar antes de preparar outro cadastro.

## Verificações e aprovações

Os nove casos aprovados em AgendaOpenEndedTaskTests.cs foram preservados byte a byte. Foram autorizados também dois ajustes antigos: remover somente a entrada `[InlineData(18, null)]` da rejeição de intervalos e mudar a espera do teste nativo para formulário limpo com uma tarefa na lista, mantendo prazo e verificações dos dados.

Primeira suíte: 639 passaram e o teste nativo falhou pela expectativa antiga de manter a edição aberta. Depois da aprovação e ajuste dessa espera, a suíte completa passou: **640 testes, 100 unitários e 540 integrações**, sem falhas ou ignorados. Build Release sem avisos/erros e formatação passaram no WSL. Gravação/reabertura e backup/restauração usam SQLite em perfis fictícios; o teste nativo existente verifica o botão ligado à interface.

Pacote Windows separado gerado para revisão com um perfil de teste próprio. A execução interativa desse novo pacote ainda depende da validação do responsável. Não houve nova migração, dependência, alteração de formato ou acesso ao perfil pessoal.

## Próximas fatias

Ainda não há aviso de tarefas abertas ao encerrar nem exclusão de tarefas. O responsável escolheu considerar Pendentes e Em andamento e oferecer: Encerrar tarefas e sair; Sair mantendo abertas; Cancelar. Apenas a primeira opção preenche fim e conclui. Precisaremos especificar tarefas futuras, outros dias, virada de meia-noite e falhas parciais antes de implementar. O modelo atual guarda um único dia e horários no mesmo dia; não registrar fim anterior ao início nem transformar planejamento futuro em trabalho realizado.

## Para estudar

1. Por que o serviço de gravação não deve limpar um formulário?
2. Por que a confirmação de escrita e a atualização da lista são resultados diferentes?
3. Como preservar uma edição feita enquanto uma operação assíncrona está pendente?

[Especificação](61-agenda-tarefas-abertas-especificacao.md) · [Revisão](../reviews/069-agenda-open-ended-green.md).
