# 101 — Comparação semanal e dados ausentes

Data: 09/11/2026. 33 casos RED para revisão antes da implementação.

Esta fatia propõe comparar os fechamentos imutáveis já salvos e preparar os textos/dados do futuro painel. Não consulta tarefas atuais, não cria semanas anteriores e não altera o serviço, banco ou interface. Apenas contrato e testes foram adicionados.

## Comparação proposta

IAgendaRetrospectiveComparer.Compare recebe a captura atual validada e, opcionalmente, a da semana imediatamente anterior. Captura anterior com outra data (mesma semana, futura ou com intervalo de duas semanas) é erro de argumento, evitando comparar com o último registro disponível quando há um buraco no histórico.

AgendaRetrospectiveComparison retorna um estado de comparação, variação percentual em pontos percentuais, diferenças de planejadas/concluídas/abertas e flags sobre conclusões sem data, fora do período e continuidades em qualquer uma das duas capturas.

| Situação | Tratamento |
| --- | --- |
| Duas semanas adjacentes com percentual e mesmo TimeZoneId | Diferença dos percentuais já guardados, em p.p., e diferenças das contagens |
| Semana anterior ausente | PreviousMissing, sem deltas; nunca presumir 0% |
| Uma ou ambas sem tarefas | NoPercentageBase e sem delta percentual; diferenças de contagem continuam disponíveis |
| TimeZoneId registrado diferente | DifferentTimeZones, sem deltas diretos |
| Mesmo fuso registrado e offsets de captura diferentes | Comparação permitida; offset não é identificador de fuso |

Exemplo: de 50% para 75% significa +25 p.p., independentemente da mudança de quantidade de tarefas. Para permanecer coerente com os números exibidos e preservados, subtrair os percentuais arredondados guardados: 66,7% − 33,3% = 33,4 p.p. Não recalcular razões com mais casas e apresentar um delta que não corresponde aos valores do painel.

A regra de fuso é conservadora: IDs diferentes podem representar contextos distintos, inclusive após mudança de sistema. Não consulta mapeamentos do sistema para declarar equivalência nem afirma que o fuso físico necessariamente mudou. A interface explica “Fusos registrados diferentes”.

Conclusões sem data ou fora do período não desaparecem na comparação. As flags sinalizam qualidade/contexto dos dados, mesmo quando a comparação numérica for possível. O percentual continua sendo o de conclusões datadas dentro da semana, definido na etapa anterior; não vira uma pontuação geral de produtividade.

## Preparação da apresentação

AgendaRetrospectiveSummaryViewModel será um modelo de apresentação somente leitura, com construtor recebendo relatório e comparação. Prepara ComparisonLabel, CompletionLabel, OpenCount, CompletedCount, OpenBreakdownLabel, OpenTasks, Days, Categories e DataQualityMessages para os bindings futuros.

Textos em português usam +/− e vírgula decimal, sem depender da cultura da máquina. Sem tarefas aparece “Sem tarefas”, distinto de 0%. Ausência de base/histórico/fuso não é exibida como +0 p.p. Pendentes e Em andamento compõem abertas; concluídas sem data ou fora do período não são tratadas como pendentes.

Avisos distinguem dados ausentes atuais e apenas da semana anterior. Dias, categorias e identidades das tarefas vêm da captura, sem modificar seus dados, consultar banco, inventar datas ou calcular horas trabalhadas. O painel usará esses valores depois; esta fatia não contém renderer ou XAML.

## Verificação e próximos passos

33 falhas esperadas pela ausência do comparador e do modelo de apresentação; build Release e formatação aprovados. Resultados da base, hashes e diff no [relatório](../reviews/108-retrospective-comparison-red.md).

Após aprovação: implementar mantendo os casos intactos, conferir a suíte e revisar o diff. Depois integrar carregamento das capturas atual/anterior, política de abertura da retrospectiva, gráficos/avisos e responsividade do painel, com testes do ponto de entrada real. Sem migração, mudança de formato ou cálculo de semanas ausentes nesta entrega.
