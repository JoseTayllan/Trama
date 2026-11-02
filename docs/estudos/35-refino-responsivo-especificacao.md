# Refino responsivo: especificação para revisão

30/10/2026. Feedback sobre quatro capturas do responsável após a aproximação à referência visual. Esta etapa contém somente testes e planejamento; implementação aguardando revisão conforme a seção 9 do CODEX.

## Problemas observados e direção

1. A margem do cabeçalho e a coluna dos blocos usam origens diferentes; os marcadores dos blocos de texto ficam ocultos. Proposta: uma única linha, com marcadores visíveis para todos os blocos e numeração atualizada após reordenar/remover. O título não conta como bloco numerado.
2. Rolagem larga e concorrente na lateral prejudica a leitura. Refinar visualmente as barras e manter rolagem por roda, teclado e arraste; não esconder conteúdo longo nem eliminar a navegação horizontal necessária ao código.
3. Busca/filtros ficam acima da região rolável. Reunir busca, filtros e acervo em uma área rolável, com filtros recolhíveis sem perder critérios. A pedido do responsável, esta é a busca do acervo: Ctrl+F na página não faz parte do escopo.
4. Lateral sem recolhimento e barra de formatação com várias linhas reduzem a escrita em largura compacta. Proposta: lateral aberta em largura ampla, recolhida inicialmente em 860 unidades, controle acessível para reabrir/fechar; formatação compacta com expansão explícita. Recolher painéis não pode descartar texto, seleção ou histórico.
5. Setas e cruz de cada bloco repetem a composição que o responsável quer abandonar. Proposta: um acesso discreto “Bloco”, com menu textual “Antecipar bloco”, “Adiar bloco” e “Remover bloco”, reaproveitando os comandos atuais. Arrastar e soltar para reordenar não entra nesta etapa.

## Contratos executáveis

Os 12 casos em `NotebookLayoutRefinementTests.cs` cobrem: marcadores em texto/código, alinhamento à linha, renumeração/identidade, recolhimento manual da lateral com rascunho, largura compacta, filtros dentro da região rolável, filtros por teclado com critérios preservados, formatação compacta e seleção preservada ao reabrir, menu sem setas expostas, comandos do menu e proteção da edição ao redimensionar.

Usam controles reais, ViewModels e SQLite temporário. Não criam propriedades falsas no produto para compilar. Abertura/fechamento por clique e teclado, aparência das barras, DPI e conforto visual também precisam de conferência da aplicação renderizada na implementação. Testes de bounds não substituem inspeção visual.

## Plano após aprovação

Unificar a margem e seus marcadores; introduzir controles de recolhimento sem recriar o editor; reorganizar a rolagem lateral; substituir as ações visíveis por menu; conferir seleção/foco ao expandir; executar regressões e renderizar com filtros abertos/fechados em larguras ampla e compacta. Só depois preparar executável isolado e diff para revisão. Preservar banco, formatos, pacotes e manual/PDF.

Ver [relatório RED](../reviews/045-layout-refinement-red.md). Estude a distinção entre estado da interface e conteúdo salvo: recolher a lateral é uma mudança de apresentação, enquanto reordenar um bloco altera o documento e precisa continuar acionando autosave.
