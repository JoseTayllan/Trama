# 67 — Último encerramento real persistido no SQLite

Data: 04/11/2026. Implementado para revisão; serviço e diálogo de encerramento ainda pendentes.

## Dados preservados

A oitava migração `20261104234835_AgendaTaskLastCompletion`, gerada pelo EF, adiciona somente `LastCompletedAt` opcional, como TEXT em AgendaTasks. O modelo usa DateTimeOffset? para preservar instante e offset. As sete migrações anteriores permanecem intactas. Registros antigos recebem null; não se inventa uma hora para tarefas já concluídas.

AgendaTaskSnapshot continua descrevendo o planejamento. A leitura separada por `IAgendaTaskCompletionReader.GetLastCompletedAtAsync` retorna o último encerramento ou null. Editar/reabrir preserva o registro; concluir novamente o atualiza. Não existe histórico completo de todos os encerramentos, início real ou duração trabalhada.

## Transação e revalidação

`SqliteAgendaTaskCompletionRepository` recebe um contexto pertencente ao chamador. Rejeita seleção vazia, repetida ou identidade vazia antes de abrir transação. Dentro dela, relê os registros sem tracking, valida snapshots e exige tarefas abertas com data planejada até o dia local do encerramento. Se faltar ou mudar a elegibilidade de qualquer tarefa, nenhuma é concluída.

As atualizações usam ExecuteUpdateAsync somente em Status e LastCompletedAt, com verificação do estado/data lidos. Isso preserva o planejamento e evita chamar SaveChanges sobre outras alterações pendentes no contexto. O adaptador usa transação própria; o serviço futuro deverá fornecer contexto novo sem outra transação ativa. O contexto não é descartado pelo repositório.

Só depois de todas as atualizações ocorre o commit. Falha ou cancelamento anterior ao commit provoca rollback. Erros conhecidos de SQLite/materialização tornam-se AgendaPersistenceException; cancelamento continua propagando. O trigger de teste provoca falha depois de uma primeira atualização, conferindo que estado e instante de todo o lote voltam ao original.

## Backup e legados

O formato do pacote continua 1. A validação existente materializa todos os registros da Agenda; com a nova propriedade tipada, um instante malformado falha na leitura e impede publicar/restaurar o backup, inclusive fora do dia exibido. Não foi necessário duplicar validação ou alterar o formato do manifesto.

A restauração migra a cópia preparada, preservando o banco de origem. Backups anteriores recuperam planejamento e null no encerramento real; backups novos preservam também o offset. O Down da migração remove a coluna e perde seus dados: nenhuma reversão foi aplicada e esse caminho exige decisão explícita ao operar dados reais.

## Evidências

Os 22 casos aprovados passaram sem alteração, incluindo migração de sete para oito versões, revalidação, rollback por trigger, reabertura, nova conclusão, cancelamento e backups adulterados. Depois, a suíte completa passou com **679 testes: 117 unitários e 562 integrações**, zero falhas e zero ignorados. Build Release sem avisos/erros e formatação aprovados no WSL.

A primeira passagem pela formatação encontrou BOM no arquivo novo gerado pelo EF; ele foi normalizado para UTF-8 sem BOM e namespace de arquivo conforme a convenção. A suíte completa só foi executada depois dessa correção. Nenhuma migração antiga ou teste foi alterado.

```bash
dotnet ef migrations add AgendaTaskLastCompletion --project /home/barberdev/trama-codex/src/Trama.Infrastructure --output-dir Persistence/Migrations -- --database /tmp/trama-completion-design.db
bash tools/run-wsl.sh test
```

O primeiro comando registra como a migração desta entrega foi gerada; não repeti-lo para aplicar atualizações. O caminho de design era isolado. Bancos usados nas verificações foram fictícios; o perfil pessoal não foi acessado.

## Próxima integração

Ainda falta um serviço com contexto por operação, consulta das candidatas e registro na composição. Depois, conectar seleção e as três escolhas de saída, coordenando Agenda e Cadernos antes de concluir tarefas. O fechamento atual do aplicativo não registra encerramentos automaticamente. Esta entrega não alterou a tela nem publicou executável novo.

Para estudar: por que ExecuteUpdate evita gravar alterações alheias? Por que validar a seleção antes da transação não substitui reler os registros? Por que o último encerramento não equivale a um histórico de trabalho?

[Especificação aprovada](66-encerramento-sqlite-especificacao.md) · [Relatório](../reviews/074-agenda-completion-storage-green.md).
