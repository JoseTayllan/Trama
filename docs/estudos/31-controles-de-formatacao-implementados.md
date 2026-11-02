# Escrita rica no editor: implementação

30/10/2026. Implementado após aprovação dos 25 casos, preservados integralmente. **399 testes aprovados**, build/formatação aprovados, harness SQLite/Skia e publicação Windows isolada conferidos. Manual/PDF intactos. A composição visual completa com Espaço → Caderno → Páginas continua como próxima fatia.

## Fluxo de formatação

A [view](../../src/Trama.Desktop/Views/NotebookEditorView.axaml) oferece uma barra compartilhada com negrito, itálico, tamanho, cor, marca-texto e limpar. Selecionar um trecho no bloco de prosa define o destino; entrar no código/título retira esse destino. Os seletores refletem a seleção sem editar a página e mostram Misto quando o atributo varia.

O [coordenador visual](../../src/Trama.Desktop/Views/NotebookEditorView.Formatting.cs) trata foco, opções e sincronização dos controles. O AvaloniaEdit pode marcar eventos de foco como tratados, por isso a view também observa esses eventos. Popups possuem outra raiz visual: abrir/selecionar uma opção não deve abandonar o alvo de prosa. Ao trocar página ou destacar o editor da árvore visual, remover a assinatura do alvo anterior.

O caminho de uma alteração é barra → [NotebookProseEditor](../../src/Trama.Desktop/Views/NotebookProseEditor.cs) → [ações do ViewModel](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.Writing.cs) → [InlineTextDocument](../../src/Trama.Application/Knowledge/InlineTextDocument.cs). A view não acessa banco e o ViewModel não controla brushes.

O modelo divide os intervalos nas bordas da seleção e transforma apenas os intervalos selecionados. Tamanho, cor e destaque preservam os outros atributos; padrão/automática/sem destaque removem somente o atributo correspondente. Limpar cria intervalos sem atributos apenas na seleção. A lista só é publicada depois da validação, e trechos vizinhos inteiramente iguais voltam a ser unidos.

Seleção vazia e valor idêntico não geram revisão/histórico. A barra não programa um estilo para futura digitação; a herança de atributos ao digitar dentro de um trecho formatado continua seguindo a regra existente.

## Histórico e salvamento

O editor captura os intervalos completos antes/depois, reaproveitando `StyleHistoryOperation` da pilha nativa. A aplicação de estilo e a limpeza formam ações reversíveis; desfazer restaura texto/atributos coerentes e seleção. Salvar/autosave preservam a pilha, e outra edição após desfazer abandona o ramo antigo de refazer.

O ViewModel verifica `CanEditContent`, bloco, seleção e igualdade dos intervalos antes de publicar o estado. Essa publicação segue as notificações e o autosave existentes. A persistência já implementada grava atributos novos como página 3; não houve nova alteração de banco ou de pacote de backup.

## Recursos e medição de texto

O transformer aplica tipo de fonte, tamanho e brushes aos trechos do AvaloniaEdit. [Palette.axaml](../../src/Trama.Desktop/Themes/Palette.axaml) define recursos semânticos `TramaProseForeground…Brush` e `TramaProseHighlight…Brush`; as Views não recebem cores hexadecimais espalhadas. O controle observa substituição de recursos e mutação de brushes, removendo assinaturas ao sair da árvore visual. Paletas podem mudar a apresentação sem regravar identificadores semânticos do documento. O editor de preferências de paleta ainda é futuro.

A conferência visual encontrou sobreposição: AvaloniaEdit 11.3.0 definia altura fixa pelo tamanho padrão, embora os trechos desenhassem fontes maiores. Atualizamos somente AvaloniaEdit para 11.4.1 (MIT), que permite altura medida automaticamente; Avalonia permanece 11.3.19. Fontes oficiais: [implementação 11.3.0](https://raw.githubusercontent.com/AvaloniaUI/AvaloniaEdit/11.3.0/src/AvaloniaEdit/Rendering/VisualLineTextParagraphProperties.cs), [correção 11.4.1](https://raw.githubusercontent.com/AvaloniaUI/AvaloniaEdit/11.4.1/src/AvaloniaEdit/Rendering/VisualLineTextParagraphProperties.cs) e [dependências do pacote](https://www.nuget.org/packages/Avalonia.AvaloniaEdit/11.4.1). Não foram usados reflection, mudança global de tamanho ou cópia de código interno da biblioteca.

Os locks alteram somente AvaloniaEdit e sua referência transitiva pelo desktop; grafos Windows anteriores foram preservados, e locks de projetos sem mudança ficaram intactos. A restauração Windows com `-r win-x64 --locked-mode` passou. A barra quebra em linhas em janela compacta para manter os controles acessíveis.

## Evidências e limites

Os [25 casos aprovados](../../tests/Trama.IntegrationTests/Knowledge/RichTextEditorTests.cs) passaram junto com os 374 anteriores. O harness externo abriu popup, focou a opção, formatou, desfez/refez pelo teclado, aguardou autosave real, reabriu por outra composição SQLite e conferiu substituição/mutação/remoção de recurso. Capturas em 1340 e 860 de largura foram inspecionadas; limites dos controles, altura da prosa e altura em relação ao maior trecho foram conferidos.

Na paleta padrão, as 36 combinações avaliadas entre texto/cores disponíveis e fundos de página/destaque tiveram contraste mínimo 5,882:1. Isso não garante legibilidade de qualquer paleta futura. DPI e uso interativo pelo responsável continuam pendentes. O pacote Windows de teste abriu, respondeu com título Trama e fechou normalmente usando perfil sintético separado; não acessou notas pessoais nem substituiu a publicação habitual.

Ver [relatório completo](../reviews/041-rich-text-editor-green.md). Próxima etapa: aplicar a composição visual própria e deixar o caminho Espaço → Caderno → Páginas explícito também no topo, mantendo as proteções da escrita já implementadas.

Para estudar: por que sincronizar um ComboBox não deve executar uma nova edição? Como foco tratado e popups mudam o alvo de uma ação? Por que inspecionar propriedades de fonte não comprova que as linhas tenham altura correta? Como uma cor semântica sobrevive à troca da paleta?
