# 98 — Fechamentos persistidos e recuperáveis

Data: 07/11/2026. Implementação após aprovação dos 33 casos, contratos e migração 11. O escape de uma chave no helper de corrupção foi aprovado separadamente; todas as asserções foram preservadas.

SqliteAgendaRetrospectiveRepository implementa IAgendaRetrospectiveStore e IAgendaRetrospectiveSource com um TramaDbContext emprestado. A nova tabela AgendaWeekRetrospectives é criada por 20261107143302_AgendaWeekRetrospectives. As dez migrações anteriores permanecem intactas.

## Inserção sem sobrescrita

O repositório valida o relatório recalculando a captura com AgendaRetrospectiveBuilder e o fuso recebido. Confere semana, identificador de fuso, dados capturados e todos os indicadores. Salva o resultado canonicalizado, com coleções materializadas, em JSON Web de formato 1.

StartDate é a chave primária da tabela. Uma única instrução INSERT com ON CONFLICT (StartDate) DO NOTHING retorna true quando insere e false se outra captura já existe. A chave não inclui fuso: trocar o fuso não cria uma segunda versão da mesma semana. Essa instrução atômica resolve tentativas concorrentes sem a janela entre consultar existência e inserir. SQL usa parâmetros; não há concatenação de conteúdo do usuário ou SaveChanges de entidades não relacionadas do contexto emprestado.

Os snapshots ficam dentro da captura, sem referência com cascata para AgendaTasks. Editar, reagendar, reabrir, enviar à lixeira, restaurar ou remover uma tarefa atual não altera o fechamento. Não há operação de atualização/exclusão de fechamentos nesta fatia.

## Leitura e fuso portátil

O registro guarda início/fim, ClosedAt com offset, TimeZoneId, regras TimeZoneData, FormatVersion e Content. A leitura restaura TimeZoneInfo a partir das regras serializadas, sem procurar o ID no sistema operacional. Recalcula e compara cabeçalho/payload/indicadores; rejeita JSON inválido, totais inconsistentes, versão desconhecida ou fuso inválido como AgendaPersistenceException.

GetAsync exige chave segunda-feira e retorna nulo quando ausente. ListAsync usa limites inclusivos sobre as datas iniciais e retorna as semanas mais recentes primeiro. Ambos usam AsNoTracking; cancelamento prévio é propagado antes de I/O.

A fonte consulta planejamento e LastCompletedAt juntos em um SELECT, mantendo a associação dos campos e evitando uma consulta por tarefa. A sobreposição e a exclusão da lixeira seguem as regras existentes. Dados sem conclusão real continuam nulos; timestamps antigos em tarefas reabertas são preservados para o cálculo decidir seu tratamento.

## Backup e migração

LocalBackupService valida também todos os fechamentos do snapshot SQLite, inclusive JSON, metadados e regras de fuso. O pacote continua na versão 1, separado do formato 1 do registro. A restauração de backup anterior migra somente a cópia e começa sem retrospectivas inventadas; o banco original permanece intacto.

Testes verificam restauração do fechamento e fuso, corrupção com manifesto/hash coerentes e rejeição sem publicar uma pasta de destino inválida. O Down da migração remove apenas a tabela nova; nenhum rollback foi executado.

## Verificação e próximos passos

Resultados finais, hashes, diff e ajuste aprovado do helper estão no [relatório](../reviews/105-retrospective-storage-green.md). Os casos usam bancos temporários e não o perfil pessoal. A mudança não altera contratos anteriores, locks, dependências, leitura diária ou interface.

Persistência não significa fechamento automático disponível: ainda faltam serviço com contexto por operação, coordenação da captura/leitura/gravação, comparação entre semanas e tela. Essa coordenação terá seus próprios testes. Não houve publicação Windows, Git ou edição de manual/PDF.

Para estudar: por que a chave única resolve a corrida melhor que consultar antes? Por que armazenar as regras de fuso? Por que um JSON válido pode representar indicadores inválidos? Qual transação ainda falta no fluxo de fechamento completo?

[Especificação](97-retrospectiva-persistencia-especificacao.md) · [Relatório](../reviews/105-retrospective-storage-green.md).
