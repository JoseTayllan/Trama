# Salvamento automático: especificação aprovada

29/10/2026. O responsável decidiu seguir com o produto e deixar o print no WSL como detalhe de baixa prioridade, pois a função funciona no Windows. Também escolheu **salvar automaticamente a própria página após uma pausa na digitação**, em vez de manter um rascunho separado.

## Comportamento proposto

Depois de dois segundos sem alterações, gravar a versão mais recente da página pelo fluxo existente. Digitar novamente reinicia essa pausa. Preservar a identidade, texto formatado, código, referências de imagens, legendas e ordem dos blocos. O botão Salvar continua disponível para gravar imediatamente.

Uma página nova e intocada não deve gerar gravação. Abrir uma página não deve ser tratado como edição. Se Salvar concluir antes do prazo, cancelar a gravação automática redundante.

Uma edição durante uma gravação pertence à próxima revisão: concluir o snapshot anterior não pode marcar a alteração mais nova como salva. Serializar as gravações e persistir a revisão restante após a pausa, quando a operação anterior terminar. Se o disco falhar, manter o conteúdo e o estado de alterações pendentes; uma edição posterior permite nova tentativa, sem loop automático infinito de falhas.

## Oito casos preparados

Veja [NotebookAutoSaveTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NotebookAutoSaveTests.cs):

1. Página vazia e intocada não é salva automaticamente.
2. Dois segundos de pausa salvam a página e preservam sua identidade.
3. Novas edições reiniciam a pausa e somente o conteúdo mais recente é gravado.
4. Texto com estilos, código e referência de imagem são preservados na ordem correta.
5. Salvar manualmente antes do prazo evita uma gravação automática duplicada.
6. Edição durante gravação pendente é salva depois, sem operações concorrentes.
7. Falha mantém o conteúdo, não causa repetição infinita e permite nova tentativa após editar.
8. Carregar outra página cancela a programação anterior e não provoca gravação da página carregada sem edição.

O último caso exercita a API do editor. A biblioteca deve continuar respeitando a confirmação de alterações pendentes antes de navegar; não autoriza descartar alterações silenciosamente.

## Relógio controlado nos testes

Os testes avançam um TimeProvider de teste para simular a passagem dos dois segundos, sem usar a velocidade do computador como relógio de negócio. O limite real de três segundos serve somente para detectar que a chamada esperada não aconteceu e encerrar o teste, não para aguardar a pausa de digitação.

Na etapa RED, foi acrescentada uma sobrecarga de construtor com `TimeProvider autoSaveClock` a NotebookEditorViewModel, mantendo intacta a assinatura anterior. Naquele momento, o relógio só era validado. Após aprovação, esse contrato passou a ativar o agendamento descrito no [estudo da implementação](23-salvamento-automatico-implementado.md).

As ações de persistência são controladas nesses oito casos. O GREEN completo também precisará comprovar banco real, composição, vínculo com o caderno, navegação, fechamento e feedback visual. Os testes novos não demonstram isso sozinhos.

## Plano de implementação após aprovação

1. Implementar agendamento com pausa reiniciável, revisão mais recente e serialização, sem alterar os oito casos aprovados.
2. Cancelar o trabalho pendente quando o editor for substituído/descartado; preservar as confirmações de navegação e fechamento.
3. Ativar pelo fluxo real do aplicativo e atualizar o indicador de salvamento. Preservar o botão Salvar e a atualização da biblioteca após sucesso.
4. Conferir gravação e reabertura com SQLite isolado, destinos após movimentação e falhas de I/O. Testes adicionais de novos contratos também passam por revisão antes de implementação.
5. Executar a suíte pertinente, conferir a interface, atualizar a documentação de uso e gerar publicação Windows para revisão.

Não é necessária migração de banco para o comportamento proposto: reutilizar o fluxo de gravação existente. Uma queda antes de uma gravação terminar ainda pode perder as últimas edições; isso não é recuperação integral de rascunhos nem histórico de versões. Um mecanismo separado para esses casos fica para etapa posterior.

## Estado atual

Os oito casos foram aprovados e a implementação passou os 312 testes. Este documento preserva a especificação; o [relatório RED](../reviews/031-autosave-red.md) registra o estado anterior e o [relatório GREEN](../reviews/032-autosave-green.md) contém as evidências atuais. A revisão humana do diff final permanece pendente.

Para estudar: por que usar pausa reiniciável? Qual revisão deve ser marcada como salva quando o usuário edita durante uma gravação? Por que uma falha não deve apagar a página nem iniciar um loop ilimitado? Por que cancelar um timer não substitui aguardar uma operação de persistência já iniciada?
