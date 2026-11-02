# Backup/restauração e correção de tags — GREEN

28 de outubro de 2026. Implementado após aprovação dos 34 testes de backup, formato/limites e seis casos adicionais de tags. Disponível para revisão do diff.

## Resultado

O botão Backup abre um painel para criar pacote de dados salvos e imagens referenciadas, ou restaurá-lo em pasta nova sem trocar o editor atual. O App aceita --data-directory para selecionar explicitamente o perfil. O painel informa que rascunhos não salvos ficam fora da cópia; fechamento normal é bloqueado durante a operação.

LocalBackupService usa BackupDatabase, manifesto formato 1, hashes SHA-256, orçamento expandido (1 GiB/10.000 entradas) e staging ao lado da saída. A validação recusa caminhos fora da lista permitida, duplicatas, entradas não listadas, hashes/tamanhos incompatíveis, SQLite/esquema inválido, referências órfãs e imagens faltantes. Diretórios/arquivos existentes não são substituídos. Nenhuma migração ou dependência foi acrescentada.

As imagens vêm do snapshot; são copiadas uma vez por ID. O formato atual utiliza arquivos imutáveis e não há coleta de anexos. Backup futuro com coleta/exclusão precisará preservar essa coordenação. Limpeza valida o caminho absoluto do staging antes da remoção recursiva e nunca apaga WAL/SHM da origem manualmente.

## Falha de tags relatada e aprovação

O responsável confirmou falha nos dois campos. Reprodução com seis casos: o filtro isolado já passava com caixa invertida; a consulta geral e os dois campos juntos falhavam porque os nomes das tags não participavam do texto pesquisado. RED: quatro falhas e dois aprovados. Os seis casos foram apresentados e aprovados explicitamente por pergunta assíncrona antes da correção.

SqliteKnowledgeDiscovery agora inclui tags na correspondência geral e normaliza os nomes para comparar o filtro exato. A consulta geral ignora caixa/acentos; o filtro ignora caixa e mantém a identidade acentuada da tag. Os dois critérios continuam combinando com E. Watermarks/tooltips explicam a distinção. Não houve consulta ao banco pessoal para investigar a falha.

## Execução e achado corrigido

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=backup-final" --results-directory artifacts/backup-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

- Focados finais: 40/40, sendo 34 de backup e seis de tags.
- Suíte final: **270/270**, 36 unitários e 234 integrações; nenhum ignorado.
- Build sem avisos/erros; formatação aprovada.
- Os testes de backup e todos os anteriores permanecem intactos. TagCaseRegressionTests é o único arquivo de teste novo desta implementação e foi aprovado separadamente.
- TRX em `artifacts/backup-green/results/`. A CI remota não foi executada.

Na primeira execução dos 40 casos, somente o teste de limpeza na falta de imagem falhou: uma conexão de origem somente leitura deixava auxiliares SQLite transitórios. Corrigido usando ReadWrite sem Create, para o SQLite administrar o próprio ciclo dos arquivos. A restauração WAL confirmada continuou passando. Depois da correção, os 40 casos passaram.

Revisão posterior reforçou a comparação do comprimento efetivo do manifesto e a validação de referências do catálogo. A suíte completa foi reconferida após os ajustes. Não foi necessário alterar os testes aprovados.

## Conferência externa

Harness Avalonia.Headless/Skia com composição real, banco/imagem sintéticos e cliques MouseDown/MouseUp. Verificou:

- Salvar tags em minúsculas/pesquisar em maiúsculas e o inverso, usando os dois campos reais.
- Criar backup e restaurar pelo painel; editor/rascunho atual mantidos, cópia contendo somente dados salvos.
- Abrir o caminho restaurado pelo resolvedor e nova composição; texto, código, tags e bytes PNG preservados.
- Operação controlada pendente bloqueando fechamento do painel e da janela principal.

Exit code 0. Imagens `backup-restored.png` e `restored-profile.png` inspecionadas. O formulário de backup usa rolagem; os caminhos longos usam campos copiáveis. O perfil restaurado foi aberto sem acessar LocalApplicationData real. A conexão do resolvedor ao App foi revisada no código, sem automatizar um novo processo CLI nativo.

## Limites e auditoria

Backup sem criptografia/autenticação; hashes verificam integridade, não origem. Só o esquema atual de migrações é aceito. Sem recuperação de interrupção de energia, restauração por mesclagem, agendamento ou captura de rascunhos. O trecho síncrono de BackupDatabase termina antes de observar cancelamento posterior; roda fora da thread da interface. Seletores nativos, teclado e escala Windows permanecem para conferência interativa.

Snapshot anterior, `backup-green.diff`, `audit.json`, harness e imagens estão no diretório externo `trama-backup-green`, sob a raiz de visualizações da sessão. A pasta do projeto continua sem Git. Auditoria final compara SHA-256 dos testes anteriores, preservação de migrações, links e padrões de credenciais; não foram adicionados logs nem mensagens internas de erro na interface.

## Esforço e próximos passos

Um agente, sem subagentes, executou implementação, revisão, testes e documentação. Esforço moderado: reprodução de tags, rodada de implementação, investigação/correção da limpeza SQLite e revisão dos limites/referências. Duas execuções da suíte completa, verificações focadas, build/formatação e uma conferência externa final. Tokens e custo monetário não medidos. Para economizar na próxima etapa, concentrar testes no passo 5 (colar/arrastar imagens e formatação), reutilizando o editor e armazenamento atuais.

Pendente do responsável: revisão do diff desta entrega. O passo 5 ainda não está implementado.
