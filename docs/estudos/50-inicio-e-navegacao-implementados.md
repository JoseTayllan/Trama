# 50 · Tela Início e navegação implementadas

01/11/2026. Os 17 casos foram aprovados antes da implementação e permaneceram intactos. Esta entrega disponibiliza Início e Cadernos para revisão do diff e validação de uso. Agenda ainda não possui fluxo funcional.

## O que mudou

A janela abre em Início depois de preparar o armazenamento. A barra superior oferece Início, Cadernos e acesso global ao Backup. Agenda aparece desabilitada e identificada como uma etapa em desenvolvimento.

O cartão Cadernos abre a biblioteca existente. Quando há uma página aberta ou um rascunho na sessão, Início também oferece **Continuar página**, com título, localização e indicação de alterações pendentes. Esse atalho retoma a mesma página em memória; não consulta outra versão do banco nem salva por causa do clique.

O histórico de última página entre reinicializações, páginas recentes e resumos de agenda não foram implementados nesta fatia. Os horários e indicadores do protótipo eram exemplos visuais; a interface atual não publica dados fictícios.

## Como a navegação preserva o editor

`MainWindowViewModel.Navigation.cs` contém os comandos e o estado de seleção do módulo. `Workspace` continua apontando para a mesma biblioteca. Alternar módulos muda somente as propriedades de visibilidade; não emite uma troca de conteúdo nem recria o editor.

O `ContentControl` que hospeda os Cadernos permanece na árvore visual enquanto Início aparece em uma área irmã. Assim, não basta preservar o texto do ViewModel: a instância do AvaloniaEdit também mantém seleção, rolagem e pilha de desfazer.

A navegação observa as notificações existentes da biblioteca para atualizar comandos, título e estado do atalho. Ao substituir a biblioteca, remove a assinatura da instância anterior e volta para Início. Antes de o perfil estar pronto, os comandos de módulo permanecem indisponíveis.

## Salvamento, fechamento e backup

O autosave continua funcionando quando Cadernos está oculto. A navegação não introduz gravação adicional, confirmação de abandono ou limpeza de histórico. Salvar e desfazer continuam com as regras existentes.

O fechamento de `MainWindow` já consulta `Library.RequestCloseAsync`, independentemente do conteúdo visível. Esse fluxo foi preservado: Salvar, Descartar e Cancelar também protegem o caderno oculto atrás de Início. Os testes novos cobrem as três decisões.

Backup usa o diálogo e os bloqueios anteriores. Na biblioteca hospedada pela janela principal, `UseModuleShell` oculta a marca duplicada e o botão de backup da lateral, pois as ações estão na barra global. O uso independente de `LibraryWorkspaceView` mantém sua apresentação anterior.

## Arquivos principais

| Arquivo | Responsabilidade |
| --- | --- |
| `MainWindowViewModel.cs` | Contratos existentes e ligação com a biblioteca |
| `MainWindowViewModel.Navigation.cs` | Seleção de módulo, comandos e informações da página em andamento |
| `MainWindow.axaml` | Barra global, Início e host visual preservado |
| `LibraryWorkspaceView.axaml` e code-behind | Ajuste de apresentação ao ser hospedado na janela principal |

Não houve alteração de tabelas, migrações, dependências ou versões de página/backup. Os estados de navegação são transitórios de interface, sem novas gravações no perfil.

## Verificação

Suíte completa no WSL: **529 aprovados**, sendo 77 unitários e 452 integrações, nenhum ignorado. Os 17 casos novos e os 512 anteriores foram preservados. Build Release sem avisos/erros e verificação de formatação passaram.

O roteiro SQLite/Skia conferiu Início inicial, ida e volta ao editor, rascunho, seleção e histórico mantidos, autosave com Início visível, continuidade da página, backup e fechamento. Capturas em 1380×1000 e 860×680 foram usadas para inspecionar a composição real.

O pacote Windows self-contained foi preparado em uma pasta isolada com um perfil fictício. A execução interativa e a aprovação estética do responsável permanecem pendentes. Testes headless e capturas não substituem essa validação.

## Como validar

1. Abrir o pacote de revisão e confirmar a entrada em Início.
2. Entrar em Cadernos, abrir uma página e editar um trecho.
3. Voltar a Início e usar Continuar página; conferir seleção e desfazer/refazer.
4. Repetir em janela compacta e abrir Backup a partir de Início.
5. Fechar pela tela Início com uma edição pendente e verificar as opções de proteção.

O launcher `Testar-Inicio.cmd` usa um perfil fictício separado. O comando de execução WSL e os procedimentos normais permanecem nos [guias de operação](../operacao/README.md).

Detalhes: [relatório 057](../reviews/057-home-navigation-green.md). Especificação aprovada: [capítulo 49](49-inicio-navegacao-especificacao.md). Após validar esta fatia, o próximo passo é especificar as tarefas da Agenda e sua persistência. Manual/PDF e README de apresentação não foram alterados.
