# Retrospectiva — persistência implementada para revisão

Data: 07/11/2026. Após aprovação dos 33 casos, contratos e migração 11. Uma alteração de helper foi aprovada separadamente; nenhuma asserção foi modificada.

Nova AgendaWeekRetrospectiveRecord, mapeamento, migração 20261107143302_AgendaWeekRetrospectives e snapshot do modelo. Novo SqliteAgendaRetrospectiveRepository implementa armazenamento e fonte conjunta; validação do backup inclui todos os fechamentos. Migrações anteriores intactas; pacote de backup e formato do registro permanecem 1.

Revisão: validação antes da inserção, captura canonicalizada e regras do fuso armazenadas. INSERT parametrizado com chave primária StartDate e conflito ignorado preserva o primeiro registro em tentativas concorrentes, sem descarregar entidades não relacionadas do contexto. Leitura AsNoTracking valida versão, cabeçalho, fuso e todos os indicadores usando o builder aprovado. Erros são normalizados; cancelamento não é convertido em falha de persistência. Fonte usa um SELECT para snapshot/data real com sobreposição inclusiva e filtro da lixeira.

Os primeiros resultados foram 31/33 focados e 1.079 aprovações na suíte, com duas falhas no helper antes da execução do SQL. O literal de JSON inválido continha uma chave que o EF tratava como formatação. Após revisão explícita, somente esse caractere foi escapado; o SQL passa a gravar o mesmo JSON inválido originalmente pretendido. Os arquivos antes/depois e o diff aprovado estão nos artefatos; não houve alteração para enfraquecer comportamento ou asserções.

Riscos e limites: somente um fechamento por segunda-feira/perfil, mesmo com mudança de fuso. Não há Update/Delete de relatórios. Captura é independente das tarefas atuais, mas ainda não há serviço do aplicativo coordenando leitura e gravação nem geração automática. Comparação e tela permanecem futuras. Toda verificação utilizou perfis temporários; nenhum perfil pessoal, publicação Windows, Git ou manual/PDF foi alterado.

Inspeção não encontrou segredo literal ou logging novo. Esforço moderado, sem subagentes: implementação, geração/revisão de migração, testes focados e suíte; repetição motivada pelo helper aprovado. Reutilizar o builder e a validação do backup reduziu escopo. Tokens/custo exatos não medidos. Revisão humana do diff e integração no serviço pendentes.

**Verificação final:** 33 casos passaram; contratos e asserções preservados, com somente um caractere de escape no helper aprovado separadamente e conferido por bytes/hashes. Suíte completa com 1.081 aprovados (199 unitários e 882 integrações), zero falhas/ignorados; build Release sem avisos/erros e formatação aprovados no WSL. EF confirmou ausência de mudanças pendentes no modelo. SQL exportado contém apenas criação da tabela e registro da migração 11; dez migrações anteriores intactas. Locks/dependências e demais testes preservados.

[Estudo](../estudos/98-retrospectiva-persistida.md) · [RED](104-retrospective-storage-red.md). Evidências/diff: trama-retrospective-storage-green.
