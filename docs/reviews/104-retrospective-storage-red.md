# Retrospectiva — persistência em RED

Data: 07/11/2026. Aprovação dos 33 casos, contratos e migração proposta pendente conforme CODEX.md §9.

Somente IAgendaRetrospectiveStore.cs, IAgendaRetrospectiveSource.cs e AgendaRetrospectiveStorageTests.cs adicionados ao produto/testes. Sem implementação, mudança em mapeamento, migração ou backup. Classe futura procurada por reflexão para manter o runner compilável; falhas por adaptador/tabela/migração ausentes.

Proposta: um fechamento por segunda-feira/perfil, primeiro vence e nenhuma sobrescrita. Dados independentes das tarefas atuais. Cabeçalho + relatório JSON Web versão 1 + regras serializadas do fuso; migração 11 preserva as dez existentes. Leituras retornam snapshots validados sem depender de TimeZoneInfo.FindSystemTimeZoneById, permitindo validar fusos personalizados e capturas restauradas entre sistemas.

33 casos cobrem roundtrip, duplicidade/fuso, inserção concorrente, histórico, independência de tarefas/perfis, entradas inválidas, cancelamento/falha, migração, leitura conjunta de planejamento/conclusão e backup/restauração. O interceptor exige uma leitura SQL para o conjunto tarefa/data real. Corrupções de JSON/totais/versão/fuso devem falhar em leitura, exportação e restauração; backup legado migra só a cópia.

Pontos para revisão: a chave não inclui fuso — mudar fuso não cria outro relatório da mesma semana. Fuso é um argumento da gravação e suas regras ficam no banco, sem alterar o contrato do cálculo aprovado. O formato do registro começa em 1; o pacote de backup continua 1. Não há atualização, exclusão ou recálculo silencioso do fechamento. O serviço que coordenará captura e gravação ainda não existe.

Inspeção não encontrou segredo literal ou logging novo. Bancos temporários; exclusão SQL em um caso só comprova independência da captura em fixture descartável. Esforço moderado, sem subagentes: uma rodada focada e uma regressão. Reutilizar builder e backup atuais reduz escopo e custo. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF.

**Verificação:** 33 falhas esperadas por adaptador/tabela/migração ausentes; build Release sem avisos/erros e formatação aprovados no WSL. Os 1.048 anteriores passaram separadamente (199 unitários e 849 integrações), sem falhas/ignorados. Hashes confirmam src/tests anteriores intactos; a suíte incluindo a classe nova permanece em RED.

[Estudo](../estudos/97-retrospectiva-persistencia-especificacao.md). Evidências/diff: trama-retrospective-storage-red.
