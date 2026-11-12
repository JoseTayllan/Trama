# Retrospectiva — serviço em RED

Data: 09/11/2026. Aprovação dos 34 casos e contrato pendente conforme CODEX.md §9.

Somente IAgendaRetrospectiveActions.cs e AgendaRetrospectiveServiceTests.cs adicionados ao produto/testes. Classe futura SqliteAgendaRetrospectiveActions procurada por reflexão; nenhum stub funcional, serviço, registro, migração ou teste antigo alterado. Uma guarda diária passa; falhas esperadas pela classe/registro ausentes.

Contrato: GetOrClose normaliza referência, retorna o fechamento existente ou captura semana encerrada pelo relógio local injetado. Get/List são apenas leitura. Caminho absoluto, contexto por operação, perfil ausente não criado, calendário/cancelamento validados antes de I/O. Falhas de dados/banco normalizadas; cancelamento propagado. Captura existente não depende do relógio ou da validade das tarefas atuais e corrupção não autoriza substituição silenciosa.

34 casos cobrem mistura de estados/legados/continuidades, limite local domingo/segunda, vazio, preservação de dados, histórico, perfis, construtor/argumentos, ausência de banco, cancelamento, recuperação, concorrência e composição. Um caso pausa a inserção e verifica que uma edição concorrente espera o fim da transação, protegendo a coordenação leitura/gravação. A interrupção antes do insert também verifica rollback por cancelamento.

Limites para revisão: não há gatilho automático ao iniciar o aplicativo; somente a semana solicitada é criada. Sem comparação com semana anterior, reagendamento de pendências, conclusão de tarefas ou UI nesta fatia. O serviço deve ser registrado para o mesmo perfil, mas a conexão ao fluxo de abertura visual terá cobertura própria.

Inspeção não encontrou segredo literal ou logging novo. Fixtures SQLite temporárias, fuso/relógio controlados e observação EF limitada ao banco de cada teste. Esforço moderado, sem subagentes: testes focados e uma regressão. Reutilizar builder/repositório e preservar API existente reduz custo e duplicação. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF.

**Verificação:** 33 falhas esperadas pela classe/registro ausentes e uma guarda diária passando; build Release sem avisos/erros e formatação aprovados no WSL. Os 1.081 anteriores passaram separadamente (199 unitários e 882 integrações), sem falhas/ignorados. Hashes confirmam src/tests anteriores intactos. Harness separado instanciou a instrumentação real dos testes e comprovou que observa exatamente o insert do banco alvo, ignorando outro perfil. A suíte incluindo a classe nova permanece em RED.

[Estudo](../estudos/99-retrospectiva-servico-especificacao.md). Evidências/diff: trama-retrospective-service-red.
