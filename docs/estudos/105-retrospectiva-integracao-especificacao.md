# 105 — Retrospectiva integrada à Agenda e ao App

Data: 09/11/2026. Proposta com 22 casos antes da implementação.

O modelo de carregamento está implementado, mas ainda não é consumido pela Agenda. A proposta adiciona uma sobrecarga de AgendaViewModel com as seis dependências semanais existentes mais AgendaRetrospectiveViewModel. Os construtores anteriores continuam disponíveis e mantêm seus recursos atuais.

## Contrato da apresentação proposto

`Retrospective` expõe o carregador, `HasRetrospectiveView` indica disponibilidade e `IsRetrospectiveView` indica o modo atual. `ShowRetrospectiveAsync(DateOnly, CancellationToken)` abre uma semana encerrada e entra no modo somente após sucesso. `RefreshRetrospectiveAsync(CancellationToken)` atualiza a última semana carregada pelo mesmo fluxo protegido.

Os modos Dia, Semana e Retrospectiva são exclusivos. Voltar para Dia ou abrir Semana preserva o fechamento já carregado. Entrar na retrospectiva não troca o dia selecionado, não apaga a lista diária nem descarta a semana em cache. Falhar ao abrir mantém o modo anterior; falhar ao navegar dentro da retrospectiva mantém seu último resultado visível, com feedback de erro.

Abrir o fechamento não é salvar nem descartar um rascunho de tarefa. O texto, inclusive horário ainda incompleto, continua no formulário. Alternar de módulo também mantém a instância do editor de Cadernos e seus dados. Testes com SQLite verificam que o rascunho da Agenda não é gravado por essa navegação.

## Operações e saída

O IsBusy da Agenda cobre a operação completa do carregador, para reaproveitar a proteção atual de navegação e fechamento. Durante a consulta, Dia/Semana/Nova tarefa/Salvar ficam bloqueados, e o coordenador de saída deve pedir para aguardar antes de consultar tarefas abertas ou exibir confirmações. O token do chamador é propagado; cancelamento não entra no novo modo e libera a Agenda.

A UI deverá chamar os métodos protegidos da Agenda para abertura/atualização. Não ligar botões diretamente a Loader.OpenAsync e contornar o IsBusy do módulo. O carregador continua sendo um componente independente; esta integração é a fronteira que coordena suas operações com os editores.

## Composição e inicialização

DesktopComposition registra comparador e carregador e fornece este último ao AgendaViewModel. O caminho manual de App.OnFrameworkInitializationCompleted também deve recebê-lo com o mesmo perfil escolhido e o prompt da janela. Dois testes abrem MainWindow via App/Opened, verificam disponibilidade sem criação automática e registram um fechamento no perfil temporário escolhido, preservando os dois rascunhos.

Inicializar o Trama não cria capturas nem abre retrospectiva automaticamente nesta fatia. O acesso será explícito quando os controles forem implementados. A política de abertura automática permanece pendente de especificação própria; não deve congelar semanas silenciosamente durante o bootstrap.

## Limites

Esta etapa conecta os modelos e o caminho real de inicialização, sem novo XAML ou gráfico. O painel visual terá testes de ações, bindings, estados, acessibilidade e layout amplo/compacto na próxima fatia. O aceite visual não é demonstrado pelos testes de estado.

Não há migração, mudança no pacote de backup ou alteração de regra dos fechamentos. Uma captura registrada permanece independente de edições posteriores em tarefas; um caso exercita essa sequência pelo fluxo da Agenda com armazenamento real.

[Revisão](../reviews/112-retrospective-host-red.md) · [Carregamento implementado](104-retrospectiva-carregamento-implementado.md).
