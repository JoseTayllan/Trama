# 107 — Painel visual da retrospectiva

Data: 09/11/2026. Proposta com 28 casos antes da implementação da tela.

A referência é docs/prototipos/retrospectiva-semanal-v1.png. A estrutura central mantém quatro indicadores, Planejado × concluído por dia, categorias e pendências. Os números vêm do fechamento preservado, não das tarefas atuais ou de exemplos do protótipo. A reflexão pessoal exige gravação própria e permanece para uma etapa posterior; não apresentar um botão de salvar sem funcionalidade.

## Acesso e navegação

Dia, Semana e Retrospectiva serão modos visíveis da Agenda. Ao acessar pela visão diária ou por uma semana ainda aberta, a retrospectiva abre a última semana encerrada pelo relógio local. Ao acessar de uma semana encerrada já em consulta, usa esse período. A ação passa por ShowRetrospectiveAsync da Agenda; atualizar usa RefreshRetrospectiveAsync, mantendo a proteção compartilhada de operações e fechamento.

O cabeçalho mostra o intervalo capturado, data de gravação e a informação de fechamento preservado. Anterior/Próxima navegam por sete dias, sem ultrapassar a menor data representável ou entrar em semana ainda aberta. A consulta anterior usada para comparação não cria captura; navegar explicitamente até outro período usa o serviço existente de obter/registrar fechamento.

## Conteúdo

| Seção | Dados e comportamento |
| --- | --- |
| Indicadores | Planejadas, concluídas datadas na semana, percentual e abertas; detalhe de pendentes/em andamento |
| Comparação | Variação em p.p. do resumo existente ou explicação da comparação indisponível |
| Dias | Sete dias ordenados, duas séries e legenda textual; valores iguais aos da captura |
| Categorias | Pessoal/Trabalho/Estudo com contagens concluídas/planejadas |
| Pendências | Títulos da captura e símbolos/rótulos distintos de estado; não mover nem concluir automaticamente |
| Qualidade | Avisos do resumo para conclusões sem data, fora do período e tarefas iniciadas antes da semana |
| Vazio | Sem tarefas, zero contagens e barras válidas; não inventar percentual |

As séries diárias são barras de ProgressBar com classes semânticas e dados de AgendaRetrospectiveDay. Suas escalas devem permitir todos os valores capturados, inclusive conclusões num dia sem início planejado. As contagens diárias não necessariamente somam todas as planejadas: tarefas iniciadas antes da semana aparecem no total e no aviso de continuidade. Não apresentar horas realizadas, pois não existe medição para isso.

## Estado e layout

Durante a consulta, mostrar carregamento e desabilitar mudança de modo; em falha, mostrar feedback genérico sem caminhos internos. Primeiro erro permite nova tentativa pelo botão de modo; falha de navegação mantém dados/cabeçalho anteriores com aviso. Os dois cenários são distintos de semana vazia ou histórico ausente.

Ocultar lista, filtros e formulário da visão diária/semanal enquanto a retrospectiva estiver ativa. Preservar os controles em vez de recriar a entrada de texto; ao voltar, rascunho e instância do formulário devem continuar os mesmos. Apenas o modo correto recebe o destaque ativo.

O painel usa rolagem vertical e adapta as seções a 760, 980 e 1600 px no componente, sem exigir rolagem horizontal. A última seção deve ser alcançável ao rolar. A janela principal mantém seu limite de largura já existente. Botões devem ter foco por teclado e nomes acessíveis; cores vêm dos recursos semânticos para permitir customização futura.

## Verificação

Os novos testes Avalonia usam capturas fictícias válidas do builder, relógio fixo, serviço controlado e o AgendaWorkspaceView real. SQLite prepara apenas o formulário existente em diretório temporário. Um gate testa o carregamento sem sleeps; os limites de espera usam Stopwatch. O recurso de paleta alterado pelo teste é restaurado em finally.

A suíte de inicialização real do App já protege a conexão do carregador; esta fatia verifica a renderização e as ações no componente visual. Após aprovação e GREEN, ainda será necessário conferir capturas ampla/compacta e obter aceite de uso/estético do responsável. Nenhum teste de layout prova fidelidade visual completa ao protótipo.

[Revisão](../reviews/114-retrospective-ui-red.md) · [Integração implementada](106-retrospectiva-integrada-a-agenda.md).
