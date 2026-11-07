# 92 — Tela semanal implementada

Data: 07/11/2026. Implementação após aprovação dos 32 casos; testes preservados. Diff para revisão.

AgendaWorkspaceView agora alterna Dia/Semana usando o mesmo AgendaViewModel e a mesma instância dos campos do formulário. A semana mostra o período, sete dias ordenados, contagem única, navegação anterior/próxima, Hoje, Atualizar e filtros de categoria/estado.

## Cartões e estados

Cada cartão abre a tarefa original, inclusive quando o planejamento começou em outra semana. Mostra título, categoria, horários pertinentes ao dia, continuidade e estado: ○ Pendente, ◐ Em andamento, ✓ Concluída. Símbolo e rótulo tornam o estado reconhecível sem depender da cor. A faixa da categoria usa os brushes dinâmicos existentes, preservando futuras paletas personalizadas.

As ações + Tarefa por dia reutilizam a resolução de rascunhos existente. Navegar/filtrar não apaga o conteúdo. Salvar atualiza os cartões e prepara outro formulário. Excluir tarefa permanece ao lado dos modos de visualização, com Lixeira da Agenda no cabeçalho.

## Responsividade sem recriar o editor

AgendaWeekPanel é um Panel visual que mede/posiciona os dias: sete colunas quando a largura disponível comporta os cartões; uma coluna na compacta. Não altera tarefas ou planejamento. AgendaWorkspaceView reorganiza somente as linhas/colunas do layout e limita a altura do formulário à área disponível quando empilhado. Os campos rolam internamente e o rodapé Salvar permanece acessível. A semana usa rolagem vertical; o modo diário conserva sua estrutura anterior.

Mudar largura ou modo não cria outro editor. Handlers de code-behind tratam cliques, foco, seleção dos filtros, destaque visual de Hoje e organização do layout. Consulta, filtros e preservação de rascunhos continuam no ViewModel e aplicação. Inscrições no ViewModel são removidas ao sair da árvore visual.

## Relógio, erros e acessibilidade

Hoje e seu marcador usam o relógio local injetado, não um dia UTC presumido. A navegação desabilita o passo que sairia de semanas completas representáveis. Carregamento bloqueia ações e apresenta mensagem própria; erro mantém o resultado anterior e oferece Atualizar, sem mensagem de vazio enganosa ou detalhes técnicos.

Cartões, filtros e ações de criar têm nomes acessíveis; ações por dia são focáveis. Horários HH:mm permanecem digitáveis. Os testes headless verificam essas propriedades, não certificam leitores de tela externos ou todos os dispositivos de entrada.

## Verificação e limites

Os 32 casos aprovados passaram intactos. Verificações finais de build, formatação, suíte completa e hashes estão no [relatório](../reviews/099-agenda-week-ui-green.md). Harness SQLite/Skia com perfil fictício renderizou semana ampla, compacta, formulário compacto, filtros e retorno diário. A janela principal tem mínimo de 860 px; o componente foi testado isoladamente em 760 px. Captura experimental abaixo do mínimo não representa uma largura suportada pela janela principal.

Sem migração, dependência, alteração dos testes antigos ou dados pessoais. Não houve nova publicação Windows, execução nativa Windows, Git ou edição de manual/PDF. O aceite visual e de uso do responsável e a revisão do diff continuam pendentes. Arraste, recorrências e indicadores de realizado ficam fora desta fatia.

Para estudar: por que um Panel pode mudar posições sem mudar os dados? Por que manter o editor montado ao redimensionar? Por que símbolo, texto e categoria precisam ser independentes?

[Especificação](91-tela-semanal-especificacao.md) · [Relatório](../reviews/099-agenda-week-ui-green.md).
