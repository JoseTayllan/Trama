# 57 — Agenda: serviço para a interface em revisão

Data: 03/11/2026. Especificação RED; operações do serviço ainda não implementadas.

**Atualização:** os 15 casos foram aprovados em 03/11. A implementação está no [capítulo 58](58-agenda-servico-implementado.md). A descrição RED abaixo permanece como histórico.

## Por que esta camada existe

O repositório SQLite recebe um contexto pertencente ao chamador. A tela precisa de um contrato simples para salvar e consultar tarefas sem controlar conexões ou guardar entidades EF. `IAgendaActions` reúne a gravação com resultado do caso de uso e as consultas já definidas por IAgendaTaskReader.

A implementação proposta é `SqliteAgendaActions`. Assim como os serviços existentes, deve guardar opções de conexão e criar/descartar o contexto em cada operação. Isso permite registrar o serviço na composição sem compartilhar um DbContext entre chamadas.

## Regras propostas

- Caminho absoluto obrigatório; construir o serviço não inicializa perfil nem cria arquivos.
- A gravação passa por SaveAgendaTask, aproveitando as regras aprovadas.
- Tarefa inválida ou token já cancelado não deve abrir/criar um banco.
- Consultar um banco inexistente retorna falha de persistência e não cria silenciosamente um arquivo vazio.
- O serviço usa somente o perfil indicado e permite novas instâncias consultarem tarefas já salvas.
- Chamadas simultâneas para tarefas distintas preservam todas as gravações.
- Uma escrita que falha não deixa entidades pendentes para a operação seguinte.
- Novas consultas enxergam alterações confirmadas por outra instância.
- DesktopComposition fornece IAgendaActions apontando para o mesmo perfil local inicializado pelos outros módulos.

O modo de conexão deve abrir banco existente, pois a inicialização e as migrações já pertencem a LocalDatabase. Não adicionar inicialização alternativa no serviço nem tratar banco ausente como acervo vazio válido.

## Testes e RED

`AgendaServiceTests.cs` contém 15 casos. Três proteções de caminho e a construção sem efeitos passam. Dez casos falham porque as operações do serviço estão vazias; um falha porque a composição ainda não fornece o contrato. Os testes compilam e usam arquivos fictícios. As verificações posteriores de escrita/isolamento ainda não foram alcançadas.

A fixture de falha cria um trigger SQLite que rejeita inserções, tenta salvar, remove o trigger e salva outra tarefa. A primeira não pode aparecer depois. Isso verifica o isolamento do estado rastreado entre operações, além do resultado de erro.

A fixture concorrente dispara seis gravações de identidades distintas. Ela não especifica detecção de conflito entre duas edições da mesma tarefa; essa limitação permanece como no capítulo 56.

Os contratos mínimos compilam, mas SaveAsync/GetByIdAsync/ListAsync ainda lançam NotImplementedException. Nenhum registro de DI ou formulário foi implementado nesta etapa. A seção 9 do CODEX exige aprovação dos casos antes do GREEN.

## Continuidade

Depois do serviço aprovado e verificado, especificar formulário/lista, estados vazios, categorias, edição, reagendamento e proteção dos rascunhos ao navegar/fechar. O botão Agenda continua desabilitado até a integração funcional. Semana visual e indicadores entram depois.

O painel geral dos Cadernos continua somente como protótipo aprovado para implementação após a Agenda.

## Para estudar

- Por que um serviço singleton pode ser seguro com opções de conexão, mas não com um DbContext compartilhado?
- Como uma entidade que falhou na gravação pode aparecer indevidamente numa operação posterior?
- Por que inicializar perfil e abrir uma conexão são responsabilidades diferentes?
- Como testar isolamento entre perfis sem acessar dados pessoais?
