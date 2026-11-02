# Salvamento automático - testes RED para revisão

29/10/2026. O responsável escolheu salvar a própria página após uma pausa na digitação. A colagem no WSL fica em baixa prioridade por decisão explícita; não bloqueia esta etapa, já que funciona no exe Windows validado pelo responsável.

## Escopo e mudança mínima

Oito testes novos em [NotebookAutoSaveTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NotebookAutoSaveTests.cs), descritos no [material de estudo](../estudos/22-salvamento-automatico-especificacao.md). Só foi acrescentada uma sobrecarga de construtor com TimeProvider, que delega ao construtor existente e valida o relógio, para compilar o contrato mínimo da especificação. Não existe implementação do salvamento automático nesta entrega. A assinatura anterior foi preservada, inclusive para consumidores já compilados.

## RED observado

Comando no WSL, após sincronizar o espelho:

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --filter FullyQualifiedName~NotebookAutoSaveTests --logger trx --results-directory artifacts/autosave-red-final
```

Oito casos descobertos: cinco falham porque nenhuma chamada automática de persistência é feita ao avançar o relógio; três proteções existentes passam (página intocada, gravação manual sem duplicação e carregamento sem gravação). Build compila; não se trata de falha de descoberta ou infraestrutura.

A primeira rodada usou TimeoutException como sinal da chamada ausente. Antes de submeter os testes, a mensagem foi refinada para uma asserção explícita de ausência de gravação após avanço do relógio. A especificação de comportamento não mudou. Os testes ainda não foram aprovados.

Validação final pelo helper normal `bash tools/run-wsl.sh test`: build Release sem avisos/erros e formatação aprovados. A suíte descobriu 312 casos: 307 passaram e cinco falharam somente nos cenários novos de autosave. Portanto, os 304 anteriores continuam passando intactos. As cinco mensagens indicam explicitamente que nenhuma chamada de persistência automática foi observada após avançar o relógio. O resultado continua RED.

## Critérios para a futura implementação

Dois segundos sem edição; reiniciar o prazo a cada mudança; preservar estilos, código e imagens; não duplicar o botão Salvar; não sobrepor gravações; não limpar revisão mais nova; manter conteúdo em falhas e retomar na próxima edição, sem loop infinito. Cancelar programação antiga ao carregar outro documento. Integração com navegação, composição, SQLite e interface será conferida antes de considerar o recurso entregue.

Não modificar testes aprovados nem antecipar a implementação antes da aprovação explícita. O diff inclui testes, contrato mínimo e documentação. Nenhum dado pessoal foi acessado e nenhuma migração foi criada. Não considerar cinco falhas esperadas como GREEN.

## Riscos e custo

Principais riscos: gravação fora do caderno correto após navegação/movimentação, edição durante persistência, tarefas pendentes após troca de editor e perda de alterações antes da última gravação concluída. Autosave não substitui backup nem promete recuperação integral após queda.

Um agente, sem subagentes. Esforço moderado: exploração focada, duas rodadas focadas RED para melhorar as mensagens, conferência dos 304 anteriores e uma execução final completa após preservar a assinatura do construtor. Sem medição de tokens ou valor monetário. Reutilizar o relógio controlado e começar pelos oito casos na próxima rodada, deixando a suíte ampla para a validação final.
