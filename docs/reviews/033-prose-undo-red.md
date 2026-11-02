# Desfazer/refazer enriquecido - RED de 29/10/2026

## Solicitação e escopo

Autosave validado pelo responsável. Próxima solicitação: desfazer/refazer de texto e formatação. Os treze casos propostos usam o bloco de prosa em foco, atalhos e histórico de sessão, com preservação da gravação manual/automática. A implementação do aplicativo não foi alterada. O manual/PDF e seu gerador permaneceram intactos, conforme a preferência registrada pelo usuário.

## Execução focada

Após sincronizar o espelho WSL:

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --filter FullyQualifiedName~ProseUndoRedoTests --logger trx --results-directory artifacts/undo-red
```

Treze casos executados: dez falharam e três passaram. As falhas são asserções comportamentais: Undo retorna false após formatação; estilos apagados não reaparecem ao desfazer exclusão/substituição; desfazer a última formatação remove a digitação anterior; Ctrl+Z mantém estilos que deveria desfazer. O caso de autosave efetivamente salvou e depois falhou ao tentar desfazer a formatação, sem timeout de infraestrutura.

Proteções que passaram: nova digitação invalida o ramo de refazer, blocos mantêm pilhas textuais independentes e outra página inicia sem histórico da anterior.

Conferência completa por `bash tools/run-wsl.sh test`: build Release sem erros/avisos e formatação aprovados. Foram descobertos 325 casos: 315 passaram e dez falharam somente em ProseUndoRedoTests. Os 312 anteriores passaram intactos, inclusive autosave. Esse resultado continua RED, sem falha de infraestrutura.

## Critérios da implementação futura

Restaurar texto/estilos coerentes, manter ordem cronológica, não gerar novas ações ao aplicar undo/redo, preservar isolamento entre blocos/páginas e permitir que uma alteração desfeita seja salva novamente. Nenhuma implementação antes da aprovação explícita dos treze testes. Os casos anteriores e os novos aprovados devem permanecer intactos durante o GREEN.

O recorte não introduz histórico persistente nem histórico de operações estruturais da página. A conferência posterior deve observar agrupamento de digitação, consumo de memória, foco/seleção e compatibilidade com autosave. Não precisa alterar banco, migrações ou formato de backup.

## Revisão, segurança e custo

O diff para revisão contém apenas testes e documentação de desenvolvimento. Nenhum segredo ou dado pessoal foi adicionado; dados de teste são locais e sintéticos. Documentos operacionais e PDF não foram atualizados. O usuário confirmou a validação funcional do autosave; revisão técnica dos diffs e teste independente do colega são estados distintos.

Um agente, sem delegação. Esforço moderado: leitura focada do editor/API oficial, uma rodada RED e uma conferência completa. Sem medição de tokens ou custo monetário. Economia para o próximo ciclo: iniciar pelos treze casos e reutilizar o harness de autosave; repetir a suíte completa na entrega final ou por uma falha nova.

Veja a [especificação para estudo](../estudos/24-desfazer-refazer-especificacao.md).
