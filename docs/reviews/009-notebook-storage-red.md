# Persistência do caderno e imagens — RED para revisão

Data: 27 de outubro de 2026. Especificação disponível para aprovação antes da implementação.

## Arquivos e escopo

- [NotebookStorageTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NotebookStorageTests.cs): dez casos de persistência e oito de anexos, incluindo variações parametrizadas. Fixtures são imagens sintéticas e bancos temporários.
- [NotebookSnapshot e INotebookRepository](../../src/Trama.Application/Knowledge/NotebookSnapshot.cs) e [IImageAttachmentStore](../../src/Trama.Application/Knowledge/IImageAttachmentStore.cs): contratos para compilação.
- [SqliteNotebookRepository](../../src/Trama.Infrastructure/Persistence/SqliteNotebookRepository.cs) e [LocalImageAttachmentStore](../../src/Trama.Infrastructure/Persistence/LocalImageAttachmentStore.cs): adaptadores vazios, sem ligação com o desktop.
- [Explicação da especificação](../estudos/09-persistencia-do-caderno-e-anexos.md).

Nenhuma implementação funcional, migração, pacote ou tela foi adicionada. Não foram alterados os testes anteriores.

## Execução e resultado

```powershell
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/notebook-storage-build --logger "trx;LogFilePrefix=notebook-storage-red" --results-directory artifacts/notebook-storage-red/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

O runner compilou e executou **90 casos**. Passaram 29 unitários e 43 de integração existentes; falharam os 18 novos de integração. Testes terminaram com código 1, sem casos ignorados. Formatação terminou com código 0. Evidências TRX em `artifacts/notebook-storage-red/results`; a CI remota não foi executada.

| Grupo | Casos | Falha atual |
| --- | --- | --- |
| Migração com nota antiga preservada | 1 | Conteúdo antigo preservado, mas não há segunda migração |
| Leitura de texto legado sem reinterpretar o corpo | 2 | GetByIdAsync lança NotImplementedException |
| Reabertura de página mista com imagem | 1 | ImportAsync lança NotImplementedException na preparação |
| Atualização, isolamento entre páginas, cancelamento e somente leitura | 4 | SaveAsync lança NotImplementedException na preparação |
| Página ausente e esquema ausente | 2 | GetByIdAsync lança NotImplementedException; os resultados específicos ainda não existem |
| Importação de PNG/JPEG | 2 | ImportAsync lança NotImplementedException |
| Cabeçalho inválido/truncado, tamanho, erro de leitura e cancelamento | 5 | ImportAsync lança NotImplementedException em vez do erro especificado |
| Anexo ausente | 1 | OpenReadAsync lança NotImplementedException |

As falhas dos adaptadores vazios comprovam execução dos cenários e ausência de implementação. As asserções posteriores, como reabertura e preservação após falhas, ainda não foram alcançadas nesses casos. Não apresentar esse RED como suporte pronto a imagens ou transações.

## Riscos e limites para a próxima implementação

O limite de 10 MiB é uma proposta inicial desta especificação. Os testes não comprovam decodificação completa nem limitação de dimensões de imagens. Não há ainda cancelamento durante cópia, rollback de falha intermediária de escrita, tratamento de anexo ausente numa página, coleta de órfãos ou recuperação conjunta de banco e arquivos. Esses comportamentos precisam de fatias próprias antes de um fluxo completo na interface.

Não implementar metadados ou blocos como JSON indistinguível no corpo de uma nota antiga. Não reescrever InitialNotes nem recriar o banco. Preparar uma migração nova e validar a compatibilidade com os testes existentes. O armazenamento de imagem não pode publicar arquivos parcialmente copiados.

## Esforço e decisão pendente

Um agente, sem delegação, uma execução da suíte completa e uma verificação de formatação. Sem dependências adicionadas; medição de tokens por tarefa indisponível. Reutilizar este mapeamento e trabalhar apenas nesses adaptadores reduz a exploração nas próximas etapas.

Decisão pendente: aprovação dos 18 casos e do limite inicial de 10 MiB antes de implementar, conforme a seção 9 do CODEX.md.
