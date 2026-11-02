# Identidade do caderno e hierarquia: especificação aprovada

30/10/2026. O responsável aprovou os 15 casos antes da implementação. Esta especificação preserva o contrato de **Espaço → Caderno → Página**, mantendo editor, estilos, histórico e salvamento. O [GREEN posterior](33-identidade-e-hierarquia-implementadas.md) passou 414 testes.

## Contexto sempre identificável

Um espaço contém cadernos; cada caderno contém páginas. Texto, código e imagem são blocos dentro da página, não níveis da biblioteca. A navegação lateral mantém esses nomes e seleções por identidade. No topo da área de conteúdo, exibir os três níveis com rótulos explícitos e os nomes correspondentes à página aberta.

O caminho é informativo nesta fatia. Não criar uma segunda navegação com regras diferentes ao clicar nos seus nomes. A navegação existente continua responsável por confirmação de rascunhos, espera pela gravação e publicação do destino somente após sucesso.

| Situação | Espaço | Caderno | Página |
| --- | --- | --- | --- |
| Página organizada | Nome do espaço | Nome do caderno | Título atual da página |
| Página sem título | Nome do espaço | Nome do caderno | Sem título |
| Não organizada | Não organizadas | Sem caderno | Título atual da página |
| Espaço sem caderno selecionado | Nome do espaço | Escolha um caderno | Nenhuma página aberta |

O título mostrado acompanha a edição atual, sem exigir Salvar e sem gravar por exibir o caminho. Nomes de pais acompanham renomeações. Mover para outro espaço atualiza o contexto mesmo quando os cadernos têm nomes iguais. Cancelar/falhar navegação mantém o caminho do conteúdo que realmente ficou aberto.

## Composição visual

- Página contínua com margem discreta, hierarquia tipográfica e ferramentas compartilhadas já entregues.
- Agrupar os três níveis no contexto superior; evitar nomes genéricos que pareçam um caminho de seleção real, como o cabeçalho estático Meu espaço / Meu caderno.
- Preservar controles/listas existentes e seus comandos, sem trocar todo o modelo por TreeView ou criar cadernos aninhados.
- Distinguir níveis com texto, agrupamento e espaçamento, além de cor. Não depender somente de ícones ou de hover.
- Reorganizar o contexto em janela compacta para manter os três níveis acessíveis; nomes longos continuam completos em tooltip. A largura de um título não pode empurrar outro nível para fora da área.
- Preservar foco, seleção e instância do editor em mudanças de composição/redimensionamento; não recriar o editor só para atualizar o caminho.
- Código e imagens mantêm seus tipos e comportamentos. Margens e superfícies usam recursos semânticos, deixando abertura para futuras paletas.

O estudo visual conceitual já aceito orienta a composição, sem copiar elementos de execução do Colab. Esta etapa não é análise jurídica e não muda logo ou marca.

## Quinze casos preparados

[NotebookIdentityTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NotebookIdentityTests.cs) usa a view real, composição e SQLite temporários:

1. Cabeçalho com ESPAÇO, CADERNO e PÁGINA, mais o caminho da página aberta.
2. Título em edição atualiza o contexto sem salvar nem mudar os pais.
3. Título vazio apresenta Sem título.
4. Renomear espaço atualiza o caminho e mantém rascunho, seleção e histórico.
5. Renomear caderno atualiza o caminho sem substituir o editor.
6. Renomear página atualiza o caminho preservando estilos salvos.
7. Mover para caderno de mesmo nome em outro espaço muda o contexto e o destino da gravação.
8. Abrir outra página altera somente o nível de página.
9. Não organizada não conserva pais da seleção anterior.
10. Espaço sem caderno mostra contexto vazio explícito e não expõe editor ativo.
11. Cancelar navegação preserva caminho e rascunho.
12. Página indisponível não altera o caminho atual.
13. Janela 860 com títulos longos mantém os níveis visíveis/dentro do contexto, com tooltips completos.
14. Proteção das seleções existentes na lateral por ID.
15. Proteção de instância, seleção e histórico da prosa ao redimensionar.

Nomes de controles propostos para automação: `PageContextTrail`, `SpaceContextHeading`, `NotebookContextHeading`, `PageContextHeading`, `CurrentSpaceLabel`, `CurrentNotebookLabel` e `CurrentPageLabel`. Os testes não exigem novos contratos de produção nem métodos vazios.

O [RED histórico](../reviews/042-notebook-identity-red.md) teve 13 falhas esperadas e duas proteções passando, preservando os 399 anteriores. Após aprovação, a implementação passou 414 testes e conferência visual/nativa, sem modificar os casos. Ver [relatório GREEN](../reviews/043-notebook-identity-green.md).

## Após aprovação

1. Derivar os nomes do contexto dos pais selecionados e do editor atual, com notificações corretas para título/renomeação/movimentação.
2. Conectar o contexto superior e aplicar composição própria do caderno, sem mudar navegação ou persistência.
3. Preservar instâncias/estados do editor e fazer os casos aprovados passarem sem modificá-los.
4. Executar build/formatação/suíte e conferir capturas ampla/compacta, nomes longos, teclado e página com texto/código/imagens.
5. Apresentar diff e pacote de teste separado para revisão humana. Manual/PDF permanece intacto até solicitação.

Para estudar: por que o título do caminho deve vir do editor e não da lista persistida? Como nomes de cadernos iguais mostram a necessidade de trabalhar por IDs? Por que mudar apenas a aparência não deve limpar histórico nem acionar uma gravação?
