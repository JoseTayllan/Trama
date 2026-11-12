# 111 — Pendências e reflexão: composição visual

Data: 09/11/2026. Proposta com nove casos para revisar antes da implementação.

A solicitação atual trata principalmente da aparência da parte inferior do painel. A referência dispõe Pendências para revisar à esquerda, em coluna maior, e Sua retrospectiva à direita, com alinhamento superior. Na janela compacta, os cartões devem ser empilhados e alcançáveis pela mesma rolagem vertical.

O cartão de reflexão apresentará as duas perguntas da referência: O que funcionou bem? e O que você quer ajustar?, com campos, ícone e ação visual de salvar. Como o armazenamento da reflexão não existe nesta etapa, os campos serão somente leitura, a ação ficará desabilitada e uma indicação explícita de prévia informará que o salvamento ainda não está disponível. Assim, o usuário pode avaliar a composição sem escrever conteúdo que seria perdido.

A lista de pendências mantém todas as tarefas capturadas e seus símbolos/rótulos. O ajuste visual não deve alterar dados, concluir, reagendar ou gravar tarefas. A instância e o rascunho do formulário da Agenda continuam preservados.

Os testes usam a AgendaWorkspaceView real e serviços SQLite em perfil fictício. Verificam disposição ampla, empilhamento em 760/980 px, perguntas/campos, transparência da prévia, nomes acessíveis, preservação das pendências e alcance por rolagem. Escolhem uma semana encerrada relativa ao relógio da composição. Nenhuma implementação foi antecipada.

A gravação por semana, edição posterior, backup e revisão funcional das tarefas precisam de especificação própria depois da avaliação visual. A aprovação destes nove casos cobre a composição e a prévia, sem autorizar um salvamento aparente.

[Revisão](../reviews/118-retrospective-bottom-red.md).
