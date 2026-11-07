# 91 — Tela semanal: especificação

Data: 07/11/2026. Testes para revisão antes da implementação visual.

A consulta, projeção e integração semanal estão implementadas. Esta fatia propõe tornar esses comportamentos acessíveis na AgendaWorkspaceView, preservando a instância do formulário atual. Somente AgendaWeekUiTests.cs foi adicionado; produto e testes anteriores permanecem intactos.

## Composição proposta

Na janela ampla, sete colunas de segunda a domingo e formulário compartilhado ao lado. Na compacta, dias empilhados e formulário acessível por rolagem vertical, sem apertar os cartões ou exigir rolagem horizontal. Redimensionar não recria campos, perde filtros ou troca a tarefa selecionada.

Alternância Dia/Semana, período visível, semana anterior/próxima, Hoje e Atualizar. Hoje usa o relógio local injetado, incluindo o marcador do dia atual. A primeira semana representável desabilita anterior; a última semana completa desabilita próxima. Modo diário e horários digitáveis HH:mm permanecem disponíveis.

Filtros: todas as categorias/Pessoal/Trabalho/Estudo; todos os estados/Pendente/Em andamento/Concluída. Categoria e estado são combinados e não alteram o rascunho. Sete dias e ações de criar permanecem visíveis quando não há resultados.

Cada dia permite criar tarefa com o fluxo existente de Salvar/Descartar/Cancelar. Cartões abrem a identidade original, inclusive em continuações. Salvar atualiza os cartões e prepara formulário limpo. Excluir tarefa e Lixeira continuam visíveis e reconhecíveis.

## Estados e continuidade

Cada cartão combina categoria, símbolo e rótulo de estado: ○ Pendente, ◐ Em andamento, ✓ Concluída. A faixa da categoria usa recursos dinâmicos da paleta, independentes do estado. O texto é necessário mesmo quando o símbolo ou a cor não são percebidos. Ações/cartões e filtros têm nomes acessíveis; ações por dia recebem foco de teclado.

Continuações indicam que a tarefa atravessa dias. Horários só aparecem no cartão da data de início/fim planejadas; dias intermediários não repetem o intervalo completo. A contagem de tarefas é única, sem somar os cartões repetidos.

Carregamento tem mensagem própria e desabilita navegação, filtros e formulário. Falha mantém os cartões anteriores com erro, sem mensagem enganosa de vazio ou detalhes técnicos. Atualizar recupera a consulta.

## Como os testes verificam

32 casos nativos Avalonia em perfis SQLite temporários: cliques, seletores, diálogo real de rascunho, preservação da mesma TextBox, conteúdo dos cartões, recurso de paleta substituído, geometria compacta e nomes acessíveis. Relógio controlado em 07/11/2026; leitor controlado apenas para falha/espera, delegando a consulta ao SQLite real.

Os nomes AgendaWeekModeButton, AgendaDayModeButton, AgendaPreviousWeekButton, AgendaNextWeekButton, AgendaTodayButton e AgendaWeekRefreshButton identificam as ações. AgendaWeekCategoryFilter/AgendaWeekStatusFilter identificam os seletores; AgendaWeekHeading/Count/Days e mensagens de vazio/erro/carregando identificam o conteúdo. Templates usam classes agenda-week-day/card/create/status/status-icon. São pontos de acesso dos testes, sem nova API de domínio.

Os testes não substituem inspeção visual por capturas: hierarquia, espaçamento, contraste, popup dos filtros e qualidade dos glifos precisam de QA ampla/compacta após implementar. Sem arraste, recorrência ou novas regras de datas nesta fatia.

Resultados e evidências no [relatório](../reviews/098-agenda-week-ui-red.md). Após aprovação, implementar preservando os casos e conferir a suíte completa antes de apresentar o diff e as capturas.
