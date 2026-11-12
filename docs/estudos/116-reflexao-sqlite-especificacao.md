# 116 — Reflexão semanal em SQLite e backup

Data: 10/11/2026. Proposta com 33 casos antes da implementação.

O caso de uso SaveAgendaReflection já está implementado. Esta etapa propõe SqliteAgendaReflectionRepository com contexto emprestado por operação, implementando o contrato de gravação existente e o novo IAgendaReflectionReader.GetAsync. Serviço de aplicativo e habilitação do formulário vêm depois.

## Modelo e migração propostos

A migração 12 adicionará AgendaWeekReflections com WeekStart como chave primária e referência ao StartDate de AgendaWeekRetrospectives. Os textos WorkedWell/Adjustments e UpdatedAt serão obrigatórios no banco. A referência deve impedir reflexões órfãs e a exclusão de um fechamento com reflexão; nenhuma migração anterior será reescrita.

Salvar deve inserir ou substituir um registro inteiro pela chave, preservando os dois textos e o instante de atualização. Atualizar não cria duplicatas nem altera o JSON do fechamento. Textos vazios são válidos e podem limpar dados anteriores; nulo é inválido no adaptador, pois a normalização já pertence ao caso de uso. Chave não segunda-feira, timestamp padrão ou offset diferente de UTC também são inválidos na persistência.

## Leitura e concorrência

GetAsync retorna nulo quando não existe reflexão e rejeita chaves inválidas. HasClosureAsync confirma o fechamento existente e deve rejeitar um pai corrompido, em vez de aceitar apenas a existência física da linha. A leitura da reflexão também valida seu vínculo e seus metadados.

A gravação real deve revalidar o vínculo e usar a restrição do banco, não confiar somente na consulta anterior do caso de uso. Um UPSERT único deve manter os campos do registro juntos em concorrência. Duas escritas da mesma semana deixam uma das versões completas; não há mesclagem, histórico de versões ou garantia de que a maior data prevalecerá. Semanas distintas preservam seus próprios registros.

O contexto pertence ao chamador. O adaptador deve respeitar transações externas e cancelamento, permitindo rollback e nova tentativa. Os testes verificam cancelamento no limite de inserção com interceptor, sem sleeps, e leitura sem cache de dados antigos no mesmo contexto.

## Compatibilidade e backup

O pacote permanece na versão 1. Reflexões devem acompanhar o SQLite no backup/restauração; bancos da migração 11 sem reflexões são migrados na cópia restaurada, sem inventar textos nem modificar o arquivo original. A migração nova preserva notas, tarefas, lixeira, fechamentos e o histórico das onze anteriores.

Leitura/exportação/restauração devem rejeitar timestamps inválidos, offsets não UTC e reflexões órfãs. A restauração não publica o destino quando falhar. O vínculo com fechamento corrompido também é inválido. Esses testes usam arquivos temporários e corrupção deliberada apenas nos perfis fictícios.

## Limites

Somente IAgendaReflectionReader e os testes foram adicionados nesta fase. Não há tabela/migração/repositório implementados nem alteração dos 25 casos já aprovados das regras. Os testes descobrem o repositório por reflexão e devem falhar por sua ausência, mantendo a solução compilável.

Após aprovação, implementar SQLite/migração/validação de backup e executar a suíte. Serviço por perfil, registro no App e interface com rascunhos protegidos precisam de especificações próprias. A prévia visual continua somente leitura até essas etapas.

[Revisão](../reviews/123-agenda-reflection-storage-red.md).
