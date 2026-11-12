# 117 — Reflexão semanal persistida em SQLite

Data: 10/11/2026. Implementação após aprovação dos 33 casos, leitor e migração 12.

SqliteAgendaReflectionRepository implementa IAgendaReflectionRepository e IAgendaReflectionReader, usando o contexto fornecido pelo chamador. HasClosureAsync usa o repositório de retrospectivas para validar a captura, e GetAsync retorna nulo quando não há reflexão. Uma reflexão presente exige textos válidos, timestamp UTC não padrão e fechamento válido.

## Escrita e integridade

O adaptador valida a entrada, reconsulta o fechamento e grava em um único UPSERT parametrizado. O mesmo registro recebe os dois textos e o timestamp juntos; outras semanas não são afetadas. A chave primária impede duplicação e a referência restritiva impede reflexões órfãs e exclusão do pai enquanto houver reflexão.

O repositório não chama SaveChanges para descarregar alterações que o chamador esteja rastreando. A escrita participa da transação do contexto, e o cancelamento pode interromper o comando antes da gravação. A leitura usa AsNoTracking e devolve o valor atualizado no mesmo contexto. A concorrência mantém um registro inteiro por semana, sem mesclagem ou histórico de versões.

## Migração e backup

A migração 12 `20261111005518_AgendaWeekReflections` cria apenas AgendaWeekReflections, com chave WeekStart, textos e UpdatedAt. O identificador foi gerado em UTC. A referência aponta para AgendaWeekRetrospectives.StartDate, com ON DELETE RESTRICT. As onze migrações anteriores permanecem intactas.

O pacote de backup permanece no formato 1. A validação existente inclui reflexões, verificando metadados e vínculo; arquivos inválidos não são exportados/restaurados como perfis válidos. A restauração de backup da migração 11 aplica a nova tabela na cópia, sem inventar reflexão ou modificar o original. A captura histórica é preservada junto aos textos.

## Próxima etapa

O repositório já persiste em banco isolado nos testes, mas ainda não está conectado ao fluxo do aplicativo. O serviço por perfil e a composição virão depois, seguidos pela edição e gravação manual na tela, com proteção de rascunhos. A prévia visual continua intacta.

[Especificação aprovada](116-reflexao-sqlite-especificacao.md) · [Revisão](../reviews/124-agenda-reflection-storage-green.md).
