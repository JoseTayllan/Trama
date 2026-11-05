# Lixeira da Agenda — SQLite em RED

**Atualização de 05/11/2026:** 31 casos e dois ajustes antigos aprovados e implementados; testes novos preservados. Suíte completa com 769 aprovados, build/formatação. [Entrega GREEN](080-agenda-trash-storage-green.md). O texto abaixo preserva o histórico RED.

Data: 05/11/2026. Para revisão dos casos e dos dois ajustes de expectativas antigas, antes da implementação.

Contratos mínimos IAgendaTrashActions, AgendaTaskTrashEntry e SqliteAgendaTrashActions vazio. Proposta: nona migração com DeletedAt opcional, exclusão lógica por ID, preservação de planejamento/estado/encerramento e restauração sem recriar tarefa. Normalmente a tarefa excluída não é lida, listada ou concluída; permanece disponível na lixeira e nos backups.

31 casos: 27 falhas esperadas e quatro guardas passando, zero ignorados. Vinte e seis operações ausentes e uma migração ausente explicam as falhas; asserções posteriores não alcançadas. Base anterior: 738 aprovados (117 unitários e 621 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram. Nenhum teste antigo alterado.

Dois testes de migração/restauração antigos usam contagem exata oito. Diff proposto altera somente essas duas expectativas para pelo menos oito, mantendo conteúdo e prefixo histórico. O novo caso exige ao menos nove e preserva as oito anteriores. Proposta ainda não aplicada; aprovação faz parte desta revisão.

Riscos para o GREEN: gravação atrasada reativar/alterar tarefa na lixeira, seleção de fechamento desatualizada, restauração mudar estado ou perder último encerramento, filtros esconderem conteúdo inválido de backup, falha parcialmente aplicar exclusão/restauração e leitura criar banco vazio. SQL/backup usam perfis fictícios e triggers de falha fixos, sem dados pessoais.

Sem migração/modelo/backup/composição/interface alterados, teste antigo modificado, dependência, log, segredo literal, Git ou publicação. Manual/PDF e README preservados. Inspeção de segredos limitada aos arquivos envolvidos.

Esforço moderado, sem novos subagentes: uma rodada RED e uma da base anterior. Reutilizar migração/backup existentes e os mesmos fixtures reduz duplicação. Tokens/custo monetário não foram medidos. Confirmação, editor e tela da lixeira serão especificados em fatia posterior; não declarar a exclusão disponível no app.

[Estudo](../estudos/72-lixeira-da-agenda-especificacao.md).
