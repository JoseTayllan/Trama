# 72 — Lixeira da Agenda: armazenamento e recuperação

**Atualização de 05/11/2026:** 31 casos e dois ajustes antigos aprovados e implementados; testes novos preservados. Suíte completa com 769 aprovados, build/formatação. [Entrega GREEN](73-lixeira-da-agenda-persistida.md). O texto abaixo preserva o histórico RED.

Data: 05/11/2026. Primeira fatia em RED para revisão; confirmação e interface virão depois da persistência.

## Modelo proposto

Uma nona migração adicionará DeletedAt opcional em AgendaTasks, sem remover linhas nem alterar o contrato de AgendaTaskSnapshot. Exclusão lógica preserva ID, título, descrição, categoria, estado, data/horários planejados e LastCompletedAt. Restaurar limpa apenas DeletedAt e devolve o estado anterior: tarefa concluída volta concluída, não vira pendente.

IAgendaTrashActions separa mover, listar e restaurar. AgendaTaskTrashEntry reúne o snapshot, instante de exclusão e último encerramento real. O instante é fornecido pelo chamador e conserva seu offset. A proposta não adiciona exclusão permanente nem histórico de todas as exclusões. A lixeira dos Cadernos permanece independente.

## Visibilidade e segurança

Tarefa excluída sai das listas diárias, da leitura normal por ID e das candidatas ao fechar. A leitura normal do último encerramento também retorna null enquanto excluída; a lixeira preserva e expõe esse dado. Restaurar tarefa Pendente/Em andamento permite que volte às candidatas conforme sua data; restaurar uma concluída não a torna candidata.

Seleção antiga do aviso que contenha uma tarefa já excluída deve falhar sem concluir parcialmente o lote. Gravação atrasada de um snapshot para tarefa já excluída deve ser rejeitada, preservando o conteúdo da lixeira. Não usar filtro global do EF que esconda conteúdo da validação de backups.

Repetir exclusão não pode trocar a data original; restaurar tarefa ativa ou inexistente deve ser rejeitado. Operações respeitam cancelamento antes do disco, perfis isolados e conexão com banco existente. O futuro serviço usará contexto por operação, sem inicializar banco quando o caminho estiver errado.

## Migração, testes antigos e backup

Preservar as oito migrações anteriores. O fixture legado usa SQL parametrizado no esquema oito, sem depender do futuro modelo com DeletedAt. A nova migração deixa tarefas anteriores ativas, preservando o último encerramento e notas dos Cadernos.

Dois testes antigos em AgendaCompletionStorageTests.cs exigem exatamente oito migrações. Propõe-se aceitar pelo menos oito, preservando a verificação das sete históricas e todas as asserções de conteúdo. O novo teste da lixeira exige pelo menos nove e preservação do prefixo de oito. Esses dois ajustes estão em diff separado, ainda não aplicados, e precisam de aprovação explícita.

O pacote de backup continua na versão 1. Backup novo preserva lixeira e encerramento; backup do esquema oito restaura tarefas ativas sem mudar a origem. Validar também conteúdo escondido e instantes malformados, inclusive fora do dia consultado e em arquivos com hashes coerentes.

## Resultado RED

AgendaTrashStorageTests.cs: **31 descobertos, 27 falhas esperadas e quatro guardas passando**, zero ignorados. Vinte e seis falhas chegam a operações ainda NotImplementedException; uma constata a nona migração ausente. As verificações posteriores às operações vazias ainda não foram alcançadas; isso não demonstra exclusão/restauração ou recuperação prontas.

Separadamente, os **738 casos anteriores passaram** (117 unitários e 621 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados. A suíte completa atual contém RED novo; não está GREEN.

```bash
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter FullyQualifiedName~AgendaTrashStorageTests --logger trx --results-directory artifacts/agenda-trash-storage-red --verbosity quiet
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaTrashStorageTests' --logger trx --results-directory artifacts/agenda-trash-storage-baseline --verbosity quiet
```

Somente contratos mínimos compiláveis e testes foram adicionados. Nenhuma migração, modelo EF, rotina de leitura/gravação, registro na composição ou interface de exclusão foi implementada. Nenhum teste antigo, perfil pessoal, Git, publicação ou manual/PDF alterado.

Depois da revisão e do GREEN: especificar confirmação de exclusão, editor/rascunhos, tela da lixeira e restauração na Agenda. Só então seguir para a visão semanal.

Para estudar: por que excluir logicamente exige proteger também as gravações? Por que restauração deve preservar o estado anterior? Por que validar apenas as tarefas visíveis deixa um backup inseguro?

[Relatório](../reviews/079-agenda-trash-storage-red.md).
