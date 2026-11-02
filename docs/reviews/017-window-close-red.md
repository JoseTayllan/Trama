# Proteção ao fechar — RED para revisão

Data: 28 de outubro de 2026. Primeiro dos cinco passos solicitados, aguardando revisão dos testes antes da implementação.

## Arquivos

- [WindowClosingTests](../../tests/Trama.IntegrationTests/Knowledge/WindowClosingTests.cs): 14 casos novos.
- [LibraryViewModel](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs): somente RequestCloseAsync vazio como contrato de compilação, sem integração com a janela.
- [Plano e explicação](../estudos/15-cinco-passos-e-protecao-ao-fechar.md).

Nenhum teste aprovado, diálogo, serviço de persistência ou comportamento de fechamento foi alterado.

## Execução final

```powershell
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --logger "trx;LogFilePrefix=window-close-red-final" --results-directory artifacts/window-close-red/final-results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**171 casos executados: 157 aprovados e 14 falhando, sem ignorados.** Passaram os 36 unitários e os 121 de integração anteriores; falharam os 14 novos de integração. Compilação concluída. Código final dos testes: 1; formatação: 0. Evidências TRX em `artifacts/window-close-red/final-results`.

Onze cenários do coordenador falham porque RequestCloseAsync ainda lança NotImplementedException. Três casos da janela falham porque ela fecha sem confirmar: Cancelar e gravação pendente não a mantêm visível, e Descartar fecha sem ter consultado o prompt. O RED não comprova as asserções posteriores de integridade e espera; registra comportamento ausente.

## Riscos para a implementação

Cancelar imediatamente o evento de fechamento enquanto houver decisão pendente. Não iniciar várias confirmações nem permitir reentrada que duplique Closed. Bloquear edição até decidir, restaurando-a ao cancelar.

Somente fechar depois de salvar com sucesso e verificar que não surgiu edição posterior ao snapshot. Falha no prompt, validação ou persistência deve manter a janela aberta e apresentar mensagem sem detalhes técnicos. Uma gravação já em andamento bloqueia a tentativa atual, sem fechar automaticamente depois dela.

O descarte autoriza o encerramento, mas não requer destruir antecipadamente o editor. O fechamento normal e o evento Closed devem ocorrer uma única vez. A confirmação não equivale a recuperação de queda do processo ou interrupção forçada.

## Esforço e decisão

Um agente, sem delegação nem dependências novas. Duas execuções da suíte, após acrescentar verificações de bloqueio/restauração da edição, e uma verificação de formatação. Medição de tokens por tarefa indisponível. Reutilizar o prompt e o estado de revisão existentes limita o próximo GREEN aos componentes de fechamento.

Decisão pendente: aprovação dos 14 casos, conforme a seção 9 do CODEX.md. Os outros quatro passos permanecem planejados, sem implementação nesta fase.
