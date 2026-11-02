# Desfazer e refazer: especificação aprovada

29/10/2026. O responsável validou o autosave e aprovou os treze casos de desfazer/refazer. Este documento preserva a especificação; a [implementação](25-desfazer-refazer-implementado.md) passou 325 testes. O manual de uso e o PDF permanecem sem alteração, conforme solicitado.

## Comportamento esperado

No bloco de prosa em foco, Ctrl+Z deve desfazer a última ação e Ctrl+Y ou Ctrl+Shift+Z deve refazê-la. Texto e estilos formam um estado coerente: apagar um trecho formatado e desfazer deve recuperar tanto as letras quanto seus intervalos de negrito/itálico.

Formatação também deve entrar no histórico. Se a pessoa digitar e depois aplicar negrito, o primeiro desfazer remove o negrito; o segundo desfaz a digitação. Sobreposições de negrito/itálico e remoção de um estilo existente devem voltar ao estado anterior correto.

O histórico pertence ao bloco e à sessão de edição daquela página. Alterar outro bloco não pode modificar a pilha do primeiro. Ao carregar outra página, não oferecer ações da página anterior, mesmo se o texto for igual. Depois de desfazer, uma nova edição abandona o ramo anterior de refazer.

Salvar pelo botão ou automaticamente não deve apagar o histórico da página aberta. Desfazer uma formatação já gravada cria outra alteração, que deve seguir pelo fluxo normal de salvamento e preservar a identidade da página.

Este recorte concentra o histórico enriquecido de prosa. Títulos e código mantêm os históricos nativos dos próprios controles. Histórico para adicionar/remover/reordenar blocos fica para uma etapa específica. O histórico proposto é de sessão; não é versionamento persistente ou restauração de páginas fechadas.

## Por que o comportamento atual falha

[NotebookProseEditor](../../src/Trama.Desktop/Views/NotebookProseEditor.cs) usa o histórico textual do AvaloniaEdit. Os estilos ficam em NotebookBlockEditor.Styles e são atualizados por [EditText/FormatTextSelection](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.Writing.cs).

Hoje, mudar estilos não acrescenta uma ação à pilha textual. Além disso, desfazer uma exclusão textual insere novamente as letras pelo fluxo comum de edição, que não conhece os estilos originais removidos. O resultado pode recuperar o texto sem sua formatação. As regressões exercitam esses efeitos diretamente pelos comandos existentes, sem criar contratos de produção nem depender de APIs futuras.

## Treze casos para aprovação

[ProseUndoRedoTests.cs](../../tests/Trama.IntegrationTests/Knowledge/ProseUndoRedoTests.cs) cobre:

1. Desfazer/refazer aplicação de negrito sem mudar o texto.
2. Voltar uma etapa ao sobrepor negrito e itálico.
3. Recuperar o intervalo original ao desfazer a remoção de negrito.
4. Recuperar texto e estilos ao desfazer uma exclusão.
5. Restaurar estados completos ao desfazer/refazer uma substituição de trecho formatado.
6. Respeitar a ordem entre digitação e formatação.
7. Manter o histórico após Salvar e persistir a alteração desfeita.
8. Manter o histórico após autosave e salvar automaticamente a formatação desfeita.
9. Invalidar refazer quando há nova digitação após desfazer.
10. Manter conteúdo e histórico independentes entre blocos.
11. Iniciar outra página sem histórico da anterior.
12. Atalhos Ctrl+Z e Ctrl+Y para estilos.
13. Atalhos Ctrl+Z e Ctrl+Shift+Z para estilos.

Os testes usam a view real em Avalonia.Headless e dados sintéticos. O caso com autosave aguarda o sinal de gravação com prazo limitado; não usa uma pausa arbitrária para presumir sucesso. As ações de persistência são controladas, sem abrir o banco pessoal.

## Plano após aprovação

1. Integrar os estilos ao histórico do editor, agrupando texto e metadados como uma ação coerente.
2. Restaurar estados sem registrar novas ações durante undo/redo; preservar a seleção/foco e impedir ciclos de eventos.
3. Reiniciar o histórico quando o bloco/página mudar, mantendo-o após salvar a página em edição.
4. Fazer as alterações restauradas notificarem o ViewModel e o autosave pelo fluxo existente.
5. Executar os casos aprovados intactos, conferir edição/atalhos com renderização e reabertura em banco isolado e apresentar o diff final.

Não alterar migrações nem formatos de backup para implementar histórico de sessão. Avaliar consumo de memória e agrupamento de digitação, mantendo uma política explícita e sem enfraquecer os casos aprovados.

## Estado atual e estudo

O [RED histórico](../reviews/033-prose-undo-red.md) teve dez falhas comportamentais e três proteções passando. Após aprovação dos treze casos, a implementação passou a suíte completa de 325 testes. Ver [relatório GREEN](../reviews/034-prose-undo-green.md). A revisão humana do diff final permanece pendente.

Para estudar: por que inserir novamente o texto apagado não recupera os estilos? Por que a operação inversa não pode criar outra entrada normal na pilha? Por que salvar o conteúdo não equivale a encerrar sua sessão de edição? Como o autosave distingue uma nova revisão criada por desfazer?
