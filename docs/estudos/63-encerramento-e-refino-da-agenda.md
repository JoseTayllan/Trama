# 63 — Agenda: refino do formulário e especificação do encerramento

**Atualização de 04/11/2026:** os 17 casos foram aprovados e implementados, preservados. Suíte completa com 657 aprovados; ajustes visuais finais conferidos com oito casos nativos. [Entrega GREEN](64-regras-de-encerramento-implementadas.md). O texto abaixo preserva o histórico RED; SQLite e diálogo continuam pendentes.

Data: 04/11/2026. Duas situações distintas: refino visual implementado para revisão; regras de encerramento em RED, aguardando aprovação dos testes.

## Formulário refinado

`AgendaWorkspaceView.axaml` organiza os campos em três grupos: A tarefa, Quando e Contexto e progresso. Título e cabeçalho ganharam hierarquia; o planejamento tem superfície própria e explicação de início sem fim. A ação Limpar horários reutiliza o mesmo handler. Salvar permanece fora da rolagem e explica que gravar não conclui a tarefa.

Recursos semânticos dinâmicos preservam a abertura para personalização de paleta. Nomes dos controles, bindings, comandos e campos foram mantidos. O refino não troca a instância do editor nem altera o fluxo de gravação. Em janela compacta, os campos inferiores ficam na rolagem; título, Nova tarefa e Salvar permanecem acessíveis.

Conferência SQLite/Skia em perfil fictício: criação pelo botão com início sem fim, limpeza do formulário, preservação do estado, abertura para edição e janela ampla/compacta. Capturas reais foram inspecionadas. Os seletores de horário ainda mostram placeholders nativos em inglês quando vazios; substituir o template desses controles não entrou nesta fatia.

## Decisão de encerramento confirmada

O responsável aprovou preservar o planejamento e registrar data/hora reais do encerramento. Tarefas futuras ficam fora da seleção. Pendentes e Em andamento com data planejada até o dia local do encerramento podem aparecer no aviso, inclusive de dias anteriores. O usuário escolhe quais concluir.

O aviso futuro terá Encerrar tarefas e sair, Sair mantendo abertas e Cancelar. Apenas a primeira opção grava encerramento e conclusão. Horários planejados não serão sobrescritos por horas realizadas; uma tarefa de ontem pode ser encerrada hoje sem produzir intervalo planejado invertido. Sem início real registrado, o sistema não deve inferir tempo trabalhado.

## Primeira fatia: regras em RED

`CompleteAgendaTasks.cs` contém somente contratos compiláveis: repositório de conclusão em lote, resultado e entrada do caso de uso ainda não implementada. Não está registrado no aplicativo e não altera o fechamento atual. O adaptador futuro terá que revalidar e gravar atomicamente a seleção, preservando os campos de planejamento. O teste controlado não comprova transação SQLite ou durabilidade.

`CompleteAgendaTasksTests.cs` especifica 17 casos:

- Duas categorias de estado elegível: Pendente e Em andamento; um lote com IDs selecionados e o mesmo instante real, offset e token.
- Rejeição de tarefa já concluída, futura, identidade vazia e enums desconhecidos; nenhuma escrita antes de validar toda a seleção.
- Seleção vazia e IDs repetidos rejeitados.
- Elegibilidade pelo dia local, sem converter a data escolhida para UTC.
- Falha conhecida de persistência reportada; cancelamento antes/durante e erro inesperado propagados.
- Não anunciar conclusão antes de a operação de repositório terminar.
- Guardas para repositório e seleção ausentes.

Resultado: **17 descobertos, 16 falhas pela operação ainda não implementada e uma guarda de construtor passando**, zero ignorados. Nenhum erro de compilação. As verificações posteriores à chamada ainda não são alcançadas nos casos que lançam NotImplementedException.

Separadamente, os **640 casos anteriores passaram**, sendo 100 unitários e 540 integrações, com o formulário refinado. Build Release sem avisos/erros e formatação passaram. Não afirmar que a suíte completa de 657 casos está GREEN: o RED é intencional e precisa de revisão conforme CODEX seção 9.

```bash
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test tests/Trama.UnitTests -c Release --no-build --filter FullyQualifiedName~CompleteAgendaTasksTests --logger trx --results-directory artifacts/agenda-closure-red --verbosity quiet
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~CompleteAgendaTasksTests' --logger trx --results-directory artifacts/agenda-closure-baseline --verbosity quiet
```

## Próximas fatias e cuidados

Depois de aprovar estes casos: implementar as regras; especificar armazenamento do encerramento real, migração e backup; conectar seleção/diálogo e coordenação do fechamento. Revalidar tarefas alteradas entre leitura e confirmação. A biblioteca não pode perder seu rascunho se o encerramento da Agenda for cancelado ou falhar; não concluir tarefas antes de resolver as decisões que ainda podem cancelar a saída. Exclusão com recuperação e semana visual continuam pendentes.

Não houve migração, dependência, alteração de formato, uso de perfil pessoal ou comando Git nesta fatia. Nenhum teste antigo foi alterado. A mudança visual tem revisão estética e de diff pendentes; nenhum novo executável Windows foi publicado nesta etapa.

Para estudar: por que planejamento e execução precisam de dados distintos? Por que gravar cada tarefa separadamente pode deixar o lote parcialmente concluído? Em que ordem resolver as confirmações de dois módulos antes de alterar dados ao fechar?

[Relatório](../reviews/070-agenda-closure-red-e-refino.md).
