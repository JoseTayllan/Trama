# Cinco próximos passos e proteção ao fechar

## Plano aprovado pelo usuário

| Ordem | Entrega | Estado atual |
| --- | --- | --- |
| 1 | Proteger alterações ao fechar o aplicativo | Implementado após aprovação dos 14 testes; GREEN com 171 casos, disponível para revisão do diff |
| 2 | Renomear espaços, cadernos e páginas; mover páginas entre cadernos pela interface | Implementado após aprovação dos 29 casos; GREEN com 200 testes. Ver [capítulo 16](16-gestao-da-biblioteca.md) |
| 3 | Buscar por título/conteúdo e organizar com tags | Implementado após aprovação dos 30 casos; GREEN com 230 testes. Ver [capítulo 17](17-busca-e-tags.md) |
| 4 | Backup consistente de banco e imagens, com restauração comprovada | Implementado após aprovação; GREEN com 270 testes, incluindo correção de tags. Ver [capítulo 18](18-backup-e-restauracao.md) |
| 5 | Colar prints, arrastar imagens e melhorar a formatação do texto | Implementado após aprovação; validação final impedida por política Windows. Ver [capítulo 19](19-imagens-e-formatacao.md) |

Cada entrega segue especificação, testes RED, revisão, implementação e verificação. Não anunciar os cinco passos como concluídos enquanto existirem itens pendentes. Agenda e assistente local continuam posteriores a essa base.

## Primeiro passo: fechamento seguro

Uma página limpa pode permitir fechar sem confirmação. Havendo alterações, reutilizar Salvar e continuar, Descartar ou Cancelar. Cancelar mantém janela, editor e seleção. Descartar permite encerrar sem escrever, mas a decisão de fechamento não precisa limpar o editor antecipadamente.

Salvar precisa aguardar a gravação da página no caderno atual. Falha de validação ou persistência impede encerrar e mantém o conteúdo. Se uma alteração chegar depois do snapshot enviado, ela continua pendente e também impede o fechamento.

Quando uma gravação já está em andamento, bloquear a tentativa de fechar sem iniciar outra confirmação; permitir tentar novamente depois. Pedidos repetidos enquanto uma decisão está pendente não podem abrir múltiplos diálogos ou duplicar a gravação.

A janela deve cancelar o fechamento inicial enquanto aguarda a decisão. Seu conteúdo fica desabilitado durante a operação e volta a funcionar se o usuário cancelar. Somente uma autorização positiva pode permitir o fechamento efetivo e o evento Closed. Isso mantém o encerramento dos serviços no fluxo existente, depois de fechar de verdade.

## Implementação e evidências

[LibraryViewModel.RequestCloseAsync](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs) agora reutiliza o coordenador de decisões da navegação. Ele bloqueia pedidos quando a biblioteca/editor está ocupado, consulta alterações pendentes, aguarda o salvamento quando solicitado e devolve autorização sem substituir nem limpar o editor.

[MainWindow](../../src/Trama.Desktop/Views/MainWindow.axaml.cs) cancela imediatamente a primeira tentativa de fechar. Enquanto aguarda a decisão, bloqueia o conteúdo da janela e ignora novos pedidos de confirmação. O diálogo é outra janela e continua utilizável. Cancelar ou falhar restaura o estado anterior de edição.

Depois de uma autorização positiva, a janela agenda o fechamento no Dispatcher, para terminar o evento cancelado antes de fechar novamente. Uma autorização de uso único permite essa segunda chamada sem repetir o prompt. O conteúdo permanece bloqueado até ela executar, evitando aceitar novas edições entre a autorização e o encerramento.

O encerramento dos serviços continua ligado ao Exit da aplicação. Como Closed só ocorre depois da autorização, a confirmação ou gravação pendente não aciona antecipadamente esse encerramento. O teste da janela verifica o evento Closed; a revisão também conferiu a ligação já existente em App.

Os [14 casos](../../tests/Trama.IntegrationTests/Knowledge/WindowClosingTests.cs) incluem onze cenários do coordenador e três da janela Avalonia em modo headless. Eles reutilizam os doubles de navegação aprovados, sem alterá-los e sem acessar dados pessoais.

Resultado final: **171 casos aprovados: 36 unitários e 135 de integração**, sem alterar os testes aprovados. Build com zero avisos e zero erros; formatação aprovada. O [RED histórico](../reviews/017-window-close-red.md) registrou 157 aprovados e 14 falhas antes da implementação.

Veja o [relatório GREEN](../reviews/018-window-close-green.md) para comandos, resultados e limites. O salvamento continua manual, mas o fechamento normal da janela agora consulta alterações pendentes.

Uma conferência externa usou o diálogo do aplicativo com SQLite e dados sintéticos isolados. Verificou Cancelar, fechar o próprio diálogo, pedidos repetidos, Salvar e reabrir o conteúdo em outra sessão, Descartar sem gravação, falha real de validação e fechamento de página limpa. O diálogo permaneceu habilitado mesmo com o conteúdo principal bloqueado. Esse roteiro não foi incorporado à suíte e não acessou o banco pessoal.

## Critérios dos próximos passos

No passo 2, validar nomes e destinos, preservar identidades e conteúdo e manter a seleção coerente após renomear ou mover. Exclusão física não foi acrescentada a esse escopo.

No passo 3, proteger busca e filtros contra resultados inconsistentes e definir como tags complementam espaços/cadernos. O comportamento e a persistência serão especificados antes de escolher uma estratégia de indexação.

No passo 4, incluir banco, metadados de organização, anexos e versão do formato. Demonstrar restauração em perfil isolado; cópia de arquivos sem teste de recuperação não comprova backup válido.

No passo 5, tratar entrada por clipboard/arrastar arquivos, erros de importação e compatibilidade das páginas existentes. Formatação e novas dependências exigirão avaliação própria.

## Limites da primeira entrega

A proteção proposta cobre fechamento normal da janela principal. Não é autosave nem recuperação após encerramento forçado, queda de energia ou falha do processo. O teste headless não substitui conferência interativa do botão de fechar e dos atalhos do Windows. Não há novas migrações, pacotes ou alterações nos testes anteriores.
