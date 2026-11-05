# 71 — Aviso de tarefas e fechamento coordenado

**Aceite de uso em 05/11/2026:** o responsável confirmou “fluxo validado”. Aviso e fechamento coordenado validados no uso. A confirmação não especifica ambiente e não equivale a aceite estético ou revisão formal do diff. Nenhuma mudança de código ou nova execução de testes nesta atualização; resultados abaixo são históricos da implementação.

Data: 04/11/2026. Implementação disponível para revisão do diff, do visual e validação de uso.

## O fluxo disponível

Ao fechar o Trama, o coordenador resolve primeiro o rascunho da Agenda e depois o dos Cadernos. Só então consulta tarefas Pendentes/Em andamento até o dia local, incluindo dias anteriores e tarefas recém-salvas nessa confirmação. Sem candidatas, permite sair sem aviso vazio.

Com candidatas, AgendaCompletionDialog mostra título, descrição, dia planejado, categoria e estado. Nada começa selecionado. Selecionar todas e limpar seleção afetam somente as caixas. As ações são Encerrar selecionadas e sair, Sair mantendo abertas e Cancelar. Concluir exige seleção válida; Cancelar é o padrão de teclado e o resultado do X. A decisão copia IDs, não títulos, para permitir tarefas de mesmo nome sem ambiguidade.

## Por que separar preparar e fechar

O antigo LibraryViewModel.RequestCloseAsync continua com sua semântica anterior para consumidores existentes. O novo PrepareCloseAsync resolve alterações sem descartar o editor. ApplicationCloseCoordinator mantém o mesmo editor vivo até a janela fechar de fato; se uma confirmação posterior cancelar ou a conclusão falhar, os rascunhos continuam editáveis.

O coordenador é descartável e mantém uma pausa de autosave durante todas as confirmações. Ao cancelar, libera a pausa e o autosave retoma. Ao aprovar, segura a pausa até o OnClosed da janela: primeiro descarta o editor, depois libera o coordenador. Essa ordem impede salvar automaticamente um rascunho que o usuário descartou durante o pequeno intervalo entre aprovação e fechamento efetivo.

MainWindow usa exclusivamente o novo caminho quando Closing está configurado, bloqueia o conteúdo enquanto aguarda e impede reentrada. Não chama de novo as confirmações antigas depois de concluir tarefas. A inicialização real do App cria o coordenador com biblioteca, Agenda, serviço do perfil, prompt da janela e TimeProvider.System. Consumidores sem coordenador preservam o caminho anterior.

## Dados e falhas

Depois da confirmação de seleção, o coordenador captura a data/hora local real e reutiliza o serviço transacional aprovado. IDs vazios, repetidos ou fora das candidatas são rejeitados; tarefas alteradas externamente são revalidadas no SQLite. Falha de consulta/gravação mantém a janela aberta com feedback. Planejamento não é sobrescrito: o Fim do formulário continua planejado; o encerramento real fica em LastCompletedAt e não é usado para preencher esse campo.

O fluxo cobre saída normal. Queda de energia, término forçado do processo e encerramento imposto pelo sistema não garantem conclusão automática. Se outro manipulador externo impedir o fechamento depois de uma conclusão confirmada e gravada, essa conclusão não é revertida; o fluxo interno evita confirmações canceláveis posteriores à escrita.

## Verificações

Os 35 casos aprovados permaneceram byte a byte intactos. Passaram primeiro focados e depois na suíte completa: **738 testes — 117 unitários e 621 integrações**, zero falhas ou ignorados. Build Release sem avisos/erros e formatação passaram. Após um ajuste apenas textual de singular/plural do contador, os 35 casos e a formatação passaram novamente; a suíte completa não foi repetida depois desse texto.

QA SQLite/Skia com diálogo e prompts reais conferiu dez candidatas, exclusão de tarefa futura, seleção inicialmente vazia, cancelamento, rolagem, janela 560×420, descarte de rascunho com espera além do autosave, conclusão somente das selecionadas e fechamento real sem gravar o descarte. Capturas inspecionadas. Uma captura falhou por compilações Avalonia concorrentes; reconstrução e execução sequenciais corrigiram o artefato. Não foi necessário alterar código funcional ou testes para essa recuperação.

Pacote Windows win-x64 self-contained publicado separadamente com cópia de perfil fictício já fechado e lançador Testar-Fechamento.cmd. Não substitui a publicação normal nem usa dados pessoais. O executável novo não foi aberto nativamente pelo agente; validação Windows e aceite estético/do diff continuam com o responsável. Manual/PDF e Git permanecem sob suas instruções anteriores.

## Para estudar

1. Por que aprovar saída não equivale a já ter fechado a janela?
2. Por que liberar o autosave no finally de toda tentativa pode violar um descarte aprovado?
3. Por que o instante real deve ser obtido depois da decisão, inclusive quando passa da meia-noite?

Próximas funcionalidades: exclusão de tarefas com confirmação/recuperação e visão semanal. O módulo Agenda ainda não está encerrado.

[Especificação aprovada](70-fechamento-coordenado-especificacao.md) · [Relatório](../reviews/078-agenda-close-workflow-green.md).
