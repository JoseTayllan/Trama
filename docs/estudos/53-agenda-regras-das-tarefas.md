# 53 — Agenda: especificação das tarefas

Data: 02/11/2026. Especificação em RED, aguardando revisão. Ainda não há tarefas funcionais no aplicativo.

**Atualização:** os 24 casos e as regras foram aprovados em 02/11. A implementação da Application e suas verificações estão no [capítulo 54](54-agenda-regras-implementadas.md). O RED abaixo permanece como histórico; banco e interface ainda não foram entregues.

## Ordem de trabalho

1. Definir e implementar as regras de gravação na Application, após aprovação dos testes deste capítulo.
2. Especificar SQLite e consultas, com migração nova, preservação dos cadernos e recuperação por backup.
3. Integrar formulário/lista ao módulo Agenda, mantendo o editor dos cadernos e protegendo rascunhos ao fechar.
4. Evoluir para a semana visual, filtros, registros do realizado e indicadores reais.
5. Acrescentar vínculos com páginas e resumos do Início.

O painel de visão geral dos Cadernos teve seu protótipo aprovado como direção. Ficará para depois da Agenda, conforme solicitado; não foi implementado nesta etapa.

## Proposta das regras iniciais

| Campo ou ação | Regra proposta |
| --- | --- |
| Identidade | GUID obrigatório; editar/reagendar usa o mesmo ID |
| Título | Obrigatório; vazio ou somente espaços é rejeitado; conteúdo válido preservado |
| Descrição | Texto preservado, sem interpretar código, comandos ou tags |
| Data | Dia local planejado, representado por DateOnly; datas passadas permitidas |
| Horário | Sem horário, ou início e fim preenchidos; fim posterior ao início no mesmo dia |
| Categoria | Pessoal, trabalho ou estudo; valores desconhecidos rejeitados |
| Estado | Pendente, em andamento ou concluída; permite reabrir |
| Conclusão | Mantém o planejamento; não inventa tempo de atividade realizada |
| Falha esperada | Retorna falha de persistência, nunca sucesso |
| Cancelamento | Respeita o token e não o transforma em sucesso |
| Gravação pendente | Aguarda o repositório antes de confirmar |

Horários que atravessam a meia-noite, categorias personalizadas, recorrência, lembretes, exclusão e registros de duração não pertencem a esta fatia. A aprovação destes casos confirma especialmente a regra de início/fim em pares. Se for necessário permitir somente o horário inicial, revisar essa regra antes da implementação.

## Contratos mínimos

`AgendaTaskSnapshot` é um registro de dados imutável. Não prova persistência. `IAgendaTaskRepository` define somente a gravação necessária a este caso de uso. Consultas e adaptador SQLite serão especificados na próxima fatia.

`SaveAgendaTask` contém apenas a guarda estrutural de dependência nula; `ExecuteAsync` lança NotImplementedException. Essa abertura permite compilar a especificação, sem implementar validação ou gravação. Não está registrado na composição nem conectado à interface.

`AgendaPersistenceException` distingue falhas esperadas do armazenamento de defeitos inesperados. Esse padrão segue os casos de uso de notas já existentes. A futura infraestrutura deve normalizar erros concretos sem expor conteúdo pessoal em logs.

## Testes

`tests/Trama.UnitTests/Agenda/SaveAgendaTaskTests.cs` possui 24 casos: título/identidade, intervalos, enums, preservação de conteúdo/categoria/estado, ausência de horário, datas passadas, reagendamento, conclusão/reabertura, falhas, espera, cancelamento e argumentos nulos.

No RED final executado, 23 falham porque ExecuteAsync ainda lança NotImplementedException; a proteção do construtor nulo passa. Não são erros de compilação. As asserções posteriores sobre escrita e transições ainda não foram alcançadas e não são evidência de recurso funcional. O repositório dos testes é controlado, sem disco ou banco pessoal.

A base unitária anterior contém 77 casos; seu resultado nesta etapa fica registrado no relatório 061. A instabilidade anterior de autosave em integração continua aberta, sem nova execução completa nesta fatia isolada.

## Próximos riscos

Antes de expor a Agenda no desktop, testar migração sobre acervo anterior e backup/restauração com tarefas. SQLite novo não deve tornar silenciosamente inválidos os backups antigos. Não reescrever as seis migrações distribuídas nem recriar o banco. A semana visual e métricas precisam de consultas reais; os dados fictícios das imagens não entram no aplicativo.

## Para estudar

- Por que DateOnly e TimeOnly representam planejamento local melhor que uma conversão automática para UTC?
- Por que concluir uma tarefa não mede quanto tempo foi trabalhado?
- O que um repositório controlado comprova e o que somente um teste SQLite pode comprovar?
- Por que cancelar uma gravação precisa ser diferente de confirmar sucesso?
