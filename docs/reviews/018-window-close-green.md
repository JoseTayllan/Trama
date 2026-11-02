# Proteção ao fechar — GREEN

Data: 28 de outubro de 2026. Primeiro dos cinco passos implementado após aprovação dos 14 casos do [RED](017-window-close-red.md), disponível para revisão humana do diff.

## Resultado

O fechamento normal da janela principal agora consulta alterações pendentes. Cancelar mantém janela e rascunho; Descartar autoriza encerrar sem gravar; Salvar espera a persistência no caderno atual e impede encerrar se houver erro ou nova edição após o snapshot.

LibraryViewModel reutiliza o coordenador existente. MainWindow cancela o evento inicial, bloqueia o conteúdo, evita confirmações concorrentes e agenda uma segunda chamada autorizada ao fechamento. Se a tentativa é negada, restaura a edição. O diálogo permanece utilizável por estar fora do conteúdo bloqueado.

Não houve nova dependência, migração, alteração nos testes aprovados ou mudança no fluxo de persistência. Os quatro passos seguintes permanecem pendentes.

## Verificação executada

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=window-close-green" --results-directory artifacts/window-close-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**171 testes aprovados: 36 unitários e 135 de integração, sem ignorados.** Build com zero avisos e zero erros; formatação aprovada. Nenhum dos 14 casos aprovados nem dos testes anteriores foi alterado. Evidências TRX em `artifacts/window-close-green/results`. A CI remota não foi executada.

Os testes cobrem página limpa, escolhas de confirmação, validação/persistência com falha, espera pela gravação, edição posterior ao snapshot, pedidos repetidos, tentativa durante gravação já iniciada, falha no prompt e eventos de fechamento reais em janela headless.

## Conferência adicional e revisão

Um roteiro externo com contêiner real, renderização Skia, diálogo do aplicativo e SQLite isolado verificou Cancelar, fechar o diálogo sem escolher, repetição de pedidos, salvar e recuperar em outra sessão, descartar sem gravar, falha de validação real e fechamento de página limpa. O diálogo permaneceu efetivamente habilitado; Closed ocorreu uma única vez ao confirmar o encerramento. Os dados eram sintéticos e o banco pessoal não foi aberto.

A revisão conferiu o bloqueio até o fechamento enfileirado, autorização de uso único, restauração da edição e preservação do editor ao negar a tentativa. Os serviços continuam sendo encerrados pelo Exit existente, posterior ao fechamento efetivo; não foi adicionado descarte antecipado ao evento Closing.

Esse roteiro não integra a suíte automatizada do produto. Botão nativo de fechar, atalhos, leitores de tela e escala do Windows ainda exigem conferência interativa. O funcionamento do diálogo foi exercitado em modo headless, não por automação de uma sessão nativa do Windows.

## Limites materiais

A proteção cobre o fechamento normal da janela da biblioteca. Não é autosave, recuperação após queda de energia, falha do processo ou encerramento forçado. Se outra operação já está em andamento, a tentativa de fechar é bloqueada e precisa ser repetida quando terminar; não há fechamento automático posterior.

Antes da biblioteca ser preparada não há editor interativo e a janela pode fechar diretamente. O caminho legado sem Library continua preservado para compatibilidade e não recebeu um coordenador de fechamento próprio nesta fatia.

## Documentação e esforço

Plano dos cinco passos, README, CODEX, roadmap e material de estudo foram atualizados. Um agente, sem delegação ou dependências novas; uma execução completa da suíte, build/formatação e um roteiro externo de QA. Medição de tokens por tarefa indisponível. O passo 2 deve focar em renomeação e movimentação, reutilizando a biblioteca atual.
