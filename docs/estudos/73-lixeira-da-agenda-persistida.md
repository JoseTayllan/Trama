# 73 — Lixeira da Agenda persistida

Data: 05/11/2026. Backend implementado para revisão; confirmação e tela da lixeira ainda pendentes.

A nona migração 20261105220851_AgendaTaskTrash adiciona somente DeletedAt opcional como TEXT/DateTimeOffset?. As oito anteriores permanecem intactas. Exclusão lógica mantém o registro físico e preserva conteúdo, planejamento, estado e LastCompletedAt; restaurar limpa só a marcação. Não existe exclusão permanente nesta fatia.

SqliteAgendaTrashActions abre banco existente em ReadWrite, Pooling=false e contexto por operação. Mover/restaurar relê a tarefa na transação e altera apenas DeletedAt. Falha reverte a operação; repetir exclusão não substitui o instante original. IDs inválidos, ausência de tarefa e estados incompatíveis são reportados sem inventar dados. Listagem da lixeira valida snapshots e ordena por exclusão recente, título ignorando caixa e ID.

Leituras normais, período e metadados de conclusão filtram explicitamente DeletedAt null. O serviço de candidatas reutiliza esse leitor. A conclusão transacional também exige tarefa ativa; seleção antiga contendo uma excluída falha sem concluir parcialmente as outras.

Salvar snapshot confere a visibilidade persistida sem tracking dentro de transação, mesmo que o contexto tenha versão antiga rastreada. Se a tarefa estiver na lixeira, retorna falha, preservando o conteúdo excluído. Quando não há transação do chamador, o repositório cria uma para proteger a leitura e a escrita; não se cria filtro global que esconda dados da validação de backups.

Backup permanece no formato 1 e valida todos os registros, inclusive escondidos e fora do dia exibido. Materialização tipada rejeita DeletedAt malformado; validação compartilhada rejeita conteúdo inválido. Restauração migra a cópia, preserva a origem e mantém lixeira/último encerramento. Backups anteriores à nona migração recuperam tarefas ativas. Reverter a migração remove a marcação e faz tarefas reaparecer; nenhum downgrade foi aplicado.

## Verificação

Os 31 casos aprovados passaram intactos. Os dois ajustes antigos de contagem foram aplicados exatamente como aprovados: pelo menos oito migrações em vez de exatamente oito, mantendo verificações de conteúdo e prefixo histórico. O teste novo exige pelo menos nove e preservação das oito anteriores.

Suíte completa: **769 aprovados, 117 unitários e 652 integrações**, zero falhas ou ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Uma rodada focada e uma completa, em perfis fictícios. Migração gerada pelo EF e normalizada para UTF-8 sem BOM/namespace de arquivo antes das verificações.

```bash
dotnet ef migrations add AgendaTaskTrash --project /home/barberdev/trama-codex/src/Trama.Infrastructure --output-dir Persistence/Migrations -- --database /tmp/trama-trash-design.db
bash tools/run-wsl.sh test
```

O primeiro comando documenta a geração já realizada; não é comando para aplicar atualizações ao perfil. Nenhum banco pessoal, Git, manual/PDF ou publicação foi alterado.

## Datas de término: adendo confirmado

O responsável pediu as duas opções: escolher término planejado em outro dia e informar data/hora reais ao concluir. A análise confirmou que hoje o intervalo planejado compara só horas no mesmo dia, e o aviso ao sair usa o instante da confirmação. LastCompletedAt já suporta outros dias, mas não há controle para informar um instante diferente. Essa ampliação terá testes próprios; não confundir os campos planejados com o encerramento real.

Prioridade seguinte: especificar as regras das duas datas e depois persistência/controles. A confirmação e tela da lixeira continuam no plano, assim como a visão semanal. O backend da lixeira não foi registrado na composição nem exposto na interface nesta fatia.

Para estudar: por que a exclusão lógica exige proteger também Save e Complete? Por que um filtro global do EF pode esconder dados de um validador de backup? Qual é o risco de confundir o estado restaurado com uma nova tarefa?

[Especificação](72-lixeira-da-agenda-especificacao.md) · [Relatório](../reviews/080-agenda-trash-storage-green.md).
