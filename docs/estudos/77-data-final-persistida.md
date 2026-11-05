# 77 — Data final planejada persistida

Data: 05/11/2026. Implementação após aprovação dos 24 casos e da décima migração; diff para revisão.

A migração 20261105224911_AgendaTaskPlannedEndDate adiciona somente PlannedEndDate opcional como TEXT/DateOnly?. As nove anteriores permanecem intactas. Registros antigos recebem null, conservando o comportamento de mesmo dia, sem inferir quando a tarefa deveria terminar.

AgendaTaskRecord e o mapeamento do contexto recebem a propriedade. SqliteAgendaTaskRepository copia o valor ao salvar e o recompõe antes de validar a leitura. Isso evita perder a data ao reabrir um intervalo como 22h–02h do dia seguinte. Salvar null limpa uma data explícita anterior.

Conclusão, reabertura e lixeira usam o leitor compartilhado e preservam a data planejada separada de LastCompletedAt. A conclusão revalida os registros na transação; um registro inválido impede o lote. Não foi necessário duplicar mapeamento nos adaptadores de conclusão e lixeira. Consulta por período continua selecionando pelo dia inicial; interseção de intervalos da semana será outra especificação.

Backup continua no formato 1 e materializa todas as tarefas, inclusive excluídas. A materialização tipada rejeita datas malformadas; a validação compartilhada rejeita fim anterior ao início. Restauração migra e valida a cópia, sem alterar o arquivo de origem; backups legados mantêm a data final null.

## Evidências

24 casos aprovados passaram intactos. Suíte completa: **811 aprovados, 131 unitários e 680 integrações**, sem falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Testes anteriores e nove migrações históricas preservados. Perfis fictícios; nenhum banco pessoal usado.

Casos conferidos: intervalo entre dias e sem horários; edição/limpeza; consulta; conclusão e reabertura; lixeira; backup ativo/excluído; migração e legado; rejeição de corrupção em leitura/backup/restauração; cancelamento, falha com rollback e perfis isolados.

```bash
dotnet ef migrations add AgendaTaskPlannedEndDate --project /home/barberdev/trama-codex/src/Trama.Infrastructure --output-dir Persistence/Migrations -- --database /tmp/trama-planned-end-design.db
dotnet build Trama.slnx -c Release --no-restore
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test Trama.slnx -c Release --no-build
```

O primeiro comando registra a geração já realizada; não executá-lo novamente para atualizar um perfil. A migração gerada foi normalizada para namespace de arquivo e UTF-8 sem BOM. Nenhum downgrade foi aplicado; revertê-la remove as datas finais explícitas.

## Próxima etapa e limites

Ainda não há campo de término planejado no formulário nem escolha do instante real no diálogo. A interface atual mantém seus padrões. Próxima fatia: testes dos campos compactos e da preservação das duas datas ao editar/salvar/navegar. Confirmação/tela da lixeira e visão semanal continuam pendentes. Esta entrega não publica executável nem demonstra aprovação visual.

Sem Git, dependências, perfil pessoal ou manual/PDF alterados. Para estudar: por que restaurar migra a cópia? Como um leitor compartilhado protege também a lixeira e o backup? Por que nullable permite migrar registros antigos sem inventar planejamento?

[Especificação](76-data-final-persistencia-especificacao.md) · [Relatório](../reviews/084-agenda-end-date-storage-green.md).
