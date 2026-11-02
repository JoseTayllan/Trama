# Identidade do caderno e contexto hierárquico

30/10/2026. Implementação após aprovação dos 15 casos, preservados intactos. **414 testes aprovados**, build/formatação aprovados, conferência SQLite/Skia e pacote Windows separado verificados. Revisão humana da estética e do diff permanece pendente; manual/PDF intactos.

## Contexto derivado, sem dados duplicados

[LibraryViewModel](../../src/Trama.Desktop/ViewModels/LibraryViewModel.cs) acrescenta três propriedades de leitura: `ContextSpaceTitle`, `ContextNotebookTitle` e `ContextPageTitle`. Pais vêm da seleção por ID; página vem do título atual do editor, não do título antigo da lista persistida. Título vazio usa Sem título; não organizadas e espaço sem caderno têm estados explícitos da especificação.

As notificações acompanham seleção/renomeação/movimentação e mudança de título. Os getters não salvam, não navegam nem criam editor. Cancelar/falhar navegação conserva o estado anterior e, por consequência, seu caminho. Cadernos com nomes iguais em espaços diferentes continuam distintos por seus IDs.

O [contexto superior](../../src/Trama.Desktop/Views/LibraryWorkspaceView.axaml) é informativo. Não foi criado outro caminho de navegação que ignore confirmação de rascunho. Rótulos ESPAÇO, CADERNO e PÁGINA identificam os níveis; nomes longos ficam completos em tooltip. O agrupamento quebra em linhas quando necessário.

## Composição da página

A lateral usa recuos progressivos para cadernos/páginas, mantendo listas, IDs e comandos existentes. O cabeçalho geral passa a Biblioteca pessoal e deixa o contexto real para a área correspondente.

[NotebookEditorView](../../src/Trama.Desktop/Views/NotebookEditorView.axaml) envolve título e blocos em uma única superfície de página, com margem vertical discreta. Texto, código, imagem e ações de inserção continuam no fluxo. Títulos longos quebram linhas; indicador de salvamento e ações de inserção também se reorganizam para evitar sobreposição em janela compacta.

Os novos recursos `TramaNotebookPaperBrush` e `TramaNotebookMarginBrush` ficam na [paleta](../../src/Trama.Desktop/Themes/Palette.axaml), consumidos por DynamicResource. Não foram alterados logo, motor de edição, versões de dados ou regras de armazenamento. Renomear pais e redimensionar não substituem o editor, preservando seleção/histórico.

## Evidências e limites

Os [15 casos aprovados](../../tests/Trama.IntegrationTests/Knowledge/NotebookIdentityTests.cs) passaram junto com os 399 anteriores. O harness externo usou composição e SQLite reais com dados sintéticos: contexto/título vivo, renomeação sem recriação, popup/foco, teclado, autosave/reabertura e recursos dinâmicos. Uma página com texto, código e imagem foi conferida, incluindo rolagem até o anexo.

Capturas em 1340 e 860 de largura, nomes longos e anexo foram inspecionadas. Os níveis ficaram visíveis/dentro do contexto, com tooltips completos. Após inspeção inicial, foram ajustados título e indicador de salvamento para quebrar linhas. Contraste mínimo 5,882:1 nas 42 combinações avaliadas de texto/cores e fundos, incluindo a nova página; isso não certifica qualquer paleta futura.

Publicação Windows self-contained isolada passou e a janela nativa respondeu com título Trama, fechando normalmente em perfil sintético. A publicação habitual e dados pessoais não foram sobrescritos. DPI, avaliação estética/interativa do responsável e CI remota continuam pendentes. Ver [relatório](../reviews/043-notebook-identity-green.md).

Para estudar: por que derivar o título do editor evita exibir conteúdo desatualizado? Qual a diferença entre atualizar uma propriedade derivada e substituir o ViewModel? Como WrapPanel e tooltips preservam contexto sem exigir uma árvore de dados nova?
