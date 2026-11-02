# Controles de formatação: especificação aprovada

30/10/2026. Os 25 casos foram aprovados antes da implementação e permaneceram intactos. O [GREEN posterior](31-controles-de-formatacao-implementados.md) passou 399 testes. A direção visual aceita continua sendo página contínua com hierarquia explícita Espaço → Caderno → Páginas; a composição completa da navegação continua uma fatia própria.

## Comportamento esperado

Uma única barra para o editor, sem repetir seletores em cada parágrafo. Tamanho, cor e marca-texto afetam apenas a seleção do bloco de prosa ativo. Abrir um seletor não pode perder a seleção; após uma alteração efetiva, devolver o foco ao texto. Ao trocar o bloco ativo, atualizar o destino da formatação. Ao focar código, desabilitar os controles de prosa para não alterar uma seleção antiga.

Tamanhos: 12, 14, 16, 18, 24 e 32 unidades lógicas, mais Padrão. Cor: automática/padrão e identificadores semânticos aprovados. Destaque: nenhum e identificadores semânticos aprovados. Uma seleção com tamanhos diferentes não mostra um tamanho único incorreto. Sincronizar os seletores com uma seleção carregada não é edição: não marcar a página nem criar histórico.

Aplicar um atributo preserva todos os outros. Escolher Padrão remove somente o atributo correspondente, herdando o tema. Limpar formatação remove tamanho/cor/destaque/negrito/itálico apenas da seleção e mantém texto, offsets sobreviventes e demais trechos. Seleção vazia ou reaplicar valor idêntico não cria edição/histórico. Estilo para digitação futura permanece fora desta fatia.

Mudanças de atributos e limpeza entram no histórico existente como uma ação coerente, usando as cópias de intervalos completas. Ctrl+Z/Ctrl+Y mantêm atributos juntos. Salvar/autosave não limpa a pilha; uma nova formatação após desfazer abandona o ramo anterior de refazer. Abrir outra página não pode aplicar automaticamente valores da seleção anterior.

## Controles e apresentação

Para automação e testes, os controles compartilhados são identificados por `ProseFontSizePicker`, `ProseForegroundPicker`, `ProseHighlightPicker` e `ClearProseFormattingButton`. Os seletores expõem opções como `ComboBoxItem` com `Tag` estável: tamanho numérico, `default` para tamanho/cor automáticos, `none` para remover destaque e IDs semânticos para cores. Esses nomes são o contrato de teste da view, sem criar API HTTP ou novo serviço.

O renderer de prosa precisa aplicar os atributos aos trechos do AvaloniaEdit, incluindo tamanho misto, cor de primeiro plano e fundo. Testes de tamanho observam `FontRenderingEmSize` no trecho visual; cores usam brushes semânticos injetados no escopo da janela, sem exigir cores literais nas Views. As chaves exercitadas são `TramaProseForegroundTealBrush` e `TramaProseHighlightAmberBrush`.

Os testes de brush verificam resolução dos recursos. Não substituem conferência de contraste da paleta padrão, substituição dinâmica de paletas, DPI, quebra de linha ou altura com texto grande. Conferir esses pontos no harness visual depois da implementação. Não anunciar editor de paletas configurável como entregue.

## Vinte e cinco casos

[RichTextEditorTests.cs](../../tests/Trama.IntegrationTests/Knowledge/RichTextEditorTests.cs) cobre:

1. Um conjunto de controles para vários blocos de prosa.
2–4. Aplicação de tamanho, cor e destaque somente à seleção, preservando os demais atributos, seleção e foco.
5–7. Remover individualmente tamanho, cor e destaque sem apagar outros atributos.
8. Limpar somente a seleção sem apagar conteúdo.
9–10. Seleção vazia e valor idêntico sem edição/histórico.
11. Tamanhos mistos sem valor único incorreto; aplicar tamanho mantém cores distintas.
12. Sincronizar controles com trecho carregado sem editar a página.
13. Trocar bloco de prosa direciona a formatação para o novo alvo.
14. Foco no código desabilita os controles de prosa.
15. Ctrl+Z/Ctrl+Y restaura tamanho e demais atributos.
16. Limpeza é uma única ação reversível.
17. Salvar, desfazer e salvar novamente preserva os atributos restaurados e o histórico.
18. Autosave recebe a alteração da barra e preserva o histórico.
19. Nova formatação após desfazer invalida refazer.
20. Outra página começa sem valores anteriores aplicados automaticamente.
21–23. Tamanho/cor/destaque carregados chegam aos trechos renderizados.
24–25. Proteções do histórico já existente para exclusão de texto enriquecido e alternância de negrito.

Os casos usam Avalonia.Headless, a view real e ações de persistência controladas com dados sintéticos. Autosave aguarda o sinal real da gravação, com prazo limitado; não presume sucesso após uma pausa arbitrária. Persistência real/backup já foram verificados na fatia anterior; aqui não é usado banco pessoal.

O [RED histórico](../reviews/040-rich-text-editor-red.md) teve 23 falhas esperadas e duas proteções passando, preservando os 374 anteriores. As falhas de renderização confirmaram tamanho 16 em vez de 24, primeiro plano padrão e fundo ausente. Após aprovação explícita, o GREEN passou todos os 399 casos e conferência visual/SQLite. Ver [relatório da implementação](../reviews/041-rich-text-editor-green.md).

## Ordem após aprovação

1. Adicionar operações de atributo/limpeza no modelo e nas ações do editor, preservando os contratos existentes e todas as regras do modelo/persistência já aprovadas.
2. Conectar uma barra compartilhada com seleção/estado misto e atualização sem efeitos colaterais.
3. Integrar as operações ao histórico, reaproveitando os snapshots de estilos existentes.
4. Aplicar tamanho/brushes na renderização e tratar medição/redesenho sem regressão do texto longo.
5. Executar os casos aprovados intactos, suíte pertinente e harness de renderização/contraste em janela ampla/estreita. Apresentar diff e riscos.

A composição completa da nova biblioteca, a navegação com os três níveis e as ações estruturais de blocos permanecem recortes próprios. Não mudar o banco ou o formato do backup para construir a barra. Manual/PDF permanecem congelados até solicitação.
