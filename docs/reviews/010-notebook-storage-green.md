# Persistência do caderno e anexos — GREEN

Data: 27 de outubro de 2026. Implementação dos 18 casos aprovados, disponível para revisão humana do diff.

## Implementação

- `SqliteNotebookRepository` salva e atualiza páginas e blocos em uma transação. Lê por identidade, preservando ordem e conteúdo; adapta notas antigas como texto sem regravá-las.
- `NotebookPageRecord` e `NotebookBlockRecord` representam o armazenamento. A migração `20261027234457_NotebookPages` adiciona duas tabelas, chave estrangeira e índice único de posição por página. InitialNotes não foi alterada.
- `LocalImageAttachmentStore` copia PNG/JPEG por stream, reconhece o cabeçalho e impõe 10 MiB. O arquivo final só é publicado após terminar a cópia; temporários criados pela operação são removidos nas falhas. O destino é gerado por GUID dentro de attachments.

Não houve alteração dos testes, novos pacotes ou mudanças de tela. Nenhuma migração foi aplicada ao banco pessoal pelas ferramentas desta entrega.

## Verificação final

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/notebook-storage-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/notebook-storage-build --no-build --logger "trx;LogFilePrefix=notebook-storage-green-final" --results-directory artifacts/notebook-storage-green/final-results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Build com zero avisos e zero erros. **90 aprovados: 29 unitários e 61 de integração, sem casos ignorados.** Formatação aprovada após ajustar o arquivo gerado pelo EF. Os testes anteriores e os 18 aprovados foram mantidos intactos. Evidências TRX em `artifacts/notebook-storage-green/final-results`. A CI remota não foi executada.

Os testes demonstram atualização de banco antigo preservando Notes, adaptação literal de corpo antigo, reabertura de blocos mistos e imagem, substituição de conteúdo, isolamento entre páginas, cancelamento prévio e banco somente leitura. Também demonstram cópia de PNG/JPEG, preservação da origem, rejeição de cabeçalho inválido/truncado, limite de tamanho, falha de leitura sem arquivos abandonados e anexo ausente.

## Revisão e riscos restantes

O repositório usa contexto emprestado: descartar após falhas e não compartilhar operações concorrentes. As duas gravações pertencem à mesma transação; a leitura usa uma única consulta da página com seus blocos. Não há transação conjunta entre banco e arquivos. O adaptador não confirma a existência física de toda referência de imagem recebida.

A versão de formato e os tipos de blocos desconhecidos são rejeitados, mas esses guardas ainda não têm testes específicos. A identidade do bloco legado é estável por usar o ID da nota, também sem asserção específica no conjunto aprovado. Salvar uma nota antiga como página estruturada mantém Notes intacta; o novo leitor prioriza a página e o leitor antigo continua mostrando o registro original. Cobrir a transição antes de oferecer edição desse material na interface.

Imagens são reconhecidas pelo cabeçalho, sem decodificação integral ou validação de dimensões. A limpeza de temporários funciona nos cenários testados; falha do sistema ao excluir um arquivo ainda pode deixar um temporário e deve ser tratada na política de recuperação futura. Interrupção abrupta do processo e falta de espaço no disco não foram simuladas. Não alegar validação completa de arquivos arbitrários nem limpeza automática de órfãos.

O Down da nova migração remove páginas e blocos; não aplicá-lo a dados pessoais sem autorização e backup recuperável. A migração é aplicada automaticamente pelo inicializador existente na próxima abertura do desktop, mas esta entrega só a executou em bancos de teste.

## Próxima fatia e esforço

Antes de conectar a página contínua, especificar os comandos e estados do editor, coordenação entre importação e salvamento, reabertura e apresentação de imagens e código. A interface atual permanece com o formulário anterior.

Um agente, sem delegação nem dependências adicionais. Houve geração e revisão de uma migração, duas execuções da suíte e ajuste localizado de formatação, com nova confirmação após a mudança na limpeza de temporários. A medição de tokens por tarefa está indisponível. Reutilizar estes contratos e limitar a próxima exploração ao editor reduz consumo.
