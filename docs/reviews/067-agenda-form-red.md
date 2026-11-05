# Agenda — formulário e navegação em RED

**Atualização de 03/11/2026:** os 34 casos foram aprovados e implementados, preservados. O ajuste dos dois cronômetros antigos também recebeu aprovação explícita. Suíte final: 632 aprovados. [Entrega GREEN](068-agenda-form-green.md). O texto abaixo registra a especificação RED histórica.

Data: 03/11/2026. Aguardando aprovação explícita dos 34 casos.

## Escopo

AgendaViewModel e propriedade Agenda no host são contratos mínimos compiláveis. Nenhum comportamento de edição, XAML, registro de ViewModel ou mudança da inicialização foi implementado. O armazenamento e o serviço aprovados continuam intactos.

34 casos compilados: 26 de fluxo de edição com serviço controlado e oito Avalonia/headless com SQLite fictício. 32 falham e dois passam. 31 falhas ocorrem em InitializeAsync ainda não implementado; uma constata a navegação da Agenda desabilitada. As asserções finais de gravação, controles e fechamento ainda não foram alcançadas. Não são erros de compilação e não demonstram recurso concluído.

## Decisões para revisão

Lista por dia e formulário com os campos persistidos. Preservar dia local e identidade. Alternar módulos mantém rascunhos; nova tarefa, outra tarefa e mudança de dia resolvem alterações antes de substituir conteúdo. Falha de consulta mantém o estado anterior.

Salvar não muda o filtro do dia. Falha ao atualizar a lista depois de gravar não pode gerar ID novo. Gravação pendente bloqueia reentrada e fechamento; uma revisão mais recente do conteúdo permanece pendente.

Fechar considera Agenda oculta e Cadernos. Se uma decisão posterior cancelar, os dois rascunhos continuam disponíveis e editáveis. Confirmar descarte não apaga o rascunho antes de todo o fechamento ser aprovado. Manter o controle nativo dos Cadernos, seleção, histórico e autosave.

## Execução

```sh
dotnet test tests/Trama.IntegrationTests -c Release --no-restore --filter 'FullyQualifiedName~AgendaFormTests|FullyQualifiedName~AgendaNavigationTests' --logger trx --results-directory artifacts/agenda-form-red/review --verbosity quiet
dotnet test tests/Trama.UnitTests -c Release --no-restore --logger trx --results-directory artifacts/agenda-form-red/unit --verbosity quiet
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter 'FullyQualifiedName~HomeModuleNavigationTests|FullyQualifiedName~AgendaServiceTests|FullyQualifiedName~AgendaStorageTests' --logger trx --results-directory artifacts/agenda-form-red/baseline --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Nesta execução da base, 101 unitários e 55 das 56 integrações selecionadas passaram. O caso anterior Autosave_ContinuesWithTheNotebookHiddenBehindHome falhou por prazo; sua repetição isolada fica registrada junto aos artefatos. Não apresentar a base como 157 aprovados juntos. A suíte histórica de 598 aprovados pertence à entrega do serviço e não foi repetida integralmente neste RED.

A repetição isolada também falhou por prazo. A causa continua em investigação e não é atribuída ao novo formulário, ainda não conectado ao aplicativo. A verificação de formatação final passou. Esse resultado permanece pendente para a validação GREEN; nenhum teste ou mecanismo anterior de autosave foi alterado.

Especificação ajustada antes da aprovação para conferir também data vazia, falha de consulta e ligação da lista. Testes anteriores permanecem intactos. Não alterar testes aprovados para obter GREEN.

## Limites e próximos passos

A seção 9 do CODEX exige aprovação antes da implementação. Depois: ViewModel, vista, navegação condicional, composição/inicialização e coordenação de fechamento; conferir regressões, capturas amplas/compactas e pacote Windows isolado. Agenda continua desabilitada nesta especificação.

Sem migração, dependência, perfil pessoal, manual/PDF/README ou Git alterado. O painel dos Cadernos fica para depois da Agenda. A pendência de timeout anterior deve ser investigada na validação seguinte, sem apagá-la do histórico.

Esforço moderado, sem subagentes. Cobertura concentrada na edição e nas fronteiras de navegação/fechamento; reutilizar os testes existentes de armazenamento para reduzir duplicação.

Estudo: [capítulo 59](../estudos/59-agenda-formulario-especificacao.md).
