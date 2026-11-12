# Reflexão semanal — regras em RED

Data: 10/11/2026. Aprovação dos 25 casos e contratos pendente conforme CODEX.md §9.

Novo AgendaWeekReflection.cs contém apenas registro, status/resultado e IAgendaReflectionRepository. Novo SaveAgendaReflectionTests.cs especifica o caso de uso, ainda ausente. Nenhum serviço, banco, migração, XAML ou teste anterior alterado.

Cobertura: textos e UTC preservados, campos opcionais, limpeza, atualização da mesma semana, semanas independentes, conteúdo longo/Unicode/espaços, seis chaves inválidas, fechamento ausente, menor segunda-feira representável, cancelamento antes/durante operações, propagação dos tokens, falhas/retry, erro inesperado e dependências obrigatórias.

Riscos/decisões para revisão: ambos os campos podem ficar vazios, sem exclusão implícita do registro; salvar substitui o texto sem histórico completo; não truncar conteúdo. O registro é independente da captura histórica. Perfis, atomicidade, concorrência, validação de corrupção e backup exigem testes de armazenamento próprios; o repositório em memória não prova essas garantias.

Relógio fixo e dados fictícios, sem leitura do banco pessoal. Nenhum segredo literal ou logging novo. Esforço moderado, sem subagentes: rodada focada e regressão; contratos pequenos e fluxo reaproveitável reduzem escopo. Tokens/custo exatos não medidos. Sem Git, publicação ou manual/PDF.

[Estudo](../estudos/114-reflexao-semanal-regras-especificacao.md). Evidências: trama-agenda-reflection-rules-red.

Verificação final: 25 falhas esperadas pela classe SaveAgendaReflection ausente. Os 1.236 anteriores passaram separadamente (199 unitários e 1.037 integrações), zero falhas/ignorados na base. Build Release sem avisos/erros e formatação aprovados no WSL. Hashes confirmam todos os arquivos anteriores de src/tests intactos. Filtro focado: FullyQualifiedName~SaveAgendaReflectionTests; base: filtro inverso.
