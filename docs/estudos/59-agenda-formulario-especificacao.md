# 59 — Agenda: formulário, lista e rascunhos em revisão

**Atualização de 03/11/2026:** os 34 casos foram aprovados e implementados, preservados. O ajuste dos dois cronômetros antigos também recebeu aprovação explícita. Suíte final: 632 aprovados. [Entrega GREEN](60-agenda-formulario-e-navegacao.md). O texto abaixo registra a especificação RED histórica.

Data: 03/11/2026. Especificação em RED; a tela ainda não foi implementada.

## Primeira interface funcional

A proposta é uma lista por dia com formulário de tarefa, usando IAgendaActions já integrado. Campos: título, descrição, dia planejado, início/fim opcionais, categoria e estado. Ações: nova tarefa, abrir para editar, salvar, reagendar, concluir e reabrir pela mudança de estado. A visão semanal será uma entrega posterior.

Salvar mantém a tarefa no formulário com o mesmo ID. Reagendar não muda automaticamente o dia usado para filtrar a lista; a tarefa pode sair da lista daquele dia. A data escolhida no calendário deve ser preservada sem conversão para o dia UTC.

Sem data ou título válido, mostrar feedback e preservar os campos. A confirmação de gravação só ocorre após o serviço concluir. Se a escrita funcionar e a atualização da lista falhar, manter o ID já confirmado para não duplicar a tarefa na próxima gravação.

## Proteção dos rascunhos

| Ação | Comportamento proposto |
| --- | --- |
| Alternar Início, Cadernos e Agenda | Preservar os dois rascunhos e os controles do caderno, sem pedir descarte |
| Nova tarefa, outra tarefa ou outro dia | Confirmar Salvar/Descartar/Cancelar quando houver alteração |
| Consulta do novo dia falha | Manter seleção e rascunho anterior |
| Gravação pendente | Bloquear reentrada, edição visual e fechamento sem abrir confirmação extra |
| Edição posterior ao snapshot | Manter o conteúdo mais recente como pendente após gravar a versão enviada |
| Fechar com Agenda oculta | Consultar seu rascunho antes de encerrar |
| Cancelar uma decisão de fechamento posterior | Preservar os dois rascunhos e manter o caderno editável |

Autorizar descartar na preparação de fechamento não pode apagar imediatamente o rascunho, pois outro módulo ainda pode cancelar o fechamento. A coordenação também não pode descartar o editor dos Cadernos antes de saber se a janela realmente fechará. As confirmações devem deixar claro se tratam da tarefa ou da página.

## Navegação e compatibilidade

A Agenda será acessível pela barra superior e pelo cartão do Início quando seu ViewModel estiver disponível. Construções antigas de MainWindowViewModel sem Agenda devem continuar funcionando; os testes antigos que não fornecem o módulo mantêm seu significado. Não inserir métricas fictícias para ativar a navegação.

AgendaWorkspace deve ser hospedado sem substituir Workspace dos Cadernos. O autosave, seleção e histórico do editor continuam durante o uso da Agenda. Título e ações principais precisam estar acessíveis na janela mínima de 860×680, com rolagem adequada no restante do formulário/lista.

## Contratos mínimos e testes

AgendaViewModel contém apenas propriedades de contrato e métodos vazios para compilar os testes. MainWindowViewModel recebe uma propriedade Agenda ainda sem coordenação funcional. App, composição e XAML de navegação não foram alterados para habilitar a tela.

AgendaFormTests: 26 casos com serviço controlado, cobrindo estado inicial, criação/edição, datas locais, reagendamento, estado, validação, falhas, espera, identidade e confirmação de rascunhos. O serviço controlado reutiliza SaveAgendaTask para validar as regras existentes.

AgendaNavigationTests: oito casos com controles Avalonia/headless e SQLite fictício. Cobrem criação pela interface, lista vinculada, alternância com histórico, Agenda oculta ao fechar, dois rascunhos, autosave e largura compacta. Entrada e fechamento são simulados; isso não equivale a validação interativa no Windows.

No RED, 32 casos falham e duas guardas de dependência nula passam. 31 falhas são de inicialização ainda vazia; um caso constata a Agenda indisponível no host. As asserções posteriores só serão exercidas no GREEN, sem alegar formulário funcional neste momento.

## Verificação e continuidade

O relatório 067 registra as verificações da base e a pendência histórica de autosave. Os testes anteriores não foram alterados. Após aprovação dos casos conforme CODEX, implementar e conferir visualmente a tela ampla/compacta, além do ciclo real de tarefas com perfil fictício.

Semana visual, filtros avançados, duração realizada, vínculos com páginas e painel geral dos Cadernos continuam posteriores. Nenhuma nova migração é necessária para estes campos já persistidos.

## Para estudar

- Por que manter rascunho e lista como estados distintos?
- Por que uma gravação confirmada pode ser seguida de falha na atualização da tela?
- Por que descartar em uma confirmação de fechamento precisa aguardar a decisão dos outros módulos?
- Como preservar o dia escolhido sem convertê-lo inadvertidamente para UTC?
