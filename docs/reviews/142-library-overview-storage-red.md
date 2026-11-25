# Painel do acervo · consulta em RED

Em 19/11/2026, após o aceite da Agenda, foi iniciada a consulta do painel do acervo. Alterações funcionais limitadas ao contrato `ILibraryOverviewReader` e aos registros do resultado na Application. Não há implementação do leitor, novo banco, migração, tela ou mudança de testes anteriores.

`LibraryOverviewStorageTests.cs` contém 19 casos para totais globais, espaços vazios, identidades distintas com títulos iguais, compatibilidade de notas antigas, exclusão dos três níveis da lixeira, cinco tags mais usadas, normalização, atualização após gravação/restauração, perfis isolados, cancelamento, banco ausente, leitura sem gravação e registro no aplicativo.

Build Release e formatação passaram no WSL. Os 19 casos falham: 18 exigem que SqliteKnowledgeDiscovery ofereça ILibraryOverviewReader, e um exige o registro na composição. O log detalhado confirma que as falhas chegam às asserções de disponibilidade, após a preparação de dados. A suíte anterior de 1.358 passou com o filtro de exclusão dos novos casos. A base permanece funcional; a suíte total está intencionalmente RED até a implementação aprovada.

Comandos executados no espelho WSL:

```sh
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter FullyQualifiedName~LibraryOverviewStorageTests
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~LibraryOverviewStorageTests'
```

A primeira verificação de formatação identificou CRLF nos arquivos novos; foram normalizados para LF conforme .editorconfig antes da verificação final. Nenhum teste existente foi alterado. A execução usa perfis temporários; nenhum banco pessoal foi acessado.

Riscos que a implementação deve tratar: contar descendentes ocultos, somar duas vezes IDs de notas antigas e páginas atuais, gerar inconsistência entre totais e espaços durante gravações concorrentes e abrir um banco vazio por engano. A consulta deve ser consistente em uma transação de leitura e evitar carregar o conteúdo de todas as páginas. A tela e os riscos de navegação serão especificados na próxima entrega.

Revisão do contrato, das regras e dos casos é obrigatória antes do GREEN conforme CODEX §9, que exige “aguardar aprovação explícita do responsável”. O aceite anterior da Agenda não aprova esses novos casos. Não realizar commit desta especificação RED como uma entrega concluída.

Um agente, sem delegação. Uma preparação e uma rodada de correção de formatação; esforço moderado. Não há medição de tokens ou custo. Para economizar na próxima etapa, reutilizar esses contratos e os leitores de páginas recentes, sem repetir o mapeamento da Agenda. A verificação dos arquivos alterados não identificou credenciais; os dados dos testes são fictícios.
