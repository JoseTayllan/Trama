# Gestão da biblioteca: renomear e mover

28 de outubro de 2026. Passo 2 implementado após aprovação dos 29 testes. Os 200 casos passam sem modificar os testes aprovados; o diff está disponível para revisão.

## Como usar e comportamento implementado

Selecione um espaço/caderno e use **Renomear** ao lado do título da seção na barra lateral. Na página aberta, use **Renomear página** ou **Mover para…** acima do editor. O diálogo de destino mostra `Espaço / Caderno`, incluindo cadernos de outros espaços. Sem outros cadernos, ele orienta a criar um destino; confirmar exige selecionar um item. Fechar o diálogo ou cancelar não altera dados.

Renomear espaços, cadernos e páginas mantendo seus identificadores. Remover espaços externos do novo título e rejeitar títulos em branco. Não mudar textos, código, ordem dos blocos, referências de imagens ou localização ao renomear. Nomes iguais continuam permitidos: a identidade é o GUID.

Mover uma página salva para um caderno, inclusive em outro espaço, sem duplicar seu conteúdo. Após o sucesso, abrir a mesma página no destino. Edições e salvamentos posteriores devem continuar nesse destino. Páginas antigas e páginas não organizadas também podem ser movidas. Remoção, reorganização dos próprios cadernos e retorno para Não organizadas ficam fora desta entrega.

Renomear o espaço ou caderno não abandona a página: preservar o editor e qualquer rascunho sem confirmação. Renomear a página aberta ou movê-la exige resolver as alterações pendentes: salvar, descartar ou cancelar. Salvar deve terminar com sucesso antes da operação; descartar usa a versão persistida somente após a operação bem-sucedida. Se a mutação falhar, manter o rascunho e a seleção disponíveis. Uma página nova precisa ser salva primeiro; seu título pode ser editado no campo existente antes disso.

Durante as operações, bloquear edição e solicitações concorrentes. Mensagens ao usuário não devem revelar detalhes internos de exceções.

## Arquivos e limites

- [Contrato de gestão](../../src/Trama.Application/Knowledge/ILibraryManagementActions.cs): novo port, separado de ILibraryActions para manter compatibilidade dos consumidores existentes.
- [Adaptador SQLite](../../src/Trama.Infrastructure/Persistence/SqliteLibraryManagement.cs): contexto novo por operação; renomeação modifica somente Title; movimentação reutiliza PlacePageAsync e sua transação.
- [Coordenador visual](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs): confirma o rascunho, prepara leituras antes da escrita e publica a seleção após sucesso.
- [Composição](../../src/Trama.Desktop/DesktopComposition.cs): registra o adaptador de gestão. O argumento opcional no ViewModel mantém compatibilidade dos consumidores antigos; sem o port, as ações ficam desabilitadas.
- [Biblioteca visual](../../src/Trama.Desktop/Views/LibraryWorkspaceView.axaml): ações nos cabeçalhos, conectadas aos diálogos [Renomear](../../src/Trama.Desktop/Views/RenameEntryDialog.axaml) e [Mover](../../src/Trama.Desktop/Views/MovePageDialog.axaml).
- [Testes de persistência](../../tests/Trama.IntegrationTests/Knowledge/LibraryManagementStorageTests.cs): 15 casos com SQLite temporário e leitura por novos contextos.
- [Testes de coordenação](../../tests/Trama.IntegrationTests/Knowledge/LibraryManagementWorkflowTests.cs): 14 casos com ações controladas e o ViewModel real.

O port expressa o que a aplicação precisa; o adaptador SQLite implementa como persistir. As Views apresentam os diálogos usando cores semânticas existentes. O code-behind controla janelas, seleção e prevenção de diálogos repetidos; regras e decisões de persistência ficam no ViewModel e nos contratos da aplicação. A conferência externa com Avalonia.Headless exercitou os botões reais, os diálogos e a composição com SQLite; não usou dados pessoais.

Não é necessário mudar o esquema para atualizar títulos e associações existentes. O leitor prioriza páginas estruturadas quando existe também uma nota antiga com o mesmo ID; renomear a página atual deve manter essa prioridade e preservar a nota histórica.

## Riscos que orientam a implementação

O editor captura o caderno ao ser criado. Ao mover, o coordenador prepara um novo editor com o ID do destino e o conteúdo persistido. Somente depois da movimentação bem-sucedida ele substitui o editor e a seleção. Assim o próximo salvamento não devolve a página ao caderno anterior. O teste Move_DiscardUsesStoredContentAndSubsequentSaveKeepsDestination protege esse caso.

Para evitar uma leitura falhar depois de uma escrita confirmada, o coordenador lê previamente o catálogo, as páginas do destino e o conteúdo. Prepara o editor e a lista em memória antes da mutação. Após a escrita, aplica o estado preparado sem outra leitura de banco. Renomear espaço/caderno atualiza os registros locais preservando o mesmo editor. Os contextos de banco são descartados por operação; o aplicativo continua voltado ao uso local individual, sem coordenação entre múltiplas instâncias editando o mesmo banco.

NotebookEditorViewModel.RestoreSnapshot concentra a reconstrução limpa dos blocos, tanto na abertura normal quanto na preparação da gestão. Ele preserva IDs e conteúdos e estabelece a referência de alterações salvas; somente o coordenador interno o usa para conteúdo já persistido ou preparado para uma renomeação que será confirmada antes de exibir o editor.

Imagens são representadas por referências nos testes de renomeação/movimentação; estes casos verificam que a referência não muda. A integridade dos arquivos continua coberta pela suíte anterior de anexos.

## Evidências e perguntas de estudo

Build sem avisos, formatação válida e 200 testes aprovados: 36 unitários e 164 de integração. Os 29 testes desta etapa e todos os anteriores permanecem intactos. O [relatório RED](../reviews/019-library-management-red.md) preserva a especificação inicial; o [relatório GREEN](../reviews/020-library-management-green.md) registra a implementação e a conferência dos controles, reabertura e anexo real com dados sintéticos.

1. Por que manter a identidade ao renomear evita reescrever associações?
2. Por que renomear um espaço não precisa descartar o rascunho da página?
3. Como provar que uma página movida não volta ao destino anterior quando salva?
4. Por que um teste com ações controladas não substitui a reabertura real de um banco?

Busca e tags foram implementadas posteriormente no [capítulo 17](17-busca-e-tags.md). Permanecem futuros: exclusão, movimentação de cadernos inteiros, retorno explícito para Não organizadas, backup e autosave. A conferência headless não substitui uma sessão interativa para teclado, escala e integração nativa do Windows.
