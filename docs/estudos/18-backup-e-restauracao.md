# Backup e restauração: implementação e estudo

28 de outubro de 2026. Passo 4 implementado após aprovação dos 34 testes e limites. A entrega inclui a correção de tags com seis casos aprovados separadamente; os 270 testes passam. Diff disponível para revisão.

## Como usar

Abra **Backup** no topo do aplicativo. Escolha um arquivo novo `.trama-backup` e clique em **Criar backup**. Para restaurar, selecione o pacote, informe uma pasta que ainda não exista e clique em **Restaurar cópia**. O seletor de pasta permite escolher o local onde será criada uma subpasta nova; o caminho também pode ser editado manualmente. O formulário é rolável.

A biblioteca atual não é substituída nem mesclada. Os caminhos dos últimos resultados aparecem somente após sucesso. Operações repetidas são bloqueadas; falhas apresentam orientação sem revelar detalhes internos de exceções. O painel e a janela principal recusam fechamento normal enquanto a operação está em andamento. Feche o painel depois da conclusão para continuar a edição.

O backup contém somente dados já salvos. Não salva automaticamente o rascunho da página; a interface explica esse limite antes da exportação. Imagens importadas mas ainda não associadas a páginas salvas ficam fora do pacote. A restauração não troca automaticamente a biblioteca aberta, preservando qualquer rascunho atual.

Para utilizar a cópia restaurada, a inicialização aceita `--data-directory` seguido de um caminho absoluto. Sem esse argumento, continua usando o perfil padrão. Entrada inválida é rejeitada, sem abrir silenciosamente outro perfil. O argumento está conectado ao App; o tooltip de Armazenamento local mostra o caminho do perfil ativo.

Exemplo, a partir da raiz do projeto, após o build Release:

```powershell
dotnet run --project src/Trama.Desktop --configuration Release --no-build -- --data-directory "C:\Backups\Trama-Restaurado"
```

## Conteúdo do pacote

Formato inicial: ZIP com extensão `.trama-backup`, composto por:

- `manifest.json`: formato 1; lista de arquivos com `path`, `length` e `sha256`, em JSON camelCase.
- `trama.db`: snapshot consistente de todo o banco atual, preservando notas antigas, páginas/blocos, catálogo, localização e tags.
- `attachments/<guid-sem-hífens>.image`: cada imagem referenciada no snapshot, uma vez por ID. Imagens compartilhadas por blocos não geram cópias duplicadas.

Arquivos pessoais não relacionados, temporários de importação e anexos sem referência não entram no pacote. O manifesto descreve exatamente os arquivos de dados; não inclui a si mesmo para evitar uma dependência circular de hash. Os hashes usam SHA-256 hexadecimal em minúsculas.

Limites iniciais aprovados, ajustáveis por BackupArchiveLimits: **1 GiB de tamanho total expandido e 10.000 entradas**, incluindo manifesto. Há também um teto de 1 MiB para o manifesto. A leitura conta bytes efetivos além dos tamanhos declarados. A tela de configuração desses valores não faz parte desta entrega. O pacote inicial não é criptografado; hashes detectam corrupção, não autenticam a origem. O cofre futuro exige formato e proteção próprios.

## Consistência e publicação

Copiar apenas o arquivo SQLite enquanto o aplicativo está aberto pode perder transações que estão no WAL. O serviço usa SqliteConnection.BackupDatabase em uma tarefa separada para capturar dados confirmados sem bloquear a interface durante a cópia. O teste WAL mantém uma conexão aberta, grava um texto confirmado no WAL e depois outro ainda sem commit: a restauração contém o primeiro.

A conexão da origem usa ReadWrite sem Create. Isso permite ao próprio SQLite remover os auxiliares transitórios ao fechar quando não há outros donos. A primeira tentativa com conexão somente leitura deixava arquivos auxiliares após uma falha de imagem; o teste de limpeza identificou o problema. Nunca remover WAL/SHM da origem manualmente: podem pertencer a uma gravação ativa. A origem precisa de acesso compatível com o perfil gravável do aplicativo.

Identificar as referências de imagem **a partir do snapshot**, e não de uma consulta posterior ao banco em edição. Os arquivos gerenciados atuais são identificados por GUID e não são sobrescritos: inclusões posteriores não mudam os bytes de imagens já referenciadas. Não há coleta de anexos nesta fase. Uma futura exclusão/coleta precisa ser coordenada com a criação do backup. Se uma imagem referenciada faltar, falhar sem publicar um pacote incompleto.

Criar o pacote em área temporária controlada e publicar o arquivo final somente após concluir e verificar os dados. Um arquivo de saída existente nunca será sobrescrito. Falhas devem remover somente os temporários criados pela operação.

## Restauração

Validar o ZIP, versão do formato, manifesto, ausência de entradas duplicadas/não listadas, tamanhos e hashes antes de publicar o perfil. Aceitar apenas os nomes esperados; rejeitar caminhos absolutos, `..`, caminhos de unidade e qualquer tentativa de escapar da pasta de trabalho. Extrair os bytes como arquivos regulares em staging controlado, sem executar conteúdo do pacote.

Validar o banco SQLite, seu esquema e as referências de imagem. Mesmo que os hashes estejam corretos, rejeitar um banco inválido, migrações desconhecidas/futuras ou imagens referenciadas ausentes. Os testes iniciais cobrem pacotes do esquema atual; não prometem compatibilidade com qualquer banco arbitrário ou versão futura.

Somente depois da validação, publicar por movimentação do staging para a pasta nova, no mesmo volume. Pasta de destino já existente será rejeitada, inclusive vazia. Não mesclar arquivos nem substituir o perfil em uso. Falhas e pré-cancelamento não devem deixar destino ou temporários publicados. A restauração precisa funcionar sem acesso à pasta original; o teste muda essa pasta de lugar antes de restaurar.

## Arquivos e testes

- [ILocalBackup e DTOs](../../src/Trama.Application/Storage/ILocalBackup.cs): criação, restauração, manifesto, recibos e limites.
- [LocalBackupService](../../src/Trama.Infrastructure/Persistence/LocalBackupService.cs): snapshot, cópia limitada/hashes, criação do pacote e publicação por staging.
- [Validação do pacote](../../src/Trama.Infrastructure/Persistence/LocalBackupService.Validation.cs): lista permitida de caminhos, manifesto, orçamento, integridade e esquema SQLite/referências.
- [BackupViewModel](../../src/Trama.Desktop/ViewModels/BackupViewModel.cs): estados de espera, resultado, falha e bloqueio de solicitações concorrentes.
- [Painel Backup](../../src/Trama.Desktop/Views/BackupDialog.axaml): seletores de arquivo/pasta, campos locais e feedback; code-behind somente coordena a interface.
- [DataDirectoryOptions](../../src/Trama.Desktop/DataDirectoryOptions.cs): seleção explícita do perfil, usada pelo App antes de preparar a composição.
- [BackupStorageTests](../../tests/Trama.IntegrationTests/Knowledge/BackupStorageTests.cs): 22 casos com SQLite e ZIP reais, dados temporários e imagens PNG/JPEG sintéticas.
- [BackupWorkflowTests](../../tests/Trama.IntegrationTests/Knowledge/BackupWorkflowTests.cs): 12 casos de estado, falhas, concorrência e seleção de perfil.

São 270 casos aprovados: 36 unitários e 234 integrações. Os 34 casos de backup aprovados permaneceram intactos; seis regressões de tags foram adicionadas e aprovadas antes da correção. Build sem avisos e formatação aprovados. O [relatório RED](../reviews/023-backup-red.md) preserva a especificação inicial; o [relatório GREEN](../reviews/024-backup-green-e-correcao-tags.md) registra os resultados finais.

## Conferência e limites

A suíte especifica restauração fiel, WAL confirmado, conteúdo/identidades/tags/imagens, falhas de validação, caminhos maliciosos, orçamentos e pré-cancelamento. Não injeta todas as falhas de disco, interrupção de energia ou cancelamento em cada etapa. Isso exige avaliação posterior; a publicação por staging limita a exposição de dados incompletos, sem prometer recuperação de toda falha de hardware.

A conferência Avalonia.Headless/Skia com cliques simulados criou e restaurou um pacote pelo painel real, manteve o rascunho original, abriu o perfil restaurado com o resolvedor/composição reais e comparou conteúdo, código, tags e bytes da imagem. Uma operação controlada pendente comprovou o bloqueio de fechamento do painel e da janela principal. A conexão do argumento ao App foi revisada no código; essa conferência não iniciou um segundo processo nativo pelo CLI.

Seletores nativos de arquivo/pasta, teclado e escala do Windows ainda exigem conferência interativa. A evolução de formatação do [capítulo 19](19-imagens-e-formatacao.md) acrescentou atualização de prefixos conhecidos de migrações em staging, incluindo backups do conjunto anterior de quatro migrações. O formato do pacote continua 1. A rotina não fornece backup agendado, criptografia ou recuperação de rascunhos. A limpeza valida que o staging é uma pasta irmã criada pela operação; não utiliza nomes do pacote para determinar a raiz a excluir.

## Perguntas para estudo

1. Por que copiar `trama.db` pode ser insuficiente quando existe WAL?
2. Por que as referências de imagens devem vir do snapshot do banco?
3. O que um hash prova e o que ele não prova?
4. Por que validar em staging antes de publicar uma pasta?
5. Como demonstrar restauração sem depender dos arquivos originais?
