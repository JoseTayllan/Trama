# Biblioteca visual e rascunhos — RED para revisão

Data: 27 de outubro de 2026. Especificação disponível para aprovação antes de implementar.

## Escopo

31 casos novos, sem editar os 126 anteriores: dez de alterações não salvas, 15 de navegação e seis de View/composição/persistência. O [capítulo de estudo](../estudos/13-navegacao-da-biblioteca.md) descreve os comportamentos e os arquivos.

Foram adicionados contratos mínimos ILibraryActions, SqliteLibraryActions, LibraryViewModel, IUnsavedChangesPrompt e uma LibraryWorkspaceView vazia. NotebookEditorViewModel recebeu HasUnsavedChanges e TryLoadAsync vazios; DesktopComposition recebeu parâmetro opcional de confirmação ainda não utilizado. Não houve implementação funcional, ligação da nova View, migração ou dependência nova.

## Execução final

```powershell
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/library-navigation-build --no-restore --logger "trx;LogFilePrefix=library-navigation-red-final" --results-directory artifacts/library-navigation-red/final-results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**157 casos executados: 126 aprovados e 31 falhando, sem ignorados.** Os 36 unitários e os 90 de integração existentes passaram; os 31 novos de integração falharam. Código final dos testes: 1; formatação: 0. Compilação concluída. Evidências TRX em `artifacts/library-navigation-red/final-results`.

Uma tentativa inicial foi interrompida pelo analisador xUnit2031 numa asserção nova. A asserção foi ajustada para o overload com predicado, sem suprimir o analisador. Esse erro de compilação não foi contado como RED comportamental; os resultados acima vêm da execução posterior completa.

| Grupo | Casos | Falha atual |
| --- | --- | --- |
| Estado de alterações do editor | 10 | HasUnsavedChanges e TryLoadAsync ainda não implementados; os casos param antes de completar suas asserções |
| Inicialização, criação e navegação | 15 | InitializeAsync do novo ViewModel ainda lança NotImplementedException, inclusive na preparação |
| View e bindings | 1 | InitializeAsync ainda ausente; os controles da View vazia ainda não são alcançados |
| Composição entre sessões | 1 | LibraryViewModel ainda não registrado no contêiner |
| Rollback de página/localização e validação de nomes | 4 | Métodos do novo port SQLite ainda lançam NotImplementedException |

Boa parte das asserções de navegação e rollback ainda não foi alcançada, pois a preparação para nos contratos vazios. O RED comprova descoberta dos cenários e ausência de implementação, não navegação pronta ou rollback comprovado.

## Riscos e limites

A proteção precisa considerar título, texto, linguagem, estrutura e imagem. Uma gravação não pode marcar como salva uma edição que chegou depois de seu snapshot. Cancelar deve preservar a seleção e o editor antes de carregar outro documento. Falha ao salvar impede a navegação; falha ao abrir o destino mantém a página anterior.

Salvar conteúdo e associar o caderno precisam de uma transação SQLite coordenada. Não confirmar transação de outro chamador nem alterar os testes para acomodar gravação parcial. A importação de anexos continua sem transação conjunta com arquivos.

O parâmetro novo de composição é opcional e mantém os consumidores antigos compilando. Nenhum serviço novo foi registrado nesta preparação. Não conectar os getters e métodos vazios à janela antes do GREEN.

Não estão cobertos fechamento da janela, autosave, falha do processo, desenho do diálogo de confirmação, teclado/foco, resultados atrasados de consultas concorrentes, renomeação, exclusão ou comandos visuais para mover páginas. Verificar os riscos concretos durante a implementação antes de declarar a interface pronta.

## Esforço e decisão

Um agente, sem delegação ou novas dependências. Uma tentativa interrompida por analisador e uma execução completa da suíte, mais formatação. Medição de tokens por tarefa indisponível. Reutilizar catálogo, editor e anexos já aprovados e limitar a exploração aos arquivos de navegação reduz retrabalho.

Decisão pendente: aprovação dos 31 casos, incluindo as escolhas Salvar e continuar / Descartar alterações / Cancelar e o salvamento associado ao caderno. Pausa prevista na seção 9 do CODEX.md.
