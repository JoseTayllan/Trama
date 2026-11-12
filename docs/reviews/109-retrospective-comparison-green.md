# Retrospectiva — comparação implementada

Data: 09/11/2026. Implementação autorizada pelos 33 casos e contrato aprovados.

Dois arquivos de implementação: AgendaRetrospectiveComparer na Application e AgendaRetrospectiveSummaryViewModel no Desktop. Comparação pura de capturas adjacentes, estados explícitos para comparação indisponível, diferenças em pontos percentuais e contagens, avisos de dados incompletos e resumo somente leitura. Sem migração, pacote novo, alteração de contrato aprovado ou teste antigo.

Revisão: ausência de histórico não vira zero; semanas vazias não produzem percentual; menor data não sofre underflow; sinais e cultura são explícitos; abertas não incluem concluídas sem data; não há leitura de tarefas atuais, escrita no banco ou logs/segredos novos. Os objetos de entrada devem vir do builder/armazenamento validado. IDs de fuso diferentes bloqueiam a comparação por cautela, sem consulta ao sistema operacional. A interface e a composição deste resumo permanecem para a próxima fatia.

Esforço moderado, sem subagentes. Uma rodada focada e uma suíte completa; reaproveitar as capturas e separar a apresentação pura evita repetir consultas e renderizações nesta etapa. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF. Revisão humana do diff pendente.

[Estudo](../estudos/102-retrospectiva-comparacao-implementada.md). Evidências: trama-retrospective-comparison-green.

Verificação final: 33 casos e contrato aprovados intactos por hashes. Suíte completa com 1.148 aprovados (199 unitários e 949 integrações), zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Todos os arquivos anteriores de src/tests preservados.
