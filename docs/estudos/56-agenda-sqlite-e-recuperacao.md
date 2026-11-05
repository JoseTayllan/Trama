# 56 — Agenda: SQLite e recuperação implementados

Data: 02/11/2026. Implementação após aprovação dos 24 casos do capítulo 55; diff para revisão humana.

## Armazenamento

`AgendaTaskRecord` representa a tabela AgendaTasks. A sétima migração, `20261103015956_AgendaTasks`, cria somente essa tabela e um índice de PlannedDate/StartTime. ID, título, descrição, dia e horários são preservados; categorias e estados usam valores numéricos estáveis.

As seis migrações anteriores permanecem intactas. O snapshot do EF passa a incluir a entidade nova. A data no identificador da migração foi gerada pelo EF; os registros deste capítulo seguem a data local da sessão.

`SqliteAgendaTaskRepository` recebe um contexto pertencente ao chamador. Salvar consulta a identidade e insere ou atualiza os campos, aguardando SaveChangesAsync. Não confirma ou descarta uma transação externa. A futura composição deverá criar e descartar um contexto por operação.

## Validação e consultas

As regras aprovadas foram extraídas de SaveAgendaTask para `AgendaTaskValidation`, reutilizado pela aplicação e pela leitura SQLite. Os testes do caso de uso continuam idênticos. O repositório também protege escrita direta e rejeita descrição nula, conforme o contrato textual não anulável.

Consultas usam AsNoTracking e retornam snapshots. Isso evita entregar entidades EF à interface e permite que leituras repetidas vejam mudanças confirmadas em outro contexto.

O período inclui os dois dias. A ordenação é feita por dia, presença de horário, início, título e ID. Tarefas sem horário ficam depois das agendadas no mesmo dia. Período invertido é rejeitado; tarefa ausente retorna null.

Erros esperados de banco são normalizados em AgendaPersistenceException, sem incluir o conteúdo da tarefa na mensagem. Cancelamento segue como OperationCanceledException. Dados inválidos lidos do armazenamento são rejeitados, incluindo título vazio e enums desconhecidos.

## Backup e restauração

O pacote permanece na versão 1 e inclui o banco com as tarefas. A validação do backup examina todas as linhas da Agenda, sem filtrar por data; uma linha inválida não pode desaparecer da validação por ficar fora de um período.

Na restauração, o fluxo existente valida o histórico de migrações e atualiza a cópia em staging antes de validar os registros. Assim, um backup anterior à Agenda ganha uma tabela vazia sem modificar sua origem. Um banco com dados inválidos é rejeitado mesmo quando o manifesto tem hashes coerentes.

Criar uma cópia exige perfil já atualizado, preservando a política anterior. A inicialização normal do aplicativo aplica as migrações disponíveis. Nenhuma operação foi executada no perfil pessoal nesta verificação.

## Evidências

Os 24 casos aprovados passaram intactos com SQLite e arquivos reais em diretórios fictícios. Conferiram reabertura, precisão do horário, atualização, consultas, falhas, migração, corrupção e recuperação. As seis migrações anteriores e todos os testes são comparados por hash.

A suíte completa no WSL passou: 101 unitários e 482 integrações, total de 583, sem ignorados. Build Release sem avisos/erros e formatação passaram. A instabilidade anterior de autosave não se reproduziu nessa rodada; a causa histórica não foi corrigida ou demonstrada por esta mudança.

## Limites e próxima etapa

O armazenamento ainda não foi registrado na composição nem conectado ao formulário. O botão Agenda continua desabilitado. Não há métricas, registros de tempo realizado, recorrência, vínculos com páginas ou exclusão de tarefas nesta fatia.

Atualizações usam o ID fornecido e não possuem controle de concorrência entre duas edições simultâneas. Não apresentar o upsert como detecção de conflitos. A próxima fatia deve especificar o serviço que cria contextos por operação e o formulário/lista com proteção de rascunhos, inclusive ao fechar o aplicativo.

O uso Windows da Agenda e a revisão humana do diff ainda serão necessários. Não foi gerado executável novo nesta fatia sem interface.

## Para estudar

- Por que manter os dados da Agenda em tabela própria?
- Por que uma consulta sem tracking enxerga melhor atualizações externas?
- Por que validar todas as linhas do backup em vez de consultar apenas a semana atual?
- Qual a diferença entre atualizar pelo mesmo ID e detectar conflito entre duas edições?
