> Histórico RED: casos aprovados e implementados; suíte com 865 aprovados. Ver [fluxo implementado](../reviews/088-agenda-trash-workflow-green.md). O patch de separação do RED é histórico e não deve ser usado no commit atual.

# 80 — Lixeira da Agenda: fluxo de confirmação e rascunhos

Data: 05/11/2026. Especificação em revisão; persistência existente, integração ainda não implementada.

Antes da tela, esta fatia define o fluxo entre AgendaViewModel e IAgendaTrashActions. A composição deve registrar o serviço no mesmo perfil e oferecer uma sobrecarga do ViewModel com ações de lixeira e TimeProvider, preservando o construtor anterior. Nenhuma migração nova é necessária.

## Regras propostas

MoveToTrashAsync recebe a identidade e uma confirmação assíncrona com o snapshot persistido. Confirmar deve preceder qualquer resolução do rascunho; cancelar não grava nem descarta conteúdo. Se a tarefa excluída estiver em edição e tiver alterações, respeitar Salvar/Descartar/Cancelar. Gravar falha ou cancelar impede excluir. Descarte só limpa o editor depois de a exclusão ter sido persistida; falha mantém o rascunho. Excluir outra tarefa não altera o rascunho atual.

Reler disponibilidade após a confirmação evita continuar com tarefa já excluída em outra ação. Durante o fluxo, bloquear reentrada, navegação e fechamento. Exceção da confirmação não pode esconder a tarefa ou deixar a interface ocupada. O relógio injetado define DeletedAt; conteúdo, planejamento e estado são preservados.

ListTrashAsync consulta todos os dias. RestoreTrashEntryAsync restaura estado/planejamento/último encerramento originais, atualiza a lista do dia selecionado sem trocar o dia e preserva rascunhos independentes. Falhas mantêm o item na lixeira. Se a exclusão gravar mas a atualização da lista falhar, informar lista desatualizada, sem fingir que nada foi excluído nem manter editor apontando para registro oculto.

## Evidências

AgendaTrashWorkflowTests contém 21 casos: 20 falhas esperadas e uma proteção passando. As falhas são o registro ausente de IAgendaTrashActions e a sobrecarga de integração ausente, exigidos por asserções; os testes compilam e não usam erro de infraestrutura como RED. A guarda preserva cadastro com o construtor antigo. Outros casos especificam estados distintos, confirmação, alterações não salvas, falhas, disponibilidade, reentrada, listagem e restauração.

Base reexecutada: **844 aprovados (131 unitários e 713 integrações)**. Build Release sem avisos/erros e formatação aprovados no WSL. Arquivos anteriores de src/tests intactos por hashes. Nenhum código de implementação, migração ou teste antigo alterado nesta fatia. Os diálogos e controles nativos terão seus próprios testes depois do fluxo; ainda não foram implementados.

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --no-build --filter FullyQualifiedName~AgendaTrashWorkflowTests
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaTrashWorkflowTests'
```

## Checkpoint de commits

O responsável informou branch feat/agenda-semanal e status com entregas desde Início ainda sem commit. O agente não executou Git. Artefato checkpoint-validado.patch descreve somente esta nova especificação (testes e documentos). Após preparar os arquivos no índice, aplicá-lo com git apply --cached --reverse retira a especificação RED apenas do índice, preservando os arquivos de trabalho e mantendo para commit o checkpoint anterior de 844 testes. Conferir --check antes de aplicar e revisar git diff --cached antes de commitar. Não executar o commit se qualquer comando de preparação falhar. Não fazer merge para main antes do aceite do módulo completo.

O patch não inclui mudanças do produto ou trabalho anterior; não apaga arquivos da pasta. O restante RED permanece para aprovação e implementação. Os comandos são fornecidos ao responsável, que continua operando Git e GitHub. Nenhum commit, push ou branch foi criado pelo agente.

Sem publicação, perfil pessoal, dependência, migração ou manual/PDF alterados. Após aprovação dos casos: integrar o fluxo, verificar a suíte e preparar testes próprios da confirmação/tela. Para estudar: por que confirmar antes de resolver o rascunho? Como distinguir mutação persistida de falha ao atualizar a lista? Por que restaurar não deve sobrescrever outro rascunho?

[Relatório](../reviews/087-agenda-trash-workflow-red.md).
