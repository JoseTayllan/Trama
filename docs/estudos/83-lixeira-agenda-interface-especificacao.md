> Histórico RED: casos aprovados e implementados, com 887 testes aprovados. Ver [entrega para revisão](../reviews/091-agenda-trash-ui-green.md). Aceite visual pendente.

# 83 — Tela da lixeira da Agenda: especificação

Data: 06/11/2026. Testes em revisão; fluxo e armazenamento já existem, controles ainda ausentes.

O responsável confirmou que a correção do CI deu certo. Registrar esse aceite sem atribuir ao agente consulta de execução remota ou contagem de testes do GitHub. A última validação local da correção teve 865 aprovados no WSL; o bloqueio local da DLL Windows continua sendo um problema distinto.

## Fluxo visual proposto

AgendaDeleteTaskButton exclui a tarefa aberta, com ação indisponível para rascunho novo. AgendaTrashButton abre Lixeira da Agenda. A confirmação identifica a tarefa, informa que poderá ser restaurada e tem Cancelar como padrão e Escape. Fechar pelo X cancela. Confirmar usa o fluxo já aprovado; quando o editor tem alterações, reutiliza o diálogo Salvar/Descartar/Cancelar. Um segundo clique não abre outra confirmação.

A lixeira lista tarefas de todos os dias, preservando planejamento, estado, categoria, exclusão e último encerramento. Itens usam AgendaTaskTrashEntry e começam sem seleção; Restaurar só fica ativo após selecionar. Restauração atualiza lista e mantém dia selecionado e rascunho independente. Fechar a tela sem restaurar não altera os dados. Não há exclusão permanente.

Erros de carregamento mostram feedback e ação Atualizar, sem apresentar falha como lixeira vazia. Erro ao restaurar mantém item/seleção e permite tentar novamente. Durante carga ou restauração, desabilitar ações conflitantes e bloquear fechamento pelo X; o carregamento deve ser visível. Confirmação em 480x320 e lixeira em 520x420 devem manter ações acessíveis, inclusive com título longo. Estilos reutilizam a paleta; aceite visual ocorrerá depois da implementação e das capturas reais.

## Especificação executável

AgendaTrashUiTests contém **22 casos: 21 falhas esperadas e uma guarda passando**. Falhas são ausência dos botões e diálogos; compilação e runner funcionam. A guarda confirma que o construtor legado sem serviço de lixeira não expõe ações ativas e ainda salva tarefas.

Outros casos cobrem cancelamento/X, três estados da tarefa, três decisões de rascunho pelo diálogo nativo, exclusão externa antes do clique, duplicação de confirmação, metadados entre dias, restauração selecionada com texto parcial preservado, erros e tentativa seguinte, fechamento durante operações, fechamento sem mutação e janelas compactas.

Build Release sem avisos/erros e formatação passaram. Base anterior reexecutada no WSL: **865 aprovados, 131 unitários e 734 integrações**, zero falhas/ignorados. Arquivos anteriores de src/tests intactos por hashes. Não houve implementação, migração ou alteração de testes antigos. Uma chamada inicial de Assert.Single foi ajustada ao overload com predicado exigido pelo analisador antes da execução válida; erro de compilação não foi contado como RED.

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --no-build --filter FullyQualifiedName~AgendaTrashUiTests
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaTrashUiTests'
```

Após aprovação: implementar controles e ViewModel de seleção/listagem, conectar os diálogos ao fluxo existente, rodar casos intactos e suíte completa, conferir capturas ampla/compacta com perfil fictício e entregar diff. Nenhuma publicação, Git, perfil pessoal ou manual/PDF foi alterado nesta fatia. A semana visual continua sendo a etapa posterior à lixeira.

Para estudar: por que um erro de consulta não deve aparecer como lista vazia? Como uma janela modal protege seleção e rascunhos? Como impedir fechamento ou dupla execução sem deixar a janela presa depois de uma falha?

[Relatório](../reviews/090-agenda-trash-ui-red.md) · [Fluxo implementado](81-lixeira-agenda-fluxo-implementado.md).
