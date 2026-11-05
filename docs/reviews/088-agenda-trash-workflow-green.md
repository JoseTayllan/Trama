# Agenda — fluxo da lixeira implementado

Data: 05/11/2026. Para revisão do diff; controles nativos ainda pendentes.

Após aprovação dos 21 casos, IAgendaTrashActions foi registrado na composição e fornecido à instância real do App. Nova sobrecarga do ViewModel preserva o construtor anterior. Partial de lixeira coordena confirmação, rascunho afetado, exclusão lógica, consulta e restauração, com relógio injetado e IsBusy.

21 casos aprovados intactos; suíte completa **865 aprovados (131 unitários, 734 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram no WSL. Testes anteriores e migrações intactos por comparação com a base. Perfis fictícios; sem nova execução/publicação Windows.

Revisão do código: confirmar antes de resolver rascunho; reler disponibilidade; não limpar descarte antes de persistir exclusão; não resolver rascunho independente; bloquear reentrada e fechar enquanto ocupado. Erros de refresh são tratados após a mutação, mantendo feedback de sucesso com aviso de lista desatualizada. Restauração não troca o dia nem reabre automaticamente a tarefa. Exceções não deixam IsBusy preso. Não há histórico adicional ou exclusão permanente.

Limites: falta especificar e implementar a confirmação/tela nativa; o usuário ainda não tem botões para esse fluxo. Revalidação não introduz versionamento otimista geral; o armazenamento mantém suas proteções existentes. Sem migração, dependência, perfil pessoal, Git ou manual/PDF alterados. Inspeção limitada não encontrou credencial literal ou logging novo.

Esforço moderado, sem novos subagentes: uma rodada focada e uma suíte completa após o RED aprovado. Reutilizar o fluxo de rascunhos e adaptar os diálogos existentes reduz retrabalho na próxima etapa. Tokens/custo exato não medidos. Revisão humana do diff pendente.

Commit orientado ao responsável, branch feat/agenda-semanal. Com o GREEN, usar o checkpoint normal do estado atual; o patch preparado para separar a antiga especificação RED tornou-se histórico e não deve ser aplicado neste commit. Nenhum Git foi executado pelo agente.

[Estudo](../estudos/81-lixeira-agenda-fluxo-implementado.md) · [RED](087-agenda-trash-workflow-red.md).
