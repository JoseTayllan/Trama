> Histórico RED: os 25 casos e contrato foram aprovados e implementados; suíte de 912 aprovados. Ver [entrega para revisão](../reviews/093-agenda-period-query-green.md). A tela semanal continua pendente.

# 85 — Visão semanal: consulta por sobreposição

Data: 06/11/2026. Especificação em revisão; consulta ainda não implementada.

O responsável validou no uso a lixeira da Agenda e o acesso à exclusão. Ambiente não informado; revisão formal do diff continua separada. A próxima etapa é a visão semanal. Proposta inicial: segunda a domingo, cores por categoria, filtros por categoria/estado e tarefas apresentadas pelas datas planejadas. Modelo de sete dias e interface serão especificados nas fatias seguintes.

## Por que começar pela consulta

IAgendaTaskReader.ListAsync filtra pelo início planejado, como já aprovado para a lista diária. Isso não traz uma tarefa iniciada antes da semana e cujo fim planejado ainda cai dentro dela. Alterar esse método quebraria o comportamento existente. A proposta adiciona IAgendaPeriodReader.ListOverlappingAsync, mantendo os consumidores atuais.

O único código de produto criado nesta fatia é a interface mínima para compilar os testes. Não existe SQL, implementação no facade ou registro na composição novos. O contrato retorna snapshots originais, uma vez por identidade, sem projetar a mesma tarefa em várias colunas; essa distribuição pertence ao modelo semanal futuro.

## Regras propostas

Consultar intervalo inclusivo de datas: PlannedDate <= through e (PlannedEndDate ?? PlannedDate) >= from. Incluir início/fim exatamente nos limites, intervalos que atravessam a semana e todos os estados ativos. Ocultar itens na lixeira e incluí-los novamente após restauração. Sem data final explícita, o planejamento permanece no dia inicial, mesmo que a tarefa esteja pendente ou sem horário final. Não inferir novo planejamento a partir do estado.

Não inventar horas, duplicar IDs, alterar datas ou estado. Ordenação mantém dia inicial, hora (sem horário depois), título e ID. Preservar o filtro atual de ListAsync pelo dia inicial. Intervalo invertido gera erro de argumento, pré-cancelamento evita I/O e banco ausente não cria perfil vazio. Leitura de dados selecionados inválidos deve resultar em AgendaPersistenceException. Serviço registrado usa o mesmo perfil das ações existentes.

Consulta é por datas; não calcula duração nem delimita blocos de horas. Filtros de categoria/estado e semana segunda–domingo ainda são propostas para a próxima camada, não comportamento implementado por este leitor. Não há nova migração: datas e lixeira já existem.

## Evidências

AgendaPeriodQueryTests contém **25 casos: 24 falhas esperadas e uma proteção passando**. O facade ainda não oferece IAgendaPeriodReader e a composição ainda não o registra. São falhas de asserção pelo serviço ausente; compilação e runner funcionam. A guarda prova que a consulta diária continua ignorando tarefas iniciadas antes do intervalo.

Base anterior reexecutada: **887 aprovados (131 unitários e 756 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram no WSL. Arquivos anteriores de src/tests intactos por hashes; nenhuma implementação ou teste antigo alterado. Casos cobrem interseções/limites/exclusões, horas opcionais, títulos repetidos, ordem, estados, lixeira, validação/cancelamento, dados corrompidos, perfis e composição.

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --no-build --filter FullyQualifiedName~AgendaPeriodQueryTests
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaPeriodQueryTests'
```

Após aprovação: implementar leitura no repositório/facade e registrar o contrato, preservar casos e verificar suíte. Depois: especificar agrupamento em sete dias e filtros; conectar navegação semanal, seleção/criação por dia e editor existente; conferir janela ampla/compacta. Sem Git, banco pessoal, dependências, publicação Windows ou manual/PDF alterados.

Para estudar: por que início dentro do período e sobreposição são consultas diferentes? Por que manter o contrato diário? Por que ausência de fim não significa planejamento infinito? Onde uma única tarefa deve virar cartões de vários dias?

[Relatório](../reviews/092-agenda-period-query-red.md) · [Lixeira implementada](84-lixeira-agenda-interface-implementada.md).
