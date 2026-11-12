# 114 — Reflexão semanal: regras de gravação

Data: 10/11/2026. Proposta com 25 casos antes da implementação.

A reflexão é um registro separado do fechamento. Sua chave é a segunda-feira da semana, dentro do perfil escolhido. WorkedWell e Adjustments correspondem às duas perguntas da interface; UpdatedAt registra o instante de atualização. Alterar uma reflexão não modifica tarefas, totais, datas ou percentuais capturados.

## Regras propostas

- Só gravar se já houver um fechamento salvo para aquela semana. A operação não cria nem recalcula fechamentos.
- Os dois campos são opcionais. Nulo é convertido em string vazia; salvar ambos vazios permite limpar uma reflexão anterior.
- Preservar espaços, quebras de linha, acentos e Unicode. Não aplicar Trim nem truncar conteúdo; há um caso com mais de 20 mil caracteres.
- Salvar novamente substitui o texto daquela semana, com novo instante de atualização; outras semanas permanecem independentes. Histórico completo de versões não faz parte desta proposta.
- A chave deve ser segunda-feira. Uma chave inválida falha antes de consultar o repositório ou o relógio, em vez de normalizar silenciosamente para outra semana.
- Instante vem de TimeProvider injetado, em UTC, lido uma vez apenas após validar chave e existência do fechamento.
- Cancelamento é propagado antes de I/O, entre consulta e gravação e pelo adaptador. Falha de persistência tem resultado próprio; erros de programação não são ocultados como falhas do banco.

## Contratos mínimos

AgendaWeekReflection contém chave, dois textos e UpdatedAt. AgendaReflectionSaveResult distingue Saved, ValidationFailed, ClosureMissing e PersistenceFailed. IAgendaReflectionRepository define HasClosureAsync e SaveAsync, ambos com token. Esses tipos permitem compilar os testes; não existe implementação de SaveAgendaReflection nesta fase.

O caso de uso proposto receberá repositório e TimeProvider e exporá ExecuteAsync(weekStart, workedWell, adjustments, token). Os testes procuram a implementação por reflexão; a falha deve ser sua ausência, não erro de compilação ou infraestrutura.

## Próximas fatias

Após aprovação, implementar as regras mantendo os 25 casos intactos. Depois, especificar SQLite/migração, leitura, registro por semana/perfil e backup/restauração, com validação dos registros. A existência do fechamento precisa ser protegida também na gravação real do adaptador; esta sequência de chamadas unitárias não demonstra atomicidade nem concorrência.

Em seguida, integrar serviço e interface: carregar o texto da semana, habilitar edição/salvamento manual, mostrar falhas sem perder rascunhos e proteger navegação/fechamento. Os casos visuais antigos que exigem prévia somente leitura precisarão de revisão explícita quando o comportamento funcional for especificado. A prévia atual continua intacta até essa etapa.

[Revisão](../reviews/121-agenda-reflection-rules-red.md).
