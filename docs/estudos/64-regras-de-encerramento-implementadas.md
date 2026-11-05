# 64 — Regras de encerramento e seletores da Agenda

Data: 04/11/2026. Implementação para revisão; armazenamento do encerramento real e aviso ao sair continuam pendentes.

## Regras implementadas

`CompleteAgendaTasks` recebe a seleção e o instante real de conclusão. Antes de escrever, copia a seleção, verifica cancelamento e valida todo o lote: não vazio, IDs únicos e válidos, snapshots válidos, estados Pendentes/Em andamento e datas planejadas até o dia local do instante recebido. Datas futuras e tarefas concluídas são rejeitadas. Não normaliza o horário recebido para outro offset nem altera o planejamento.

O caso de uso chama `IAgendaTaskCompletionRepository.CompleteAsync` uma vez com os IDs selecionados, o instante e o token. Só reporta Completed depois do await. Somente AgendaPersistenceException vira PersistenceFailed; cancelamento e erros inesperados propagam para a camada que coordena o fluxo.

O contrato exige um adaptador com atualização atômica e preservação dos campos planejados. Os testes desta fatia usam repositório controlado: não comprovam transação SQLite, conflito de edição, backup, durabilidade ou coordenação da janela. O caso de uso ainda não está registrado na composição nem é chamado ao fechar. Não existe aviso funcional ou conclusão automática nesta entrega.

## Por que usar data/hora real separada

Uma tarefa planejada ontem pode ser concluída hoje. Guardar apenas o horário de fim no planejamento produziria um intervalo incorreto ou apagaria o plano original. Esta camada entrega um DateTimeOffset ao adaptador futuro, preservando também o offset. Ela não infere início real nem duração trabalhada.

Como o instante é fornecido pelo chamador, a camada de fechamento futura deverá obtê-lo do relógio local ao confirmar a operação. O adaptador deverá revalidar os registros atuais dentro da transação. Uma seleção desatualizada não pode sobrescrever tarefas modificadas ou removidas.

## Refino de data e horários

`AgendaWorkspaceView.axaml` aplica estilos locais aos DatePickers e TimePickers, incluindo o filtro de dia: fundo da paleta, bordas arredondadas, altura de toque maior, divisórias discretas e destaque de foco/hover. O mês do planejamento usa nome completo. Largura do filtro de dia é explícita para não comprimir seus segmentos; os campos do formulário continuam na rolagem e o botão Salvar permanece fixo.

Os componentes nativos, bindings e handlers foram preservados. Não há novo parser de texto ou troca de formato persistido. Os nomes PART_FlyoutButton, PART_FirstPickerHost e PART_SecondPickerHost foram conferidos na árvore real do tema Avalonia 11.3.19; revisar esse acoplamento ao atualizar o tema. Os placeholders nativos vazios hour/minute ainda estão em inglês; esta fatia não substitui o template ou o comportamento de localização.

## Evidências

Os 17 casos aprovados permaneceram byte a byte intactos. Suíte completa: **657 aprovados, 117 unitários e 540 integrações**, zero falhas ou ignorados. Build Release sem avisos/erros e formatação passaram. Depois, houve ajuste exclusivamente visual de alinhamento/altura/espaçamento: build/formatação repetidos e os oito testes de navegação nativa passaram novamente. A suíte completa não foi repetida após esse último ajuste de estilo.

QA SQLite/Skia em perfil sintético conferiu janela ampla 1380×1000 e compacta 860×680, início sem fim pelo botão, limpeza, edição, foco por teclado, abertura dos popups e preservação de data/horário ao fechar o seletor sem confirmar alterações. Capturas reais foram inspecionadas. Não houve publicação nem execução nativa Windows nova nesta fatia.

```bash
bash tools/run-wsl.sh test
dotnet test tests/Trama.IntegrationTests -c Release --no-build --filter FullyQualifiedName~AgendaNavigationTests --logger trx --results-directory artifacts/agenda-picker-final --verbosity quiet
```

## Próximas etapas

Especificar a persistência do encerramento real, migração preservando cadernos/tarefas e recuperação por backup. Depois, integrar consulta de candidatas, seleção e diálogo com Encerrar tarefas e sair, Sair mantendo abertas e Cancelar. Resolver todas as decisões que ainda podem cancelar a saída antes de concluir tarefas. Exclusão com recuperação e semana visual continuam no roadmap.

Para estudar: por que aguardar um repositório não comprova atomicidade? Por que a elegibilidade usa DateTimeOffset.DateTime e não UtcDateTime? Que riscos surgem ao aplicar estilos a partes internas de um controle nativo?

[Especificação e histórico RED](63-encerramento-e-refino-da-agenda.md) · [Relatório](../reviews/071-agenda-closure-rules-green.md).
