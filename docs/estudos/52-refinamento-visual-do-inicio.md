# 52 — Refinamento visual do Início

Data: 02/11/2026.

## Objetivo

Aproximar a implementação da imagem conceitual aprovada: navegação com ícones, cartões ilustrados, hierarquia mais forte e informações claras sobre o acervo e o backup.

A imagem é uma referência visual. Tarefas, totais, datas de acesso e indicadores de uma Agenda ainda ausente não podem ser tratados como dados existentes. A página em andamento usa título, localização e estado reais já disponíveis na navegação.

## Implementação

`MainWindow.axaml` concentra as mudanças. Geometrias vetoriais locais representam Início, Cadernos, Agenda, armazenamento, backup e página. Escalam sem depender de fontes de ícones ou pacotes adicionais. Os botões preservam comandos e nomes de controles; nomes acessíveis explícitos continuam identificando ações cujo conteúdo agora combina desenho e texto.

Os cartões usam um degradê discreto com cores semânticas da paleta, bordas e títulos consistentes. Cadernos usa verde-água; Agenda usa o recurso violeta existente. Os desenhos de cadernos empilhados e calendário são decorativos e não recebem cliques. O calendário não possui tarefas ou números fictícios.

O topo mantém um único acesso global ao backup. A faixa inferior explica onde estão os dados e como acessar a recuperação. O fluxo e o diálogo de backup permanecem os mesmos.

O painel de continuidade usa `HasNotebookToContinue`, título, localização e estado já existentes. Sem página aberta, mostra orientação para iniciar uma anotação. Não representa um histórico de páginas recentes nem grava uma lista de acessos.

`MainWindow.axaml.cs` aplica a classe visual `compact-home` abaixo de 1080 unidades lógicas. Essa classe esconde ilustrações laterais e reduz o título para 34 unidades, mantendo os cartões e ações. A largura mínima da janela continua 860. O redimensionamento não troca o conteúdo do editor.

## Validação

Nenhuma regra de navegação, contrato, persistência, dependência, formato ou teste aprovado foi alterado. O refino usa a especificação existente: os 17 casos de navegação e os seis de rolagem passaram, junto de um caso adicional de autosave já existente, totalizando 24 verificações focadas. Não foram criados testes que apenas repetem medidas e desenhos do XAML.

Build sem avisos/erros e verificação de formatação passaram. A execução completa mais recente aprovou 534 dos 535 casos, com um timeout em teste anterior de autosave de listas; a repetição focada passou. Uma execução anterior do refino teve timeout no autosave oculto, que também passou na verificação focada. A instabilidade permanece registrada e impede declarar uma suíte única totalmente verde nesta entrega. Não houve alteração dos testes ou do mecanismo de autosave para contorná-la.

Conferência visual feita com renderização real Skia, SQLite e perfis fictícios. Capturas e pacote isolado ficam fora do repositório. A validação estética do responsável e a revisão do diff continuam necessárias.

## Para estudar

- Por que usar caminhos vetoriais em vez de símbolos dependentes de uma fonte?
- Qual a vantagem de referenciar cores semânticas no degradê e nos ícones?
- Por que a aparência do protótipo não autoriza mostrar números fictícios?
- Como alterar o layout da janela mantendo a instância do editor e seu histórico?
