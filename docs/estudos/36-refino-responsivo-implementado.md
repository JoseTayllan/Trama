# Refino responsivo e seleção da biblioteca

31/10/2026. Implementação dos 12 casos aprovados, mais o destaque visual solicitado para espaço e caderno ativos. Disponível para revisão do diff e do resultado; a aprovação dos testes não equivale à aprovação estética da entrega.

## O que mudou

Espaço, caderno e página selecionados usam fundo verde-água e uma borda à esquerda. As seleções continuam baseadas nos IDs dos objetos: nomes iguais em locais diferentes não passam a representar o mesmo caderno. Os recursos vêm da paleta existente, permitindo evoluir a personalização sem gravar cores nas páginas.

A lateral tem um controle **Acervo/Recolher acervo**. Abaixo de 1000 unidades de largura da área da biblioteca, começa recolhida e pode ser reaberta. O tamanho do editor aumenta sem reconstruir sua instância. Busca, filtros, listas e criação compartilham uma área rolável; o rodapé continua separado. As três listas deixam de ter barras verticais concorrentes. Os filtros mantêm critérios ao recolher, sem executar uma busca ou alterar o documento só por abrir/fechar o painel.

A formatação tem um controle **Texto** para expandir/recolher. Em largura de editor abaixo de 900 unidades, fica inicialmente recolhida; a expansão manual permanece enquanto não se atravessa esse limite de largura. O controle fica fora das ferramentas desabilitadas para que possa ser aberto mesmo antes de focar a prosa. Os seletores existentes continuam vinculados ao texto ativo e preservam a seleção. As dimensões são unidades lógicas do Avalonia, não pixels físicos de uma captura em qualquer DPI.

O título deixa de contar como bloco. Todos os blocos, inclusive texto, têm marcador sequencial; a coluna de 44 unidades tem a mesma origem que a linha contínua. Reordenar/remover atualiza os números sem criar novos metadados de conteúdo. A numeração acompanha os blocos reais, não títulos internos do texto.

**Bloco ···** substitui as setas e a cruz expostas. O menu oferece **Antecipar bloco**, **Adiar bloco** e **Remover bloco**, reutilizando os comandos atuais e o ID do bloco. Arrastar e soltar para reordenar permanece fora do escopo. As barras de rolagem ficam mais finas; o código longo continua acessível horizontalmente.

## Como estudar a implementação

- `LibraryWorkspaceView.axaml` reúne a rolagem e os estilos da seleção. Seu code-behind ajusta apenas a visibilidade da lateral e a coluna correspondente ao cruzar a largura compacta ou receber o clique do usuário.
- `NotebookEditorView.axaml` compartilha a origem da margem e separa o botão de expansão das ferramentas. O menu continua presente e acessível por teclado, sem depender de hover para existir.
- `NotebookEditorView.Presentation.cs` trata apresentação, numeração e menu. A observação da coleção é removida ao desanexar; a mudança visual não cria uma revisão do documento. O estado compacto é reavaliado apenas ao mudar de faixa para não desfazer a escolha manual a cada layout.
- `Themes/Controls.axaml` define a espessura e o aspecto das barras usando recursos de tema. A navegação por roda, teclado e arraste continua pertencendo aos controles existentes.

## Evidências e limites

Os 12 testes aprovados foram preservados; suíte completa com **426 aprovados**, build Release e formatação conferidos. Harness SQLite/Skia verifica foco, seleção, histórico, autosave/reabertura, painéis, filtros, hierarquia selecionada, nomes longos e imagens em larguras ampla/compacta. Ver [relatório](../reviews/046-layout-refinement-green.md).

O estado aberto/fechado dos painéis é temporário e não é uma configuração persistida por usuário. A lateral continua mostrando listas da seleção atual, sem implementar uma árvore global de todos os espaços simultaneamente. Barras finas exigem conferência humana de conforto em outros DPI e dispositivos de entrada. Não houve busca Ctrl+F, migração, dependência nova ou alteração de manual/PDF.

Para revisar seu aprendizado: por que esconder uma View é diferente de recriá-la? Por que o menu precisa guardar o ID do bloco, em vez de sua posição? Por que atravessar um limite de largura e redimensionar dentro da mesma faixa precisam de tratamentos distintos?
