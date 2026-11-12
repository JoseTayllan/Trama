# 100 — Serviço de fechamento implementado

Data: 09/11/2026. Implementação após aprovação dos 34 casos e contrato, preservados. Diff para revisão.

SqliteAgendaRetrospectiveActions implementa IAgendaRetrospectiveActions e é registrado em DesktopComposition para o mesmo perfil das demais funções. Recebe caminho absoluto e TimeProvider; guarda opções de conexão imutáveis e cria/descarta TramaDbContext por operação. A conexão abre em ReadWrite, sem criar um banco ausente nem aplicar migração silenciosamente.

## Obter ou fechar uma semana

GetOrCloseAsync valida cancelamento e limites do calendário antes de abrir o banco. Normaliza a referência para segunda/domingo usando o projetor existente. Consulta primeiro a captura salva: se existir, retorna seus dados validados, sem consultar o relógio ou tarefas atuais.

Se não existir, inicia transação serializável do SQLite e consulta novamente a mesma chave. Essa segunda leitura cobre uma captura criada entre a consulta inicial e a aquisição da transação. Na criação, a reserva de escrita antecede a leitura dos dados vivos; uma edição concorrente espera o fechamento terminar.

O relógio local injetado determina fuso e ClosedAt. O builder valida primeiro que a semana já acabou, antes de ler tarefas. A fonte lê planejamento e conclusão juntos, o builder calcula a captura e o repositório a insere sem sobrescrita. O serviço verifica cancelamento antes do commit e retorna o vencedor. Transação/contexto são descartados em erro ou cancelamento, sem linha parcial.

Dados de tarefas inválidos, inclusive conclusão posterior à captura, geram AgendaPersistenceException. Semana aberta continua sendo erro de argumento; cancelamento continua OperationCanceledException. A captura existente usa as regras de fuso guardadas e permanece acessível mesmo se o relógio atual ou os dados vivos mudarem.

## Histórico e composição

GetAsync exige a segunda-feira e apenas lê uma captura. ListAsync valida limites e retorna histórico inclusivo, mais recente primeiro. Nenhum deles cria semanas ausentes. Cada chamada tem contexto próprio, permitindo recuperação após falha e uso da mesma instância em chamadas concorrentes.

DesktopComposition publica o serviço como singleton, mantendo somente opções e relógio, sem compartilhar DbContext. O teste de composição lê a captura do perfil informado; a integração na abertura do painel ainda é futura. Nenhum fluxo atual de App foi substituído por outro caminho de inicialização.

## Verificação e limites

Os 34 casos aprovados e o contrato permaneceram intactos. Resultados finais, hashes e diff no [relatório](../reviews/107-retrospective-service-green.md). Testes verificam transação com edição concorrente, duas chamadas da mesma semana retornando o mesmo vencedor, semanas distintas, cancelamento no insert, falha/retry, banco ausente, fuso, legado, composição e preservação das tarefas.

Ainda não existe gatilho automático, comparação com semana anterior ou painel funcional. O serviço atua somente sobre a semana solicitada; não fecha tarefas nem reagenda pendências. A interface e a política de abertura automática terão especificação própria, incluindo o ponto de entrada real do aplicativo.

Sem migração, dependência, alteração dos testes anteriores, perfil pessoal, publicação Windows, Git ou manual/PDF nesta entrega.

Para estudar: por que consultar novamente depois de iniciar a transação? Por que a captura existente não depende do relógio atual? Por que singleton de serviço não significa singleton de contexto?

[Especificação](99-retrospectiva-servico-especificacao.md) · [Relatório](../reviews/107-retrospective-service-green.md).
