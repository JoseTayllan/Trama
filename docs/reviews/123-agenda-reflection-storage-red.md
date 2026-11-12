# Reflexão — persistência em RED

Data: 10/11/2026. Aprovação dos 33 casos, leitor e migração 12 proposta pendente conforme CODEX.md §9.

Novos arquivos IAgendaReflectionReader.cs e AgendaReflectionStorageTests.cs. Nenhuma implementação de repositório, modelo EF, migração ou backup antecipada. Contratos/regras anteriores e testes antigos intactos.

Cobertura: reabertura, edição sem duplicatas, limpeza, texto longo/Unicode, semanas/perfis, fechamento preservado, leitura ausente, validação de chave/textos/UTC, vínculo ao fechamento, FK real, transação externa/rollback, leitura atual no mesmo contexto, cancelamento no insert/retry, concorrência, migração preservando dados e onze versões anteriores, backup formato 1 e legado, dados corrompidos em leitura/exportação/restauração e pai corrompido.

Decisões para revisão: timestamp persistido em UTC e diferente do padrão; nulo inválido no adaptador; FK sem exclusão em cascata; último registro confirmado prevalece em concorrência, sem comparação pelo timestamp. Pacote de backup continua 1. A consulta GetAsync não cria reflexão.

Perfis temporários isolados; o interceptor cancela apenas o comando de inserção de reflexões. SQL que desativa FK ou corrompe registros roda exclusivamente nesses arquivos de teste. Nenhum segredo literal, logging novo ou acesso ao perfil pessoal. Sem Git, publicação ou manual/PDF.

Esforço moderado, sem subagentes: rodada focada e regressão, reutilizando serviços de fechamento/backup e fixtures SQLite. Tokens/custo exatos não medidos. Serviço/registro no App e formulário continuam fora desta fatia.

[Estudo](../estudos/116-reflexao-sqlite-especificacao.md). Evidências: trama-agenda-reflection-storage-red.

Verificação final: 33 falhas esperadas pelo repositório ausente. Os 1.261 anteriores passaram separadamente (224 unitários e 1.037 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. A pré-validação corrigiu duas quebras de linha antes da execução; isso não foi tratado como RED funcional. Hashes confirmam todos os arquivos anteriores de src/tests intactos. Filtro focado: FullyQualifiedName~AgendaReflectionStorageTests; base: filtro inverso.
