# 69 — Serviço de encerramento e candidatas implementados

Data: 04/11/2026. Para revisão do diff; o aviso ao sair ainda não está integrado.

## Responsabilidades

`SqliteAgendaCompletionActions` implementa IAgendaCompletionActions e guarda apenas opções imutáveis do perfil. Cada consulta, conclusão ou leitura do último encerramento cria um TramaDbContext e o descarta com await using. Conexão ReadWrite abre banco existente e Pooling=false evita manter conexões no pool. Construir o serviço não inicializa nem migra um banco.

`DesktopComposition` registra o serviço como singleton no mesmo databasePath da Agenda, depois da inicialização normal do perfil. O singleton não compartilha contexto, conexão ou entidade rastreada entre operações. Outros contratos de edição permanecem intactos.

## Consulta e gravação

ListCandidatesAsync consulta de DateOnly.MinValue até o limite inclusivo pelo leitor existente e filtra estados Pendentes/Em andamento, preservando a ordem de dia, horário, sem horário depois, título e ID. Inclui tarefas de dias anteriores e tarefas reabertas, mesmo com último encerramento histórico. O serviço recebe o dia do chamador, sem decidir sozinho o horário de fechamento.

A implementação inicial materializa os registros do período, inclusive concluídos antes de filtrar. Não houve benchmark de grande volume; se necessário, especializar a consulta SQL com os mesmos critérios e testes. A lista é uma fotografia: não autoriza concluir uma tarefa sem revalidação.

CompleteAsync verifica argumento e cancelamento, cria o contexto em memória e chama CompleteAgendaTasks com o adaptador aprovado. O caso de uso rejeita seleção inválida antes de acessar o banco; o repositório revalida a seleção na transação. Construir um contexto não abre a conexão. Falha conhecida resulta em PersistenceFailed; cancelamento propaga. Contexto novo na próxima operação evita reaproveitar alterações de uma tentativa que falhou.

GetLastCompletedAtAsync usa o leitor aprovado e preserva instante/offset. IDs ausentes e conclusões legadas sem registro retornam null. Falha de consulta não é convertida em lista vazia ou instante inventado. Um caminho ausente não cria banco vazio.

## Evidências

Os 24 casos aprovados foram preservados byte a byte e passaram. A suíte completa passou com **703 testes: 117 unitários e 586 integrações**, zero falhas ou ignorados. Build Release sem avisos/erros e formatação aprovados no WSL.

Os cenários verificam consulta histórica, ordenação, atualizações externas/reabertura, conclusão preservando planejamento, perfis isolados com o mesmo ID, cancelamento, validação antes do disco, banco ausente, falha seguida de repetição, seleção desatualizada e composição. Duas tarefas em threads separadas compartilham o serviço e concluem IDs distintos, exercitando operações independentes. Isso não promete resolução de conflito entre duas edições do mesmo ID.

```bash
dotnet test tests/Trama.IntegrationTests -c Release --filter FullyQualifiedName~AgendaCompletionServiceTests --logger trx --results-directory artifacts/agenda-completion-service-green --verbosity quiet
bash tools/run-wsl.sh test
```

## Próximo passo

Especificar o diálogo de seleção e o fechamento coordenado: Encerrar tarefas e sair, Sair mantendo abertas ou Cancelar. Resolver as decisões que ainda podem cancelar a saída antes de gravar a conclusão, incluindo rascunhos dos Cadernos e da Agenda. O fechamento atual permanece inalterado; registrar o serviço não o chama automaticamente.

Sem migração, dependência, formato de backup/página ou teste alterado. Sem perfil pessoal, Git, nova publicação Windows ou manual/PDF. Roadmap atualizado e revisão humana pendente.

Para estudar: por que um singleton pode criar contextos independentes? Por que filtrar a lista não elimina a necessidade de uma transação? Como uma falha de gravação poderia afetar a próxima operação se o contexto fosse compartilhado?

[Especificação aprovada](68-servico-de-encerramento-especificacao.md) · [Relatório](../reviews/076-agenda-completion-service-green.md).
