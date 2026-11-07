# Agenda semanal — projeção e filtros em RED

Data: 06/11/2026. Aguardar aprovação explícita conforme CODEX.md §9.

Somente dois arquivos de contratos Application e AgendaWeekProjectionTests.cs adicionados ao produto/testes. AgendaWeekProjector ainda ausente: os 36 casos compilam e falham em Assert.NotNull ao procurar a implementação. Não são falhas de compilação ou infraestrutura; as asserções de comportamento serão exercitadas após a aprovação.

Cobertura proposta: semana segunda–domingo, virada de ano, vazios, sobreposição por dia, continuidade entre semanas, horários apenas nas extremidades, meia-noite inclusiva, contagem única, ordenação, filtros combinados, preservação independente de categoria/estado, entradas inválidas e limites de DateOnly. Contratos anteriores, persistência e interface preservados.

Diretriz visual registrada: cor/faixa da categoria e ícone com rótulo para cada estado. Pendente com círculo, Em andamento com progresso, Concluída com check. Ainda sem renderer ou estilos novos; os testes de modelo não certificam aparência/acessibilidade da futura tela.

Riscos para revisão: período inclusivo inclui o dia final mesmo à meia-noite; tarefa sem data final não se estende por continuar aberta; ordenação usa início original; snapshots inválidos são rejeitados mesmo quando filtros os esconderiam. Consultas e projeção deverão compartilhar essas regras sem alterar a consulta diária existente.

Inspeção dos arquivos novos não encontrou segredo ou logging. Esforço moderado, sem subagentes: uma execução focada e uma regressão. Reutilizar snapshots e o leitor existente reduz escopo e custo; tokens/custo exatos não medidos.

**Verificação final:** 36 falhas esperadas na especificação; 912 testes anteriores aprovados (131 unitários e 781 integrações). Build Release sem avisos/erros e formatação passaram no WSL. Hashes confirmam todos os arquivos anteriores de src/tests intactos.

Resultados finais, hashes e diffs ficam nos artefatos trama-agenda-week-model-red. [Explicação](../estudos/87-semana-projecao-e-estados-especificacao.md).
