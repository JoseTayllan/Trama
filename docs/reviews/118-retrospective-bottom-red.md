# Retrospectiva — composição inferior em RED

Data: 09/11/2026. Nove casos propostos em AgendaRetrospectiveBottomLayoutTests.cs.

Escopo acordado para revisão: pendências e Sua retrospectiva lado a lado na janela ampla, com coluna maior para as pendências; empilhamento compacto; duas perguntas com campos somente leitura e ação Salvar desabilitada, acompanhados de aviso de prévia/salvamento indisponível. Preservar pendências, símbolos/rótulos e rascunho da Agenda.

Ainda sem implementação, alteração de src, migração ou teste anterior. A seção 9 do CODEX exige revisão explícita antes de implementar os novos controles. Os nove casos não especificam gravação da reflexão nem revisão funcional de tarefas.

Risco principal: campos editáveis sem persistência sugeririam que o texto será salvo. A prévia deve deixar a indisponibilidade explícita e impedir edição. Novas funções virão em especificação separada.

Esforço moderado, sem subagentes, reutilizando o harness de interface e os serviços do perfil temporário. Sem dados pessoais, segredo literal ou logging novo. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF.

[Estudo](../estudos/111-composicao-visual-pendencias-e-reflexao.md). Evidências/diff: trama-retrospective-bottom-red.

Verificação final: nove falhas esperadas pela ausência dos controles; 1.227 anteriores aprovados separadamente (199 unitários e 1.028 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os arquivos anteriores de src/tests intactos. Filtro focado: FullyQualifiedName~AgendaRetrospectiveBottomLayoutTests; base: filtro inverso.
