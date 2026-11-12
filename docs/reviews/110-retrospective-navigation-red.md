# Retrospectiva — carregamento e navegação em RED

Data: 09/11/2026. Aprovação dos 29 casos pendente conforme CODEX.md §9.

Somente AgendaRetrospectiveNavigationTests.cs adicionado aos testes. Nenhum modelo de apresentação, implementação, contrato, migração ou teste anterior alterado. O tipo proposto é AgendaRetrospectiveViewModel, com serviço, comparador e relógio obrigatórios; a reflexão permite descobrir sua ausência sem impedir compilação.

Cobertura: estado inicial sem I/O, normalização para segunda, semana encerrada pelo relógio local, duas capturas, anterior ausente sem criação, fechamento vazio, menor data, falhas independentes, preservação do último resultado, recuperação, feedback sem detalhes internos, bloqueio concorrente, token propagado, cancelamento antes/durante consultas, atualização pela última semana bem-sucedida, notificações e dependências obrigatórias.

Risco explícito: GetOrClose pode confirmar um fechamento antes de falhar a leitura da semana anterior. A próxima tentativa recupera a captura existente; este coordenador não deve apagar ou recalcular esse registro para simular rollback entre chamadas. Cancelamento não desfaz commit já ocorrido.

Este RED não valida rascunhos do editor, navegação entre módulos ou inicialização real do App: a integração com esses consumidores será uma fatia posterior. Sem XAML novo ou disparo automático.

Inspeção não encontrou segredo literal ou logging novo. Dados são fictícios, relógio e gates controlados; não se lê o perfil pessoal. Esforço moderado, sem subagentes, uma execução focada e uma regressão. Tokens/custo exatos não medidos. Reutilizar builder/comparador/resumo evita duplicação. Sem Git, publicação ou manual/PDF.

[Estudo](../estudos/103-retrospectiva-navegacao-especificacao.md). Evidências/diff: trama-retrospective-navigation-red.

Verificação final: 29 falhas esperadas pela classe ausente; 1.148 anteriores aprovados separadamente (199 unitários e 949 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os arquivos anteriores de src/tests preservados. Comandos: `dotnet build Trama.slnx -c Release --no-restore`, `dotnet format Trama.slnx --verify-no-changes --no-restore`, teste focado com filtro `FullyQualifiedName~AgendaRetrospectiveNavigationTests` e base com filtro inverso.
