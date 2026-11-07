# Tela semanal — implementação para revisão

Data: 07/11/2026. Após aprovação explícita dos 32 casos, preservados sem alterações.

AgendaWorkspaceView adiciona modos, navegação, Hoje, filtros, estados de carga/erro/vazio e cartões por dia. Formulário existente preservado e exclusão visível nos dois modos. AgendaViewModel.Week recebe apenas propriedades de apresentação, escolhas/índices de filtros, relógio local e limites da navegação; aplicação/SQLite permanecem intactos.

Novo AgendaWeekPanel organiza visualmente sete colunas ou dias empilhados. Code-behind encaminha ações existentes e ajusta layout/foco, sem consultar banco ou alterar regras de planejamento. Conversor prepara textos de continuidade, horários por dia e nomes acessíveis. Paleta dinâmica existente, sem cores literais novas no produto.

Revisão: intervalo inválido não permite navegar além de semanas completas; filtros sincronizam com o resultado aceito; marcador Hoje usa relógio substituível. Reorganização preserva a instância dos campos. Carga desabilita ações; erro não oculta cartões anteriores nem se apresenta como vazio. Hooks no ViewModel são removidos ao desanexar a View.

QA SQLite/Skia em perfil fictício: sete dias, categorias/estados legíveis, continuidade sem horas inventadas, formulário compartilhado, filtros e retorno diário. Capturas finais ampla/compacta conferidas. A janela principal suporta mínimo de 860 px; o componente passa a verificação isolada em 760 px. Um ajuste de altura do formulário após a primeira captura motivou repetir os testes e a suíte completa. A assinatura do evento Loaded foi corrigida durante a compilação inicial; não houve alteração dos testes para contornar falhas.

Riscos e limites: dias empilhados exigem rolagem vertical na compacta; navegar pela semana conserva o dia do formulário. Dados anteriores continuam visíveis com erro quando a consulta falha. Hoje é atualizado ao carregar a tela/semana, sem temporizador de virada de dia. Glifos e teclado foram conferidos no harness/headless, sem certificação de leitores de tela ou validação nativa Windows nesta entrega. Aceite de uso/estético e revisão humana do diff pendentes.

Inspeção não encontrou segredo literal ou logging novo. Sem migração, dependência, perfil pessoal, Git, nova publicação ou manual/PDF. Esforço moderado, sem subagentes: testes focados, suíte completa e harness visual; repetição limitada ao ajuste de layout observado. Reutilizar formulário, paleta e serviços reduziu retrabalho. Tokens/custo exatos não medidos.

**Verificação final:** 32 casos aprovados intactos por hashes. Suíte completa com 1.014 aprovados (167 unitários e 847 integrações), zero falhas/ignorados; build Release sem avisos/erros e formatação aprovados no WSL. Todos os testes anteriores, aplicação, persistência, migrações e locks intactos. Capturas finais em qa-verified; ajuste de foco traz o formulário inteiro à vista ao abrir/criar na compacta.

[Estudo](../estudos/92-tela-semanal-implementada.md) · [RED](098-agenda-week-ui-red.md). Evidências/diff: trama-agenda-week-ui-green.
