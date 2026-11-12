# 102 — Comparação dos fechamentos semanais

Data: 09/11/2026. Implementação após aprovação dos 33 casos e contrato.

`AgendaRetrospectiveComparer`, na Application, recebe dois fechamentos já validados. Não consulta tarefas atuais. Confere a distância de sete dias pelos números dos dias, evitando subtrair uma semana da menor data representável. A comparação usa os percentuais guardados: de 33,3% para 66,7%, a diferença apresentada é +33,4 pontos percentuais.

Sem fechamento anterior, não há deltas. Com semanas vazias, ainda existem diferenças de contagem, mas não diferença percentual. Identificadores de fuso distintos impedem comparação direta; isso não afirma que os fusos físicos sejam diferentes. O comparador não consulta o mapeamento de fusos do sistema operacional.

`AgendaRetrospectiveSummaryViewModel`, no Desktop, prepara textos em português, tarefas abertas, dias, categorias e avisos. Não tem comandos nem acesso ao banco. Percentuais e variações usam explicitamente pt-BR, independentemente da cultura do processo. Pendentes e Em andamento compõem as abertas; concluídas sem data ou fora da semana são informadas separadamente.

As flags do comparador consideram as duas semanas. Quando apenas a anterior possui um problema, o resumo identifica essa origem sem inventar uma contagem. Quando ambas possuem o mesmo problema, o aviso quantificado apresenta o fechamento atual; a flag continua indicando a limitação da comparação.

O modelo pressupõe capturas validadas pelo cálculo/armazenamento existentes. Não substitui a validação de corrupção da persistência. Listas filtradas e avisos são somente leitura; dias e categorias reutilizam as listas preservadas da captura.

## Próxima etapa

Integrar leitura da semana escolhida e da imediatamente anterior, estados de carregamento/erro e o painel. A política de abertura automática e os controles precisam de testes próprios, incluindo a inicialização real do App. Esta entrega ainda não muda a tela nem registra automaticamente novos fechamentos.

[Especificação aprovada](101-retrospectiva-comparacao-especificacao.md) · [Revisão](../reviews/109-retrospective-comparison-green.md).
