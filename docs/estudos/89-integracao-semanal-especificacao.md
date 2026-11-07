# 89 — Integração semanal e apresentação dos estados

Data: 06/11/2026. Especificação RED para revisão; implementação ainda ausente.

Esta fatia conecta o leitor por período e o projetor ao AgendaViewModel existente. A proposta mantém os construtores anteriores e acrescenta uma sobrecarga com IAgendaPeriodReader e IAgendaWeekProjector depois dos quatro parâmetros atuais. A composição real habilitará a semana no mesmo perfil SQLite. Nesta etapa somente AgendaWeekNavigationTests.cs foi adicionado; nenhum código do produto mudou.

## Contrato proposto para o ViewModel

| Membro | Papel |
| --- | --- |
| HasWeekView / IsWeekView | Disponibilidade e modo visível; início continua diário |
| Week / HasWeekError | Último resultado semanal e falha independente da lista diária |
| WeekCategoryFilter / WeekStatusFilter | Filtros opcionais ativos |
| ShowWeekAsync(referenceDate) | Consultar semana por sobreposição, projetar e só então substituir o resultado |
| ShowDayAsync() | Retornar à lista diária sem apagar rascunho, filtros ou dia selecionado |
| SetWeekFiltersAsync(category, status) | Reprojetar dados em memória, sem consulta ou gravação |
| CreateTaskOnDayAsync(day) | Preparar tarefa em um dia da semana visível, resolvendo rascunho antes de trocar |

Os quatro métodos assíncronos retornam bool para indicar sucesso. Valores inválidos de filtro ou semana fora do intervalo representável não substituem estado válido; semana inválida é rejeitada antes de consultar o leitor. Falhas são apresentadas sem deixar a agenda ocupada.

## Navegação e segurança

Navegar entre semanas ou filtrar preserva o rascunho e o dia selecionado; não pergunta nem salva automaticamente. A seleção de um dia para criar resolve Salvar/Descartar/Cancelar. A decisão Descartar não elimina o rascunho antes de uma consulta bem-sucedida. Dia fora da semana visível é rejeitado sem prompt.

Cartões de continuação abrem a tarefa pelo ID original, preservando seu planejamento. Salvar, excluir, restaurar e Atualizar recarregam a semana ativa com os filtros preservados. Falha na releitura após commit não desfaz a gravação nem cria outra identidade na tentativa seguinte. Rascunhos independentes sobrevivem à exclusão/restauração.

Carregamento semanal participa de IsBusy da Agenda existente: impede reentrada, gravação, exclusão, troca de dia, criação e fechamento enquanto a consulta está em andamento. Bindings recebem notificações da semana, modo e filtros.

## Estado e categoria

AgendaPresentationConverter receberá status-icon: Pendente ○, Em andamento ◐ e Concluída ✓. O rótulo textual continua explícito; a categoria mantém sua própria representação. Os testes verificam símbolos/rótulos, não contraste, tamanho ou renderização dos glifos. A tela semanal, estilos, navegação por teclado e layout amplo/compacto terão a próxima especificação visual.

## Evidências e próximos passos

34 casos: 33 falhas esperadas por sobrecarga/propriedades/operações ausentes e símbolos ainda vazios; uma proteção do construtor diário passa. A verificação separada dos 948 anteriores e os hashes ficam no [relatório](../reviews/096-agenda-week-navigation-red.md). Não há nova migração, dependência, contrato Application ou alteração dos testes antigos.

Depois da aprovação: implementar sem alterar os casos, executar a suíte e revisar o diff. Depois integrar os controles visuais ao protótipo, com diferenciação de estado além da cor de categoria.

Para estudar: por que só substituir estado depois de uma consulta bem-sucedida? Por que erro de atualização depois de commit não significa erro de gravação? Como um busy compartilhado protege o fechamento da aplicação?
