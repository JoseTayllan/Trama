# Retomada de página e conclusão automática

Quando a página aberta aparece em Páginas recentes, a própria linha mostra o estado do editor e retoma a sessão. O botão Continuar página fica no cartão Cadernos e retoma a sessão aberta. O cartão Páginas recentes exibe a página atual fora da lista apenas quando necessário.

Ao salvar uma tarefa como Concluída no formulário, o serviço da Agenda grava a data e a hora locais no campo de conclusão real já existente, na mesma transação da tarefa. A data e o horário planejados para o término ficam em campos próprios. Editar uma tarefa já concluída ou reabri-la preserva o instante anterior. Concluir novamente registra o novo instante. O encerramento manual mantém a opção de informar data e hora. Horas realizadas seguem sem registro de duração.

Build Release, formatação e 1.353 testes passaram no WSL (224 unitários e 1.129 de integração). Os três testes aprovados ficaram intactos. A conferência manual e a revisão do diff estão pendentes.
