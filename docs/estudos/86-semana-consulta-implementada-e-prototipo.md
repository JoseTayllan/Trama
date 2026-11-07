# 86 — Leitura por sobreposição e protótipo semanal

Data: 06/11/2026. Implementação após aprovação dos 25 casos e do contrato; diff para revisão.

IAgendaPeriodReader.ListOverlappingAsync agora é implementado pelo repositório e pelo facade SqliteAgendaActions. A composição publica a mesma instância de IAgendaActions pelo contrato adicional; ambos usam o mesmo perfil. Nenhum método anterior mudou de assinatura ou regra.

## Como a leitura funciona

O predicado seleciona tarefas ativas cujo início planejado é <= through e cujo fim efetivo é >= from. Fim efetivo é PlannedEndDate ou, quando ausente, PlannedDate. Ambos os limites são inclusivos. Assim, uma tarefa iniciada antes de segunda-feira e terminada durante a semana não desaparece. Estado pendente não estende o planejamento indefinidamente.

AsNoTracking mantém leitura independente do estado rastreado. Cada chamada do facade cria/descarta seu contexto; pré-cancelamento ocorre antes de abrir o contexto e intervalo invertido é rejeitado no repositório antes da consulta. Banco ausente não é criado, pois a conexão continua em ReadWrite. Erros de banco/materialização normalizados como AgendaPersistenceException; a validação compartilhada confere snapshots selecionados.

Retorno mantém identidade, datas, horários e estado originais, uma linha por tarefa. Ordem: dia inicial, horário (sem horário depois), título e ID. Itens na lixeira ficam de fora e reaparecem após restaurar. ListAsync continua filtrando apenas pelo dia inicial para preservar a lista diária. Nenhuma migração ou alteração do backup foi necessária.

## Verificação

25 casos aprovados e contrato intactos por hashes. Suíte completa: **912 aprovados (131 unitários e 781 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL. Todos os testes anteriores, mapeamentos, locks e migrações permaneceram intactos. Casos abrangem intervalos que cruzam limites, faixas externas, dias exatos, horários opcionais, títulos repetidos, ordenação, estados, lixeira, cancelamento, erros, perfis e composição.

```bash
dotnet build Trama.slnx -c Release --no-restore
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test Trama.slnx -c Release --no-build
```

## Protótipo visual solicitado

[Abrir imagem: agenda-semanal-v1.png](../prototipos/agenda-semanal-v1.png).

Imagem gerada com a habilidade imagegen e ferramenta nativa, salva em docs/prototipos. Representa uma proposta conceitual: sete colunas de segunda a domingo, navegação de semana, filtros de categoria/estado, cores do Trama, destaque de hoje, tarefa iniciada na semana anterior e editor lateral com HH:mm e exclusão visível. Textos e conteúdo são fictícios; a imagem está identificada como prévia conceitual.

A composição foi conferida: sete dias na ordem correta para 02–08/11/2026, sexta-feira 06 destacada, continuidade de tarefa anterior e ações visíveis. É referência para discussão visual, sem implementar os controles ou alterar recursos do aplicativo. A imagem original gerada foi preservada; a cópia no projeto é uma nova versão, sem substituir protótipos anteriores.

## Próxima etapa e limites

A consulta está pronta para revisão técnica; a visão semanal ainda não está disponível na interface. Próxima fatia: especificar sete dias, recorte/distribuição dos intervalos e filtros. Depois integrar navegação semanal, seleção/criação por dia e editor existente, conferindo acessibilidade e janela compacta. A imagem não fixa regras não testadas, como arraste ou duração precisa.

Sem dependência, migração, novo exe, execução nativa Windows, perfil pessoal, Git ou manual/PDF alterados. Aceite da lixeira permanece registrado. Esta alteração precisa de nova execução remota após o commit/push do responsável.

Para estudar: por que COALESCE do fim com o início preserva tarefas antigas? Por que o leitor retorna uma tarefa uma vez, mesmo quando ocupa vários dias? Onde devem ficar agrupamento visual e filtros? Como preservar um contrato existente ao introduzir outra leitura?

[Especificação](85-semana-consulta-por-sobreposicao.md) · [Relatório](../reviews/093-agenda-period-query-green.md).
