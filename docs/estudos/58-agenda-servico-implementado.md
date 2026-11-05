# 58 — Agenda: serviço integrado à composição

Data: 03/11/2026. Implementação após aprovação dos 15 casos; diff para revisão humana.

## Caminho das operações

DesktopComposition inicializa o perfil com LocalDatabase e registra IAgendaActions como SqliteAgendaActions, apontando para o mesmo trama.db usado pelos módulos existentes. A interface agrega gravação com resultado e consultas; o consumidor não recebe DbContext.

O serviço guarda somente DbContextOptions, com string de conexão. Cada SaveAsync/GetByIdAsync/ListAsync cria um contexto novo com await using. O descarte acontece ao terminar, inclusive em falha ou cancelamento. Uma entidade cuja gravação falhou não pode continuar rastreada numa operação posterior.

Salvar passa por SaveAgendaTask e pelo repositório já testados. A validação acontece antes de qualquer consulta ao banco. Criar um objeto de contexto não abre automaticamente a conexão: tarefa inválida retorna ValidationFailed sem acessar o arquivo.

## Perfil e conexão

O construtor exige caminho absoluto e não inicializa banco ou diretório. SqliteOpenMode.ReadWrite exige arquivo existente; a inicialização continua pertencendo a LocalDatabase. Consultar caminho inexistente retorna a falha normalizada pelo repositório, sem criar um arquivo vazio.

Pooling fica desativado. A configuração pode ser compartilhada no singleton porque não guarda contexto ou conexão abertos. Usar UseSqlite com a string, em vez de compartilhar uma instância de SqliteConnection, também evita compartilhar uma conexão entre chamadas simultâneas.

O token já cancelado é verificado antes da criação do contexto e segue para o caso de uso/repositório. Cancelamento continua sendo uma exceção; falhas esperadas de gravação retornam PersistenceFailed pelo caso de uso.

## Testes

Os 15 casos aprovados permanecem intactos e passaram. Verificaram construção sem efeitos, caminhos inválidos, ausência de criação silenciosa, validação, cancelamento, isolamento entre perfis, leitura por novas instâncias e registro na composição.

Um trigger fictício rejeitou uma inserção; depois de removê-lo, a gravação seguinte funcionou e a tarefa rejeitada continuou ausente. Seis gravações simultâneas de IDs distintos foram preservadas.

A suíte completa no WSL passou com 598 casos: 101 unitários e 497 integrações, nenhum ignorado. Build Release sem avisos/erros e formatação passaram. O histórico de timeout em autosave não se reproduziu nessa rodada, sem alteração do autosave nesta entrega.

## Limites e próxima etapa

O serviço está disponível na composição, mas a tela da Agenda ainda não foi construída. A navegação continua desabilitada. Não houve nova migração nem alteração do backup ou dos formatos. Não foi gerado pacote Windows novo nesta fatia sem interface.

Chamadas simultâneas para IDs distintos não equivalem a detecção de conflito entre duas edições da mesma tarefa. Esse controle não foi acrescentado.

Próxima fatia: ViewModel, formulário/lista, criação/edição/reagendamento, estados vazios, confirmação de rascunhos e integração da navegação preservando os Cadernos. Semana visual, indicadores e vínculo com páginas permanecem posteriores.

## Para estudar

- Por que opções de contexto podem ser compartilhadas, mas um contexto rastreado não?
- O que await using garante quando a operação falha?
- Por que construir um contexto não significa abrir o banco?
- Como o teste de trigger distingue falha de gravação de vazamento de estado para a próxima chamada?
