# Alinhamento da interface com a referência

30/10/2026. Correção da apresentação após o responsável apontar a distância entre a implementação anterior e a proposta visual. Disponível para revisão estética e do diff; não representa aprovação humana do resultado.

A referência organiza a experiência em três regiões: acervo lateral, contexto da página e superfície de escrita. A implementação anterior mantinha cabeçalhos, busca, ações e salvamento em várias faixas. A correção reúne essas responsabilidades nas regiões correspondentes.

## Composição e código

- `LibraryWorkspaceView.axaml`: busca na lateral, filtros e criação recolhíveis, listas com recuos e linhas de hierarquia, ações da página junto à seleção e backup no rodapé lateral. O cabeçalho reúne Espaço → Caderno → Página, estado e Salvar. Os nomes completos continuam nos tooltips.
- `MainWindowViewModel.HasLibrary` controla a apresentação do cabeçalho inicial. A biblioteca incorpora a marca; a composição sem biblioteca conserva o cabeçalho anterior. O caminho do armazenamento continua disponível no tooltip de “Local · seus arquivos”.
- `NotebookEditorView.axaml`: página aberta sem contorno de cartão, título editável, contexto do caderno, tags e margem contínua. A barra usa controles planos e oferece os mesmos comandos de formatação e histórico. Código mantém sintaxe e ganha apresentação mais discreta e fonte monoespaçada com alternativa disponível no Linux.
- `NotebookEditorView.Presentation.cs`: propriedades visuais permitem hospedar o editor sem duplicar o cabeçalho. Os números na margem são calculados pela ordem dos blocos, sem gravar metadados nem sujar o documento. Os botões Desfazer/Refazer reutilizam o histórico da prosa ativa; mudanças na pilha atualizam sua disponibilidade.
- `MainWindow.OpenBackupAsync`: o novo acesso lateral reutiliza o diálogo e suas proteções existentes contra abertura duplicada e fechamento durante operações.

O conteúdo continua estruturado em blocos internamente. A navegação usa as três listas existentes, com indentação visual, e mostra os cadernos/páginas da seleção atual; não é uma árvore completa de todos os espaços simultaneamente. O salvamento do cabeçalho usa exatamente `Editor.SaveCommand`. Não houve alteração de banco, formatos, dependências, segurança do Windows ou dados pessoais.

## Verificação e limites

Build Release e formatação passaram; 414 testes aprovados, sem ignorados e sem alterar os testes existentes. Uma conferência adicional com SQLite/Skia verificou janelas de 1340 e 860 unidades, nomes longos, seleção/foco, botões e atalhos de histórico, autosave/reabertura, imagens e recursos dinâmicos de cor. O pacote Windows separado abriu com título Trama e encerrou normalmente. Ver [relatório](../reviews/044-reference-alignment.md).

As capturas usam conteúdo fictício. Espaçamentos podem variar com DPI e fontes instaladas; revisão interativa do responsável permanece pendente. Os indicadores de cor na barra identificam os tipos de ferramenta, enquanto o texto do seletor informa o valor selecionado. O manual e o PDF não foram atualizados, conforme solicitado.

Para estudar: siga o binding do botão Salvar até o comando existente; observe por que o cabeçalho pode mudar sem recriar o editor; compare a ordem visual da margem com a coleção de blocos; investigue por que recursos de tema são preferíveis a cores gravadas no conteúdo.
