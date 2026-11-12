# 97 — Persistência dos fechamentos: especificação

Data: 07/11/2026. 33 casos em RED para revisão antes da implementação.

Os cálculos estão implementados. Esta fatia propõe guardar a captura no SQLite e preservar o primeiro fechamento de cada semana por perfil. Somente os contratos IAgendaRetrospectiveStore/Source e AgendaRetrospectiveStorageTests.cs foram adicionados; não há repositório ou migração nova ainda.

## Contratos propostos

IAgendaRetrospectiveStore.TryAddAsync(report, timeZone) retorna true quando insere e false quando a semana já existe, sem alterar o relatório anterior. A chave é a segunda-feira, independente de mudanças posteriores de fuso. GetAsync recebe essa segunda-feira e retorna nulo se ausente. ListAsync filtra datas iniciais inclusivas e retorna semanas mais recentes primeiro. Intervalo invertido e chave fora de segunda-feira são rejeitados; cancelamento prévio não consulta/grava.

IAgendaRetrospectiveSource.ListOverlappingAsync retorna o snapshot e LastCompletedAt juntos, em uma leitura SQL, evitando combinar versões entre consultas separadas e o padrão de uma consulta por tarefa. A sobreposição é inclusiva; lixeira e planejamento totalmente externo ficam de fora. Reabertura preserva o timestamp antigo como dado de entrada, deixando ao cálculo já aprovado a decisão de não contá-lo como conclusão atual. Legados continuam sem data inventada.

O adaptador previsto é SqliteAgendaRetrospectiveRepository com TramaDbContext emprestado, seguindo os repositórios existentes. Serviço com contexto por operação/composição será outra fatia; esta não muda a inicialização do aplicativo.

## Modelo e migração propostos

Décima primeira migração, preservando as dez anteriores. Tabela AgendaWeekRetrospectives com chave StartDate, cabeçalho do período/captura, FormatVersion 1, Content com o relatório em JSON Web e TimeZoneData com regras serializadas do TimeZoneInfo. TimeZoneId permanece no relatório; a validação não deve depender de localizar esse ID no Windows/Linux.

Os dados capturados ficam no próprio relatório, sem chave estrangeira para tarefas atuais. Editar, reagendar, reabrir, enviar para lixeira ou remover uma linha de tarefa não altera a captura. O repositório não oferece Update/Delete de fechamentos nesta fatia. Inserções concorrentes da mesma semana devem produzir exatamente um vencedor e preservar seu conteúdo; erros de gravação não deixam linha parcial e permitem nova tentativa.

A validação deve recalcular a captura com o builder aprovado e o fuso serializado, conferindo cabeçalho, tarefas, dias, categorias e indicadores. Relatório inconsistente é rejeitado antes de inserir, inclusive quando o período já existe. JSON inválido, totais alterados, versão futura ou fuso inválido devem ser rejeitados na leitura e no backup, com erros normalizados.

## Backup e compatibilidade

O pacote de backup continua na versão 1; FormatVersion do registro também começa em 1, mas é outro versionamento. Backup preserva captura e regras de fuso. Restauração de banco anterior à tabela migra somente a cópia e começa sem relatórios inventados. O banco original permanece byte a byte intacto.

Uma captura corrompida não deve gerar backup válido nem ser publicada em uma pasta restaurada. Os testes fornecem arquivos com manifesto/hash coerentes e conteúdo inválido para verificar a validação semântica, além da integridade do ZIP.

## Evidências e próximos passos

33 falhas esperadas pelo adaptador/tabela/migração ausentes; build Release e formatação passaram. Resultado da base e hashes no [relatório](../reviews/104-retrospective-storage-red.md). Perfis temporários e conteúdo fictício; nenhum dado pessoal foi usado.

Após aprovação: implementar a migração nova, adaptador e validação do backup, preservando os testes. Depois serviço de captura/fechamento, comparação e painel. A leitura única associa planejamento e data real de cada tarefa no mesmo resultado; a transação que coordenará leitura e gravação do fechamento precisa de cobertura na próxima fatia do serviço. Não confundir esta proposta com fechamento automático já funcionando.
