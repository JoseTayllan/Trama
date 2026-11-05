# Lixeira da Agenda — persistência implementada

Data: 05/11/2026. Para revisão do diff; exclusão/restauração ainda não disponíveis na interface.

Nona migração adiciona DeletedAt opcional sem alterar oito anteriores. Ações com contexto por operação, transação, identidade estável e estado/planejamento/último encerramento preservados. Filtros explícitos nas leituras normais e na conclusão; gravação atrasada de tarefa excluída rejeitada. Backup 1 valida também conteúdo escondido e metadados malformados, restaurando legados na cópia.

31 casos aprovados intactos. Dois ajustes de contagem antiga aplicados exatamente como propostos e aprovados, sem mudar as demais asserções. Suíte completa: **769 aprovados (117 unitários e 652 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram. Verificados estados distintos, timestamps/offset, linhas físicas preservadas, seleção antiga, gravação atrasada, perfis isolados, triggers de falha, migração e backups com hashes coerentes e dados inválidos.

Limites: sem confirmação/tela/registro na composição; não há exclusão permanente ou histórico de todas as exclusões. Reverter a migração remove a marcação e faz tarefas reaparecer; nenhum downgrade aplicado. Concorrência geral de edições do mesmo ID não ganhou versionamento otimista nesta fatia. Sem nova publicação/execução Windows.

Adendo confirmado: data de término planejada em outro dia e data/hora reais informadas pelo usuário. Diagnóstico concluído; novas regras e campos ainda não implementados. Preparar testes próprios antes de mudar a validação do mesmo dia ou o horário automático já aprovado.

Sem dependências/formatos, perfil pessoal, Git, logs novos, chamadas externas ou segredo literal alterados. README e manual/PDF preservados. Inspeção limitada ao diff. Esforço moderado, sem subagentes: uma rodada focada e uma completa. Reutilizar os fixtures e manter os oito testes históricos de migração compatíveis com novas versões reduz retrabalho. Tokens/custo exato não foram medidos. Revisão humana do diff pendente.

[Estudo](../estudos/73-lixeira-da-agenda-persistida.md) · [Histórico RED](079-agenda-trash-storage-red.md).
