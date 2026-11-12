# 113 — Ícones nas pendências

Data: 10/11/2026. Refino visual solicitado na lista Pendências para revisar.

As linhas reutilizam os ícones vetoriais de trabalho, estudo e pessoal que já existem no painel. Eles aparecem antes do nome da categoria, com tamanho consistente. O símbolo de estado ganhou tamanho e altura de linha próprios, mantendo os caracteres e rótulos já aprovados para Pendente e Em andamento.

Categoria e estado ficam em duas colunas alinhadas, separadas por divisor discreto. O título mantém quebra de linha e recebe o espaço restante. Isso aproxima a lista da referência sem depender somente da faixa de cor para identificar o assunto.

O ajuste é somente XAML no template das pendências. Reaproveita geometria, recursos de cor e conversores; não altera dados, ações, testes, contratos ou armazenamento. A reflexão continua como prévia somente leitura, conforme a etapa anterior.

Verificação pela suíte existente e capturas SQLite/Skia em perfil fictício, incluindo a dupla de cartões ampla e o empilhamento compacto. O aceite estético e a execução Windows permanecem com o responsável.

[Revisão](../reviews/120-pending-icons-refinement.md).
