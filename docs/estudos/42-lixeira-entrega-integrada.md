# Lixeira integrada de espaços, cadernos e páginas

31/10/2026. Os 18 casos de fluxo e os dez complementares foram aprovados antes de suas implementações e permanecem intactos. A suíte completa passou com **454 testes**. O responsável validou o uso da lixeira após a entrega. A revisão do diff continua registrada separadamente; a validação de uso não é tratada como confirmação automática de leitura do código.

## Uso e comportamento

Na lateral, Excluir envia o espaço, caderno ou página selecionados para a lixeira. A confirmação informa quantos itens serão afetados e mantém Cancelar como ação padrão. Havendo rascunho, continua existindo a escolha de salvar, descartar ou cancelar. Excluir não apaga definitivamente o conteúdo ou os arquivos de imagens.

O botão Lixeira no rodapé abre a lista das exclusões. Selecione uma entrada e use Restaurar. Espaços e cadernos recuperam os descendentes que foram enviados junto deles. Uma página/caderno excluído anteriormente continua na lixeira; restaurar o pai não desfaz uma exclusão independente. Quando a origem ainda está excluída, o botão Restaurar fica desabilitado e orienta restaurar o espaço/caderno antes.

A identidade dos registros é mantida. Texto, estilos, código, tags e imagens permanecem associados aos IDs originais. Itens excluídos não aparecem nas listas e na busca normais, incluindo notas do formato antigo. Um snapshot ou autosave atrasado recebe falha de gravação e não reativa a página.

## Estrutura e compatibilidade

`ILibraryTrashActions` separa prévia, envio, listagem e restauração de renomeação/movimentação. A sexta migração cria LibraryTrash, com tipo/ID, lote, raiz, título e data. Não altera as tabelas de conteúdo para remover dados. As cinco migrações anteriores e os construtores anteriores de LibraryViewModel foram preservados.

O fluxo de exclusão usa transações para reunir as marcações. A restauração remove somente marcações do mesmo lote; lotes anteriores permanecem. `LibraryTrashVisibility` controla as consultas e gravações ativas e reconhece bancos históricos sem a tabela. A interface pausa o autosave durante a decisão e descarta o editor antigo após o envio concluído.

O backup inclui banco e anexos de itens ativos e excluídos. A leitura pública continua ocultando a lixeira; `ReadStoredForBackupAsync` é um caminho interno para validar integralmente o conteúdo armazenado. O validador também confere identidades, origens, raiz única do lote e vínculo dos descendentes. Um backup não pode ser aceito só porque os dados inválidos estão escondidos na lixeira.

O pacote de backup continua na versão 1 e as páginas continuam usando versões 1/2/3. Pacotes anteriores à lixeira são atualizados na restauração. A atualização do esquema anterior foi testada preservando conteúdo, tags, imagens e localização. Cancelamento e falha no meio da cascata mantêm o estado anterior.

## Evidências

Build Release sem avisos/erros, formatação e **454 testes aprovados** no WSL: 77 unitários e 377 de integração, zero falhas/ignorados. São os 426 anteriores mais os 28 casos aprovados, sem alterações nos testes. Os três casos complementares que estavam em RED passaram após as correções.

Harness SQLite/Skia com perfil fictício percorreu confirmações, página excluída antes do espaço, bloqueio de restauração, recuperação do pai e depois do filho, lixeira vazia e reabertura da página com IDs/estilos mantidos. Também criou e restaurou um backup completo e recuperou um espaço excluído no perfil restaurado. As capturas foram inspecionadas. O pacote Windows isolado abriu, respondeu e encerrou normalmente.

Ver [relatório integrado](../reviews/051-library-trash-safety-green.md). Os capítulos [40](40-lixeira-fluxos-implementados.md) e [41](41-lixeira-seguranca-especificacao.md) preservam a evolução entre primeiro GREEN, riscos encontrados e revisão dos testes adicionais.

## Revisão e limites

O launcher Testar-Lixeira.cmd usa um perfil fictício separado. Há um espaço “Exemplo para restaurar” na lixeira para experimentar a recuperação sem envolver dados de uso. A publicação habitual e os perfis pessoais Windows/Linux não foram substituídos na validação.

Para executar pelo WSL depois de encerrar a instância anterior e salvar o trabalho, use o helper existente:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh run
```

O helper compila o espelho e mantém o perfil Linux separado. Uma janela já aberta não recebe hot reload desta entrega. Não é necessário reiniciar o WSL para atualizar o aplicativo normalmente.

Não há exclusão permanente nem esvaziamento automático nesta entrega. Revisão humana da aparência, outros DPI e testes interativos no computador de destino continuam pendentes. Os testes de falha/cancelamento/debounce cobrem cenários definidos, sem afirmar que esgotam todas as disputas possíveis entre várias instâncias. Manual/PDF permanecem conforme a preferência do responsável, sem regeneração automática.
