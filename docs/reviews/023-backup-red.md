# Passo 4 — backup e restauração em RED

28 de outubro de 2026. Especificação para aprovação; nenhuma funcionalidade de backup conectada ao aplicativo.

## Escopo

34 casos novos: 22 de armazenamento e 12 de coordenação/inicialização. Definem pacote de banco/imagens referenciadas, manifesto/hashes, WAL confirmado com gravação sem commit em paralelo, restauração independente da origem, validação contra corrupção/caminhos maliciosos, limites expandidos, preservação de destinos existentes, estados de falha/espera e abertura explícita de perfil. A [especificação de estudo](../estudos/18-backup-e-restauracao.md) detalha o formato e as regras propostas.

Foram criados apenas ILocalBackup/DTOs, LocalBackupService, BackupViewModel e DataDirectoryOptions, com métodos provisórios que lançam NotImplementedException. Não houve conexão ao App, container, controles ou mudança de migração. Os ZIPs gerados pelas fixtures servem para validar a especificação do leitor futuro, não constituem um backup funcional do produto.

## Execução

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --logger "trx;LogFilePrefix=backup-red-final" --results-directory artifacts/backup-red/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Build sem avisos/erros; formatação aprovada. Resultado final: **264 casos, 230 aprovados, 34 falhas esperadas, nenhum ignorado**. Os 36 unitários e os 194 casos anteriores de integração continuam passando.

Motivos: 12 NotImplementedException diretas e 22 verificações de exceção recebendo NotImplementedException em vez do tipo especificado. Vários casos param na preparação ou na primeira operação: não afirmar que as asserções posteriores já foram executadas. Não houve falha de compilação ou ausência de infraestrutura dos testes.

Após a primeira execução, o caso WAL foi reforçado para gravar texto confirmado exclusivamente no WAL antes da transação pendente. A suíte foi executada novamente com o mesmo resultado. A comparação da restauração deverá distinguir a gravação confirmada daquela ainda sem commit; uma simples cópia do arquivo principal não satisfaz esse caso.

TRX em `artifacts/backup-red/results/`. Todos os dados são sintéticos em diretórios temporários; o banco pessoal não foi aberto. A criação da nova pasta Application/Storage exigiu execução fora do sandbox, pois as permissões ainda apontavam para o nome antigo IDEA. Nenhum código de implementação foi escrito para contornar essa restrição.

## Revisão e riscos

Ainda é necessário implementar o snapshot SQLite, validação de pacote/esquema, cópia dos anexos e staging com publicação sem sobrescrita. Os testes de restauração não substituem conferência de todo erro de disco ou de interrupção do processo. O formato inicial será sem criptografia; hashes verificam integridade e não assinatura/autenticidade.

A escolha inicial é restaurar em pasta nova e abrir explicitamente via --data-directory, preservando o perfil e o rascunho atuais. Limites propostos: 1 GiB expandido e 10.000 entradas. A interface futura deve explicar que o backup contém somente conteúdo salvo. A conexão do painel, proteção de fechamento enquanto ocupa o serviço e abertura do perfil real serão conferidas no GREEN.

Snapshot, `backup-red.diff` e `audit.json` ficam no diretório externo `trama-backup-red`, sob a raiz de visualizações da sessão. A auditoria compara hashes dos testes anteriores, preservação das migrações, links locais e padrões de credenciais literais nos arquivos envolvidos. Nenhum log foi acrescentado.

Um agente, sem subagentes; uma rodada de especificação/revisão, build, duas execuções completas e formatação. Esforço moderado; tokens e custo monetário não medidos. Para economizar no GREEN, começar pelos 34 casos afetados e executar a suíte completa depois da integração.

Decisão pendente: aprovação dos testes, formato e limites propostos antes de implementar. O CODEX, seção 9, determina “PARAR para revisão”, conforme o TDD solicitado pelo responsável.
