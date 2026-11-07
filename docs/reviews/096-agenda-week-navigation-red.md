# Integração semanal — especificação RED

Data: 06/11/2026. Aguardar aprovação explícita conforme CODEX.md §9.

Arquivo novo: AgendaWeekNavigationTests.cs. Nenhuma implementação, migração ou edição de testes anteriores. Os testes compilam usando os contratos existentes; reflexão procura a sobrecarga e operações futuras no AgendaViewModel. As ausências falham em asserções explícitas, sem stubs funcionais.

34 casos: 33 falham e um protege o caminho diário existente. São 29 falhas pela sobrecarga ainda ausente, uma pela propriedade de disponibilidade ausente na composição e três por status-icon ainda vazio. Build Release sem avisos/erros e formatação aprovados no WSL.

Cobertura: consulta por sobreposição no mesmo perfil, continuidade e identidade original, navegação sem descartar rascunho, filtros sem I/O, retorno ao dia, criação com decisão de rascunho, falhas/retry, atualização após mutações, bloqueio durante carga e notificações. Há teste real de composição/SQLite além de leitores controlados; símbolos/rótulos são verificados separadamente da categoria.

Riscos para revisão: navegar pela semana não muda o dia do formulário automaticamente; criar pelo dia sim. Navegação conserva rascunho sem prompt; filtros não o salvam. Dados semanais anteriores permanecem visíveis com erro após falha de consulta. Releitura após commit deve preservar o ID salvo e indicar erro, sem tentar gravar de novo automaticamente.

Limites: ainda sem cartões renderizados, estilos, teclado ou QA ampla/compacta; a seleção dos glifos precisa dessa conferência visual posterior. Símbolo nunca substitui rótulo textual. Não declarar a visão semanal disponível no aplicativo nesta fatia.

Inspeção não encontrou segredo literal ou logging novo; fixtures fictícias, perfil SQLite temporário. Esforço moderado, sem subagentes: uma rodada focada e uma regressão completa. Reutilizar o ViewModel, comandos e projetor existentes reduz custo e evita duplicar o editor. Tokens/custo exatos não medidos.

**Regressão final:** 948 anteriores aprovados (167 unitários e 781 integrações), sem falhas/ignorados. Hashes confirmam src e testes anteriores intactos. A suíte incluindo a nova classe permanece em RED.

[Estudo](../estudos/89-integracao-semanal-especificacao.md). Evidências e diffs: trama-agenda-week-navigation-red. Revisão dos testes pendente.
