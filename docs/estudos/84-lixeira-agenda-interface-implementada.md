# 84 — Lixeira da Agenda na interface

Data: 06/11/2026. Implementação após aprovação dos 22 casos; diff e visual para revisão.

Aceite posterior em 06/11/2026: o responsável confirmou “Teste realizado, tudo funcionando como o esperado” para a lixeira e acesso à exclusão. Registrar validação no uso; ambiente não informado e revisão formal do diff separada. A continuidade passa à visão semanal. Nenhum teste foi executado apenas para registrar esse aceite.

A Agenda recebe Lixeira da Agenda no cabeçalho e Excluir tarefa no cabeçalho da lista Tarefas do dia. A exclusão exige tarefa persistida e serviço disponível. CanDeleteTask acompanha edição e CanEdit; o construtor legado sem lixeira mantém as ações ocultas. Os controles chamam o fluxo já aprovado, sem acesso direto ao banco ou nova migração.

AgendaTrashConfirmationDialog mostra título e possibilidade de recuperação. Cancelar é padrão/IsCancel; X retorna ausência de confirmação. Conteúdo longo fica em rolagem e ações permanecem no rodapé. Confirmar chama MoveToTrashAsync; rascunho afetado usa Salvar/Descartar/Cancelar existente. Uma operação ocupada bloqueia reentrada e preserva as proteções do editor.

AgendaTrashViewModel carrega AgendaTaskTrashEntry de todos os dias, começa sem seleção e expõe restauração somente para um item da lista atual. A tela diferencia vazio, erro e carregamento. Falhas de carga mostram Atualizar; falha de restauração mantém seleção e permite tentar novamente. Sucesso da mutação seguido de falha de recarga é comunicado separadamente, com seleção limpa e nova restauração bloqueada até atualizar.

AgendaTrashDialog bloqueia fechamento pelo X/Escape durante carga e restauração. Enquanto ocupado, comandos e lista não recebem alterações. RestoreAsync também verifica CanRestore ao entrar, protegendo até chamada direta de comando durante uma operação. Ao concluir, os comandos voltam a responder.

Cada item mostra título, categoria, estado, planejamento, exclusão e último encerramento quando existente. O converter reutiliza a formatação de horário e inclui o dia em tarefas do mesmo dia; intervalos entre dias mostram ambas as datas. A seleção usa TramaAccentSoftBrush e TramaAccentBrush por DynamicResource, acompanhando a paleta. Mensagem contextual identifica a ação sobre o item selecionado.

## Verificação

22 casos aprovados intactos. Suíte completa: **887 aprovados (131 unitários e 756 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram no WSL. Testes anteriores, persistência e migrações intactos. Cobertura inclui Cancelar/X, três estados, três decisões do rascunho, exclusão externa, confirmação duplicada, metadados, restauração escolhida, falhas/nova tentativa, bloqueio durante operação e janela compacta.

QA SQLite/Skia externo, com perfil fictício: confirmar/cancelar, seleção, restauração de estado/datas/último encerramento, manutenção de rascunho independente, estado vazio, descarte explícito e exclusão recuperável. Capturas conferidas da agenda, confirmação em 540x340/480x320 e lixeira em 720x560/520x420. A inspeção inicial motivou trocar o azul padrão pela paleta e contextualizar a mensagem da seleção; suíte e capturas repetidas no estado final. Títulos longos são cobertos no teste compacto.

```bash
dotnet build Trama.slnx -c Release --no-restore
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test Trama.slnx -c Release --no-build
```

## Limites e continuidade

Aceite visual/funcional do responsável pendente. Não houve publicação ou execução nativa de um novo exe Windows; a correção anterior do CI foi validada pelo responsável, mas esta entrega precisa de nova execução remota após seu commit/push. Sem migração, dependência, perfil pessoal, Git ou manual/PDF alterados pelo agente.

Não existe exclusão permanente. Consulta diária continua pelo dia inicial; visão semanal será a próxima etapa após validar a lixeira. Restaurar não troca a data selecionada nem abre automaticamente a tarefa. A lixeira dos Cadernos permanece separada.

Para estudar: por que vazio e erro precisam de estados diferentes? Por que validar a seleção novamente no comando? Como isolar conteúdo rolável de ações fixas em janelas compactas? Como recursos dinâmicos preservam a personalização da paleta?

[Especificação](83-lixeira-agenda-interface-especificacao.md) · [Relatório](../reviews/091-agenda-trash-ui-green.md).

## Ajuste de acesso após retorno do responsável — 06/11

O responsável informou que não encontrou como excluir, vendo apenas a entrada da lixeira. O botão estava discreto no editor. Foi movido para Tarefas do dia, com borda, fundo e instrução: clicar em uma tarefa salva e então usar Excluir tarefa. O mesmo nome, handler, habilitação e confirmação foram preservados. Nenhuma regra ou teste foi alterado.

Verificação do estado final: build/formatação e 887 testes passaram no WSL; QA SQLite/Skia confirmou localização em 1100x760 e 760x650, cancelamento/exclusão e restauração. Capturas conferidas. Este ajuste é somente de apresentação; nenhuma migração ou publicação. Reabrir o projeto atualizado para ver a posição nova; executáveis já publicados precisam de nova publicação para incorporar o ajuste. Aceite no uso continua pendente.
