# Campos e criação na lateral

Complemento de 31/10/2026: a pedido do responsável, o filtro também adota o cabeçalho **TAG EXATA +**, reutilizando o mesmo template de Espaço/Caderno. “Filtros da busca” e sua seta deixam de aparecer. IsExpanded, critérios digitados, Enter, Buscar e Fechar busca permanecem ligados aos controles/comandos anteriores; esta alteração muda somente a classe visual e o rótulo do Expander.

31/10/2026. Refinamento de apresentação solicitado pelo responsável. O filtro de tag usa a mesma tipografia, preenchimento e acabamento da busca no acervo. Os formulários de criação saem do rodapé e ficam nos cabeçalhos ESPAÇO/CADERNO, com “+”, seguindo a localização da ação de PÁGINAS. Campos ocupam a largura disponível de seu nível; “Criar espaço/caderno” fica abaixo, sem estreitar a entrada.

`LibraryWorkspaceView.axaml` preserva os controles, bindings e comandos existentes. Os mesmos Expander continuam responsáveis por abrir/recolher; um template visual mostra título e “+” no cabeçalho, com ToggleButton vinculado a IsExpanded. Não há novo estado no ViewModel, gravação ao abrir, criação automática ou regra de negócio. Renomear permanece acessível no botão de reticências. Os nomes digitados continuam no ViewModel quando o formulário é recolhido.

Build, formatação e 426 testes existentes aprovados sem alteração dos testes. A conferência SQLite/Skia inclui campos abertos/recolhidos e controle por teclado, além do cenário anterior de janela compacta, seleção, histórico e autosave. Ver [revisão](../reviews/047-sidebar-fields.md).

Foi uma correção de apresentação, sem novos testes que apenas repetissem propriedades de estilo. O primeiro render revelou destaque azul do ToggleButton e cabeçalhos estreitos; ambos foram corrigidos antes da entrega. Revisão estética humana, outros DPI e conforto de uso continuam pendentes. Manual/PDF intactos.
