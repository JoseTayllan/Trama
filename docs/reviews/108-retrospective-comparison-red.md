# Retrospectiva — comparação e apresentação em RED

Data: 09/11/2026. Aprovação dos 33 casos e contrato pendente conforme CODEX.md §9.

Novo AgendaRetrospectiveComparison.cs contém enum, record de transporte e IAgendaRetrospectiveComparer; novo AgendaRetrospectiveComparisonTests.cs descreve comparação pura e preparação de apresentação Desktop. Tipos de implementação são procurados por reflexão. Nenhum comparador, SummaryViewModel, XAML, banco, serviço ou teste antigo foi alterado.

33 casos propostos: diferenças percentuais e contagens, sinais, valores arredondados preservados, falta de semana anterior, vazio, fuso registrado, adjacência, limite mínimo, qualidade dos dados de ambas as semanas, texto português independente da cultura, abertas separadas de concluídas sem data/fora e preservação dos dados capturados.

Pontos para revisão: usar apenas a semana imediatamente anterior; sem histórico não significa 0%. Sem tarefas impede delta percentual, mas permite diferenças de contagem quando há dois relatórios comparáveis. IDs de fuso diferentes impedem comparação direta por cautela; offsets distintos com o mesmo ID não impedem. Diferença é calculada em p.p. a partir dos percentuais guardados. Avisos sobre conclusões sem data podem coexistir com delta calculado; a métrica continua limitada às conclusões datadas no período.

O modelo de apresentação não consulta dados nem produz avaliação subjetiva de desempenho. Não altera fechamento, completa tarefas ou movimenta pendências. Ainda sem painel renderizado ou abertura automática; estes precisam de integração e QA próprios.

Inspeção não encontrou segredo literal ou logging novo. Testes usam capturas fictícias do builder existente, fuso controlado e restauração da cultura após o caso de formatação. Esforço moderado, sem subagentes: uma rodada focada e uma regressão; reutilizar os fechamentos preserva consistência e reduz uso. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF.

[Estudo](../estudos/101-retrospectiva-comparacao-especificacao.md). Evidências/diff: trama-retrospective-comparison-red.

Verificação final: 33 falhas esperadas; 1.115 anteriores aprovados separadamente (199 unitários e 916 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os arquivos anteriores de src/tests preservados.
