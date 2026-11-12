# 108 — Painel da retrospectiva implementado

Data: 09/11/2026. Implementação após aprovação dos 28 casos.

A Agenda oferece Dia, Semana e Retrospectiva. O novo modo usa a semana encerrada já em consulta ou a última semana encerrada pelo relógio local. O cabeçalho identifica o período e o instante da captura preservada. Anterior/Próxima ficam limitados às datas representáveis e às semanas encerradas; Atualizar recupera o mesmo fechamento, sem recalcular tarefas atuais.

## Dados e leitura

Os quatro indicadores usam o relatório e o resumo existentes. O gráfico mostra inícios planejados e conclusões reais nos sete dias, com escala comum baseada no maior valor das duas séries. Valores vazios usam máximo visual mínimo de um, evitando divisões/intervalos inválidos sem inventar contagens.

As categorias mostram concluídas/planejadas; as pendências usam títulos capturados com símbolos e rótulos de estado. Avisos explicam conclusões sem data, fora do período e tarefas iniciadas antes da semana. O percentual continua restrito às conclusões datadas dentro da semana; não mede horas nem produtividade subjetiva.

O componente AgendaRetrospectivePanelView contém o painel e um conversor de apresentação para dias, legendas e categorias. AgendaViewModel fornece propriedades derivadas de modo, navegação, metadados e escala. Ações do componente passam pelos wrappers da Agenda, preservando IsBusy, navegação e fechamento coordenado.

## Estado e responsividade

Carregamento e erro ficam visíveis mesmo antes da primeira abertura bem-sucedida. Falha ao navegar preserva o relatório anterior com aviso; ausência real de histórico e semana vazia têm mensagens distintas. A lista/formulário existentes são ocultados pelo contêiner; a instância das entradas e os rascunhos continuam no mesmo lugar.

Indicadores passam de quatro para duas colunas em largura menor. Dias e categorias ficam lado a lado na janela ampla e empilhados na compacta; uma rolagem vertical alcança as pendências. Cores usam recursos semânticos dinâmicos, e as ações têm foco e nomes acessíveis.

## Verificação e uso

Os testes protegem valores, modos, erros, carregamento, limites, formulário, recursos dinâmicos e larguras 760/980/1600 no componente. Capturas SQLite/Skia usam a MainWindow com perfil fictício isolado, fechamento gravado pelo serviço real e larguras 1680/860. A inspeção visual levou a reduzir a largura das barras; a versão final foi verificada novamente.

Para validar no uso, salve e feche a instância antiga e execute o comando de desenvolvimento habitual. Abra Agenda → Retrospectiva; consulte uma semana encerrada e confira o retorno a Dia/Semana com um rascunho. A abertura registra o primeiro fechamento daquela semana, que permanece preservado após mudanças nas tarefas. Uma instalação sem tarefas naquela semana exibirá o estado vazio.

O aceite estético e a execução nativa Windows desta entrega continuam pendentes. Reflexão gravável e abertura automática não fazem parte desta etapa. Manual/PDF e publicação permanecem sob solicitação do responsável.

[Especificação aprovada](107-retrospectiva-tela-especificacao.md) · [Revisão](../reviews/115-retrospective-ui-green.md).
