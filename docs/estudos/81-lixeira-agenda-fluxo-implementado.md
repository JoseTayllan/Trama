# 81 — Fluxo da lixeira da Agenda integrado

Data: 05/11/2026. Após aprovação dos 21 casos; diff para revisão. Confirmação/tela nativa ainda pendentes.

DesktopComposition registra IAgendaTrashActions no mesmo perfil, e a composição real do App fornece esse serviço e TimeProvider.System à Agenda. O construtor anterior de dois argumentos continua funcional, com lixeira indisponível; a nova sobrecarga recebe serviço e relógio. AgendaViewModel.Trash concentra exclusão, listagem e restauração, reutilizando as regras e o armazenamento já existentes.

MoveToTrashAsync recebe ID e callback de confirmação com o snapshot persistido. Durante a operação, IsBusy bloqueia reentrada, navegação e fechamento. Cancelar a confirmação não aciona resolução de rascunho. Se confirmado, relê disponibilidade antes de resolver alterações da tarefa em edição e novamente antes de excluir. Rascunhos de outra tarefa permanecem intactos.

Para a tarefa em edição, Save/Discard/Cancel reutiliza ResolveChangesAsync. Falha de Save impede exclusão; Cancel mantém tudo. Discard só limpa o editor após a exclusão persistir. Falha da exclusão mantém o rascunho. O relógio injetado define DeletedAt. Não existe exclusão permanente nesta fatia.

RestoreTrashEntryAsync mantém dia selecionado e rascunho, restaurando planejamento, estado e último encerramento. ListTrashAsync abrange todos os dias. RefreshAfterTrashMutationAsync separa sucesso da mutação da atualização da lista: falha de consulta marca HasListError e orienta Atualizar, sem negar o resultado persistido ou reabrir tarefa excluída.

## Verificação

21 casos aprovados passaram intactos. Suíte completa: **865 aprovados (131 unitários e 734 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Arquivos anteriores de testes e migrações intactos. Cobertura: estados distintos, confirmação, Save/Discard/Cancel, falha de gravação, desaparecimento durante confirmação, reentrada, recuperação, perfis e metadados preservados.

```bash
dotnet build Trama.slnx -c Release --no-restore
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test Trama.slnx -c Release --no-build
```

Sem novo controle, diálogo, migração, dependência ou publicação Windows. O fluxo está conectado à composição, mas ainda não está disponível por botões da interface. Próxima fatia: testes próprios de confirmação e tela da lixeira, com Cancelar como padrão, restauração e janela compacta. A visão semanal vem depois.

## Commit desta entrega

O responsável forneceu branch feat/agenda-semanal e status pendente desde o Início. Com esta fatia implementada e a suíte no GREEN, os arquivos atuais podem compor um checkpoint conjunto de Início, Agenda diária e fluxo da lixeira, após revisão do índice. O plano anterior de retirar a especificação RED do índice foi superado; não usar checkpoint-validado.patch da fatia anterior para este commit.

O agente não executou Git. O responsável executa git add, revisa git diff --cached e faz commit/push na branch atual. Merge para main permanece após validar o módulo completo. Sem dados pessoais, banco de uso, manual/PDF ou credenciais adicionados.

Para estudar: por que revalidar após a confirmação? Por que descarte não deve limpar conteúdo antes da mutação? Como comunicar sucesso persistido quando só a atualização da lista falhou?

[Especificação](80-lixeira-agenda-fluxo-especificacao.md) · [Relatório](../reviews/088-agenda-trash-workflow-green.md).
