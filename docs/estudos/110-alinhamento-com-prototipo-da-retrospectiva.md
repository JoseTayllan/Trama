# 110 — Alinhamento com a referência visual

Data: 09/11/2026. Segunda rodada de refino solicitada com a imagem do protótipo.

A comparação identificou diferenças na posição da barra de modos, no destaque da aba ativa, no título, nos cartões, no agrupamento da navegação e nos rótulos do gráfico. Os modos foram movidos para cima do título e Retrospectiva ganhou destaque sólido com os recursos de acento e texto sobre acento.

Os cartões usam superfícies com gradiente discreto e bordas derivadas da paleta semântica. Ícones e valores são separados por divisores verticais. A navegação agrupa as duas ações e o período, preservando nomes acessíveis e limites existentes. Não foi adicionado um seletor de semanas sem comportamento implementado.

Os valores planejados e concluídos aparecem individualmente acima das respectivas barras. Cada barra tem seu próprio contêiner com recorte, evitando que o desenho vertical do controle invada o rótulo. Os ícones de categorias são vetores locais para trabalho, estudo e pessoal, sem nova dependência ou imagem fixa.

As mudanças são de apresentação, nos dois arquivos XAML. Não alteram consultas, cálculos, gravação, testes ou contratos. A reflexão pessoal que aparece na referência continua pendente de implementação e persistência próprias; os dados e ações existentes são o conteúdo desta tela.

Verificação: suíte existente, build, formatação e capturas SQLite/Skia em perfil fictício, na MainWindow ampla/compacta e com rolagem até as pendências. A inspeção inicial encontrou sobreposição do valor da barra mais alta, corrigida antes da captura e da verificação finais.

[Revisão](../reviews/117-retrospective-prototype-alignment.md).
