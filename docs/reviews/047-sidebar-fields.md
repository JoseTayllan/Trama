# Campos laterais: apresentação para revisão

Complemento de 31/10/2026: filtro de tag com o mesmo cabeçalho “+” de Espaço/Caderno, rotulado TAG EXATA. Alteração de uma linha de XAML, sem modificar testes ou regras de busca. Verificação completa e captura atualizadas em `trama-tag-filter`, com launcher `Testar-Tag.cmd` e diff próprio. Os artefatos anteriores permanecem como histórico.

31/10/2026. Mudança exclusiva em `LibraryWorkspaceView.axaml`: campos padronizados com a busca, filtros sem moldura duplicada e criação no cabeçalho de Espaço/Caderno, usando “+” e os mesmos Expander/comandos. As duas seções de criação no fim da lateral foram removidas. Sem regra nova de criação, alteração de contratos, código C#, dependências ou banco; manual/PDF intactos.

`tools/run-wsl.sh test`: build Release, formatação e **426 testes aprovados**, zero falhas/ignorados. Testes anteriores preservados por hash. Conferência SQLite/Skia de layout amplo/compacto, filtros, criação aberta/recolhida por teclado, histórico, seleção, autosave e reabertura. Capturas da aplicação real em `trama-sidebar-fields/qa/`, incluindo `campos-abertos.png`. Os bindings dos botões de criação continuam cobertos pela suíte existente.

O primeiro render mostrou o estilo azul nativo do botão marcado e largura inadequada do cabeçalho; o template foi ajustado e as verificações repetidas. Publicação Windows separada com launcher `Testar-Lateral.cmd` e perfil fictício. Resultados, smoke e comparação dos fontes publicados ficam na pasta de artefatos, junto de `implementacao.diff` e `audit.json`.

Pendentes: revisão humana do diff e estética, conferência em outros DPI. Abrir simultaneamente os formulários ocupa altura e continua usando a rolagem compartilhada; não foi introduzido fechamento automático ao criar ou alternar painéis. Esforço pequeno/moderado, um agente e duas conferências após ajuste visual. Economia seguinte: reutilizar esta versão e reunir o feedback antes de outro ciclo de acabamento.
