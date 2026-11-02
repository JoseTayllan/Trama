# Navegação da biblioteca — especificação aprovada

Este capítulo registra o planejamento aprovado e a evidência RED. A implementação posterior passou com os 157 testes intactos. Para estudar o uso e o código atuais, leia [Biblioteca visual implementada](14-biblioteca-visual-implementada.md).

## Experiência prevista

A biblioteca visual será organizada em espaços, cadernos e páginas, conectada ao editor contínuo já existente. O usuário poderá criar um espaço como Curso C#, criar Fundamentos dentro dele e abrir suas páginas pela lista. Não organizadas mostrará os documentos ainda sem caderno.

Criar uma página dentro de um caderno deverá manter esse destino até salvar. Depois do salvamento, a lista precisa refletir a nova página. Fechar a sessão e reabrir a biblioteca deverá permitir recuperar o mesmo documento pelo catálogo.

Na revisão RED havia 126 casos aprovados e 31 novos falhando nos contratos vazios. Depois da aprovação, os 31 passaram e a nova View foi conectada à janela.

## Alterações não salvas

HasUnsavedChanges distinguirá uma página limpa de um rascunho alterado. Uma página nova vazia começa limpa. Alterações de título, texto, linguagem, inserção, remoção, movimentação e importação de imagem precisam marcar a página como alterada.

Salvar com sucesso limpa apenas as alterações incluídas no snapshot enviado. Se uma edição chegar depois desse snapshot e antes de terminar a gravação, ela continua pendente. Falha ao salvar não limpa o rascunho. Carregar uma página com sucesso estabelece uma nova referência limpa.

Ao trocar de página, de caderno, abrir Não organizadas ou criar outra página, haverá três escolhas:

| Escolha | Resultado esperado |
| --- | --- |
| Salvar e continuar | Aguardar a gravação; só navegar se ela confirmar sucesso e não restarem alterações pendentes |
| Descartar alterações | Prosseguir sem gravar o rascunho anterior |
| Cancelar | Manter página, seleção e conteúdo atuais, sem carregar o destino ou salvar |

Se o destino não puder ser aberto, o editor atual deve permanecer disponível mesmo que a escolha anterior tenha sido descartar. Uma gravação em andamento impede outra navegação antes de abrir a confirmação. Os testes também exigem que a opção Salvar aguarde o resultado real antes de carregar o destino.

Este contrato protege a navegação interna. Ainda não especifica a confirmação ao fechar a janela do aplicativo, restauração após falha do processo ou autosave.

## Separação das responsabilidades

[ILibraryActions](../../src/Trama.Application/Knowledge/ILibraryActions.cs) é a entrada da aplicação para a biblioteca. Estende o port do editor, acrescentando criação de espaços/cadernos, listagens e salvamento com destino. Recebe nomes, em vez de exigir que o ViewModel construa entidades de persistência. Nomes vazios devem ser rejeitados antes de gravar.

[LibraryViewModel](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs) coordenará seleção, criação, listagens e o editor atual. O port IUnsavedChangesPrompt pertence à apresentação: um adaptador visual mostra a decisão, enquanto os testes devolvem escolhas controladas. Cancelar é a escolha segura quando a confirmação não está disponível; não descartar silenciosamente.

[SqliteLibraryActions](../../src/Trama.Infrastructure/Persistence/SqliteLibraryActions.cs) começou como contrato vazio. A implementação posterior abre um contexto por operação e delega regras aos casos de uso de aplicação. O repositório de catálogo não é injetado diretamente no ViewModel.

A [composição do desktop](../../src/Trama.Desktop/DesktopComposition.cs) ganhou um parâmetro opcional de confirmação na preparação do RED. No GREEN, ele passou a registrar o prompt e o novo ViewModel, preservando as chamadas anteriores. A [LibraryWorkspaceView](../../src/Trama.Desktop/Views/LibraryWorkspaceView.axaml) foi preenchida e conectada à janela.

## Salvamento e associação ao caderno

SaveInNotebookAsync precisa confirmar conteúdo e associação na mesma transação do SQLite. Um destino inexistente não pode deixar uma página nova pela metade nem alterar o título ou a localização anterior de uma página existente. Os dois casos têm testes com banco real temporário.

Os repositórios usados nessa coordenação precisam respeitar quem abriu a transação. O adaptador atual de páginas inicia sua própria transação; a implementação seguinte deverá permitir a coordenação externa sem confirmar ou descartar a transação de outro chamador. Preservar os testes e o comportamento do salvamento independente já aprovado.

O destino pertence à página que está sendo editada. Não deslocar uma página para outro caderno só porque o usuário consultou uma lista diferente. Neste primeiro fluxo, a seleção deve passar pela proteção do rascunho antes de substituir a página atual.

Importação de arquivo e gravação da página continuam separadas: não existe transação única entre SQLite e sistema de arquivos. A coleta de anexos órfãos ainda não está incluída.

## O que os testes verificam

- [EditorUnsavedChangesTests](../../tests/Trama.IntegrationTests/Knowledge/EditorUnsavedChangesTests.cs): dez casos de estado limpo/alterado, mudanças nos blocos, falha de salvamento, carregamento e edição posterior ao snapshot.
- [LibraryNavigationTests](../../tests/Trama.IntegrationTests/Knowledge/LibraryNavigationTests.cs): 15 casos de inicialização, criação, listagem, abertura, confirmação, falhas, operação em andamento e atualização da lista após salvar.
- [LibraryWorkspaceTests](../../tests/Trama.IntegrationTests/Knowledge/LibraryWorkspaceTests.cs): seis casos para bindings da View, composição real entre sessões, nomes inválidos e rollback de conteúdo/localização.
- [Doubles controlados](../../tests/Trama.IntegrationTests/Knowledge/LibraryNavigationTestDoubles.cs): biblioteca em memória e escolhas da confirmação, sem diálogo real ou dados pessoais.

Resultado da especificação RED: **157 casos executados, 126 aprovados e 31 novos falhando**, com compilação concluída e formatação aprovada. As falhas eram de comportamentos ausentes. Veja o [histórico RED](../reviews/015-library-navigation-red.md) e o [GREEN posterior](../reviews/016-library-navigation-green.md), com os 157 aprovados.

## Limites e próximo passo

Os testes ainda não exercitam a aparência de um diálogo nativo, foco, teclado, solicitações concorrentes de listagem, renomeação, exclusão ou mover páginas por comandos visuais. O salvamento com destino é testado em falha de referência ao caderno; não simula toda interrupção possível no meio de uma transação.

A criação com nomes canônicos aparece nos doubles e no fluxo do ViewModel; a validação real com banco exige rejeição de nomes em branco. Os detalhes visuais, ordenação por idioma e política de tamanhos da barra lateral ficam para a implementação e inspeção da tela.

Após a aprovação, o port, a coordenação transacional, o rastreamento de alterações e a biblioteca visual foram implementados sem modificar os testes. A navegação e o diálogo também foram conferidos com dados sintéticos e SQLite isolado.

## Para estudar

Por que salvar uma página e atribuí-la ao caderno precisam ser uma operação consistente? Como distinguir o snapshot salvo de uma edição mais recente? Por que uma escolha de descarte não exige destruir o editor antes de abrir o destino? Qual camada deve exibir a confirmação e qual deve executar a gravação?
