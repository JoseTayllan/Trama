# Retrospectiva — tela em RED

Data: 09/11/2026. Aprovação dos 28 casos pendente conforme CODEX.md §9.

Somente AgendaRetrospectiveUiTests.cs adicionado aos testes. Nenhum src, migração, contrato ou teste anterior alterado. A implementação visual ainda não existe; controles propostos são procurados na árvore visual real.

Cobertura: modo Retrospectiva, semana padrão/selecionada, formulário preservado e oculto, indicadores, variação em p.p., sete dias com duas séries, categorias, abertas com símbolo/rótulo, avisos, vazio, histórico ausente, metadados da captura, navegação adjacente/limites, atualização, carregamento, falha inicial/retry, falha posterior, retorno aos modos existentes, larguras 760/980/1600, acessibilidade e paleta dinâmica. Um caso protege a Agenda legada sem o novo recurso.

Riscos: não usar tarefas atuais como valores do fechamento; não confundir ausência de histórico com falha; escala diária deve comportar conclusões sem início planejado naquele dia; não recriar o formulário ao ocultá-lo. A UI deve chamar os wrappers da Agenda para preservar IsBusy e o fechamento coordenado. Não expor gravação de reflexão ainda inexistente nem congelar semanas no bootstrap.

Dados fictícios, perfil temporário e relógio controlado; sem segredo literal ou logging novo. Recurso global da paleta restaurado em finally. Esforço moderado, sem subagentes; uma rodada focada e uma regressão, reutilizando a integração existente. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF.

[Estudo](../estudos/107-retrospectiva-tela-especificacao.md). Evidências: trama-retrospective-ui-red.

Verificação final: 28 casos, sendo 27 falhas esperadas por controles ausentes e uma proteção legada passando. Os 1.199 anteriores passaram separadamente (199 unitários e 1.000 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. A pré-validação corrigiu duas asserções para o analisador xUnit e uma quebra de linha no helper; esses erros de preparação não foram tratados como RED funcional. Hashes confirmam todos os arquivos anteriores de src/tests intactos. Filtro focado: `FullyQualifiedName~AgendaRetrospectiveUiTests`; base: filtro inverso.
