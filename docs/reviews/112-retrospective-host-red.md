# Retrospectiva — integração com Agenda/App em RED

Data: 09/11/2026. Aprovação dos 22 casos pendente conforme CODEX.md §9.

Dois arquivos novos: AgendaRetrospectiveHostTests.cs e AgendaStartupRetrospectiveTests.cs. Nenhum src, contrato, migração ou teste anterior alterado. Métodos/propriedades/sobrecarga propostos são descobertos por reflexão, mantendo a compilação antes da implementação.

20 casos de integração: compatibilidade diária, composição, inicialização sem captura, modos exclusivos, preservação de lista/semana/dia/rascunhos, instância do editor, saída e ações bloqueadas durante consulta, falhas/cancelamento, atualização e fechamento preservado após edição da tarefa. Dois casos abrem a janela pelo App real em perfil temporário: disponibilidade sem disparo automático e criação no perfil correto com rascunhos mantidos.

Os gates usam TaskCompletionSource sem sleeps; as esperas da inicialização usam Stopwatch com prazo de 15 segundos. O prompt de rascunhos retorna Cancelar e contabiliza chamadas para revelar confirmações indevidas. As consultas reais usam SQLite isolado; não se acessa o perfil pessoal.

Riscos para a implementação: preservar construtores anteriores, manter IsBusy da Agenda durante toda a consulta, não recriar o editor e não esquecer o bootstrap manual do App. Botões futuros devem passar pela Agenda, não acessar o carregador diretamente. Sem registro/abertura automática nesta etapa. UI, paleta e responsividade permanecem para a especificação visual.

Inspeção não encontrou segredo literal ou logging novo. Esforço moderado, sem subagentes: rodada focada e regressão, reutilizando o caminho real de bootstrap e serviços existentes. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF. Revisão humana dos testes pendente.

[Estudo](../estudos/105-retrospectiva-integracao-especificacao.md). Evidências: trama-retrospective-host-red.

Verificação final: 22 falhas esperadas por membros/sobrecarga/registros ausentes, incluindo os dois casos App/Opened/MainWindow. Os 1.177 anteriores passaram separadamente (199 unitários e 978 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os arquivos anteriores de src/tests intactos. Filtro focado: `FullyQualifiedName~AgendaRetrospectiveHostTests|FullyQualifiedName~AgendaStartupRetrospectiveTests`; base: filtro inverso com &.
