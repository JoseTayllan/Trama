# Editor contínuo — RED para revisão

Data: 27 de outubro de 2026. Aguardando aprovação dos testes antes da implementação.

## Escopo revisável

- [SaveNotebookTests](../../tests/Trama.UnitTests/Knowledge/SaveNotebookTests.cs): sete casos novos.
- [NotebookEditorTests](../../tests/Trama.IntegrationTests/Knowledge/NotebookEditorTests.cs): 16 casos novos, incluindo dois casos parametrizados de falha no salvamento e dois de importação inválida/cancelada.
- Contratos mínimos: INotebookActions, NotebookSaveResult, SaveNotebook, NotebookEditorViewModel, NotebookBlockEditor e uma NotebookEditorView vazia.
- [Material de estudo e comportamento previsto](../estudos/10-editor-continuo-do-caderno.md).

Não foi conectada uma nova tela, implementado um caso de uso, alterada a composição existente ou adicionada uma dependência. Os 90 testes anteriores não foram editados.

## Execução final

```powershell
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/notebook-editor-build --logger "trx;LogFilePrefix=notebook-editor-red-final" --results-directory artifacts/notebook-editor-red/final-results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**113 casos executados: 90 aprovados e 23 falhando, sem casos ignorados.** Dos anteriores, passaram 29 unitários e 61 de integração. Entre os novos, falharam sete unitários e 16 de integração. Testes terminaram com código 1; compilação concluída. Formatação terminou com código 0. Evidências TRX em `artifacts/notebook-editor-red/final-results`.

| Grupo | Casos | Motivo atual |
| --- | --- | --- |
| SaveNotebook: vazio, título, código, imagem, falha, cancelamento e espera pela persistência | 7 | ExecuteAsync lança NotImplementedException |
| Estado inicial, comandos, salvamento, importação e carregamento do editor | 14 | Coleção inicial vazia ou membros ainda lançando NotImplementedException |
| Bindings e rolagem da página | 1 | A View ainda não tem os controles esperados |
| Composição, salvar e reabrir dados e bytes de imagem | 1 | NotebookEditorViewModel ainda não está registrado no contêiner |

O RED não comprova o funcionamento das asserções posteriores: boa parte dos casos para antes de salvar, carregar ou importar. Registra que os cenários são descobertos e que os comportamentos ainda não existem.

## Riscos e limites

Esta especificação muda o comportamento do editor novo: salvar mantém o documento aberto e reutiliza a identidade. O editor antigo e seus testes permanecem intactos. A implementação deve manter a antiga composição resolvível enquanto conecta a nova interface.

Os testes usam um port de aplicação em vez de repositórios no ViewModel. O teste real exige um editor novo por resolução e armazenamento recuperável, mas não demonstra sozinho descarte de contexto em todos os caminhos de erro.

Os testes da interface não validam realce de sintaxe, decodificação de imagens, diálogo nativo, escala do Windows ou contraste. Não considerá-los evidência desses pontos. A composição visual deve ser inspecionada antes da entrega. A navegação e a confirmação de descarte de rascunhos ainda não têm contrato nesta fatia.

Importar e depois falhar ao salvar não apaga o arquivo gerenciado. Coleta de órfãos e recuperação entre banco e arquivos continuam pendentes; não introduzir exclusão física automática como solução sem especificação própria.

## Esforço e decisão

Um agente, sem delegação ou dependências novas. Duas execuções da suíte porque a especificação foi ampliada com espera de importação, falha de abertura e código como único conteúdo; uma verificação de formatação. Medição de tokens por tarefa indisponível. O próximo GREEN deve focar nesses contratos e na View, reutilizando os adaptadores já validados.

Decisão pendente: aprovar os 23 casos e o comportamento de manter a página aberta após salvar. Pausa prevista na seção 9 do CODEX.md.
