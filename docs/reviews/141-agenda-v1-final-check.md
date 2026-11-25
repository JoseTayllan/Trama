# Agenda v1 · conferência final

Em 19/11/2026, o responsável confirmou o commit 2 de recuperação. Push e Verify desse commit ainda não foram confirmados nesta conversa. Nenhum comando Git foi executado pelo agente.

## Evidências técnicas

- Em 17/11, build Release, formatação, 24 testes de armazenamento de backup e a suíte de 1.358 testes passaram no WSL. Ver o [relatório 140](140-backup-recovery-validation.md).
- Em 19/11, `dotnet ef migrations has-pending-model-changes` confirmou o modelo alinhado à última migração. Existem 15 migrações; não foi criada outra.
- O comando de EF usou `--database /tmp/trama-agenda-v1-model-audit-20261119.db`, sem acessar perfis pessoais. A primeira tentativa sem esse argumento foi rejeitada pela fábrica de contexto, conforme sua proteção.
- `dotnet restore Trama.slnx --locked-mode --ignore-failed-sources --verbosity quiet` passou no espelho WSL. A opção de fontes não modifica nem relaxa o lock; não comprova acesso de rede às fontes.
- O workflow Windows mantém restore travado, build, formatação e testes. `Directory.Build.props` separa os locks de publicação por RuntimeIdentifier em `obj/nuget-locks`, preservando os locks genéricos usados pelo Verify. Nenhum arquivo de build/CI foi alterado nesta etapa.

## Aceite de uso antes da integração

Usar um perfil de teste para restaurar backups. Preservar o perfil pessoal e não apontar uma restauração para ele.

| Conferência | Resultado esperado | Estado nesta rodada |
| --- | --- | --- |
| Windows e WSL | Alternar Início/Cadernos/Agenda sem perder edição; fechar/cancelar com rascunhos | Pendente |
| Tarefas e semana | Criar, editar, reagendar, concluir/reabrir e excluir/restaurar; filtros e datas cruzando dias | Pendente |
| Horas realizadas | Iniciar como Em andamento, concluir depois e conferir a duração; planejamento preservado | Já validado antes; reconferência integrada pendente |
| Páginas vinculadas | Abrir uma ou várias páginas pelo cartão e retornar à Agenda | Já validado antes; reconferência integrada pendente |
| Retrospectiva | Salvar reflexão, reabrir; fechamento preservado e status Atual acompanhando a tarefa | Pendente nesta rodada |
| Recuperação | Perfil restaurado contém tarefas, anexos, vínculos, execução, reflexão e fechamento | Automação WSL aprovada; uso final pendente |
| Janela compacta | Dia, Semana, Retrospectiva e formulário continuam acessíveis | Pendente nesta rodada |
| GitHub | Push e Verify verdes para o commit 2; revisar diff antes da integração | Pendente de confirmação |

Os testes comprovam regras e integração; o aceite visual permanece com o responsável. A limitação do clipboard de prints no WSL continua registrada. O bloqueio local de DLLs pelo Controle de Aplicativos do Windows impede declarar uma execução nativa dos testes pelo agente; não foi alterada a segurança do sistema.

## Alteração desta etapa

Atualizados somente este relatório, o resumo de `rodmap.md` e seu painel gerado. Estados antigos foram mantidos nas seções de histórico; o topo agora informa o fechamento da Agenda. Nenhuma mudança de funcionalidade, banco, contratos ou testes. A suíte não foi repetida por se tratar de documentação sem alteração do produto.

O próximo módulo previsto é o painel do acervo, cujo protótipo já foi aprovado; sua implementação começa após o aceite da Agenda. Recorrência, lembretes, instalador e assinatura permanecem fora desta primeira versão.
