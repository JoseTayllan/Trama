# Encerramento real — SQLite em RED

**Atualização de 04/11/2026:** os 22 casos e o modelo foram aprovados e implementados, sem alterar os testes. Suíte final: 679 aprovados, com build/formatação. [Entrega GREEN](074-agenda-completion-storage-green.md). O texto abaixo preserva o histórico RED; integração com o fechamento ainda pendente.

Data: 04/11/2026. Aguardando revisão dos 22 casos e do modelo proposto.

Novos contratos mínimos: IAgendaTaskCompletionReader e SqliteAgendaTaskCompletionRepository com operações vazias compiláveis. Nenhuma implementação de gravação/leitura, migração, mudança no backup ou registro no aplicativo. Fechamento continua inalterado.

Proposta: oitava migração com LastCompletedAt opcional; preservar planejamento e offset. Editar/reabrir mantém o último encerramento; concluir novamente atualiza esse instante, sem histórico completo. Backup continua no formato 1, com migração na cópia e validação de todo o acervo.

22 casos descobertos: 21 falhas esperadas e uma guarda passando. Vinte falhas por NotImplementedException; uma demonstra a oitava migração ausente. Asserções posteriores ainda não alcançadas; não declarar transação/backup verificados. Build Release sem avisos/erros e formatação passaram. Base anterior executada separadamente: 657 aprovados, 117 unitários e 540 integrações, zero falhas/ignorados. Os 22 casos novos permanecem RED; não declarar a suíte completa GREEN.

Riscos a endereçar no GREEN: revalidação dentro da transação, rollback do estado e do instante, preservação de legados, leitura de dados malformados e rejeição de backups adulterados com hashes coerentes. O registro proposto guarda só o último encerramento; não serve como auditoria completa de atividades.

Nenhum teste antigo alterado, perfil pessoal acessado, Git executado ou pacote publicado. Sem dependências, logs novos ou segredos literais. O trigger de falha usa apenas um Guid gerado; nenhum dado do usuário compõe SQL. README, manual e PDF preservados.

Esforço moderado, sem novos subagentes: duas execuções focadas RED, sendo a segunda após preparar a inserção no esquema legado, e uma rodada da base anterior. Reaproveitar o fixture SQLite e o backup existente evita duplicação de infraestrutura. Tokens/custo não foram medidos. Revisão dos testes obrigatória antes da implementação.

[Estudo](../estudos/66-encerramento-sqlite-especificacao.md).
