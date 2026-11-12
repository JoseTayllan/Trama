# 115 — Regras de gravação da reflexão implementadas

Data: 10/11/2026. Implementação após aprovação dos 25 casos e contratos.

SaveAgendaReflection segue o padrão de caso de uso da Application. Recebe IAgendaReflectionRepository e TimeProvider, valida a chave e consulta a existência do fechamento antes de montar o registro. Não conhece tarefas, métricas ou detalhes do banco.

Uma chave que não seja segunda-feira retorna ValidationFailed. Sem fechamento existente, retorna ClosureMissing sem ler o relógio ou gravar. Em uma entrada válida, campos nulos viram strings vazias e o instante vem de uma única leitura do relógio, convertido para UTC. Texto, espaços e quebras de linha são mantidos integralmente.

Salvar novamente representa uma atualização da mesma chave, permitindo também limpar os dois textos. Essa intenção é exercitada pelo repositório em memória; unicidade por semana/perfil e atualização real precisam ser verificadas no adaptador SQLite da próxima etapa.

O token é conferido antes de I/O, depois da consulta e antes da escrita, além de seguir para ambas as operações. AgendaPersistenceException é traduzida para PersistenceFailed; cancelamento e erros inesperados são propagados. O método não consulta cancelamento novamente depois de uma gravação concluída, evitando apresentar um commit confirmado como operação cancelada.

## Limites desta entrega

Esta implementação não grava no banco nem habilita os campos do painel. O repositório real, leitura, migração, serviço de composição e backup/restauração terão especificação própria. A interface continuará identificada como prévia até serem implementados o salvamento e a proteção do rascunho.

A checagem de fechamento e a gravação são duas chamadas do contrato. Não afirmar que a implementação unitária oferece transação ou proteção contra concorrência: o adaptador deve revalidar a referência e garantir a atualização por chave no banco. A reflexão fica separada da captura histórica, que não será recalculada ao editar os textos.

[Especificação aprovada](114-reflexao-semanal-regras-especificacao.md) · [Revisão](../reviews/122-agenda-reflection-rules-green.md).
