# Rolagem e seleção nos blocos — especificação em revisão

Data: 02/11/2026.

Relato: a roda do mouse funciona fora das células, mas não sobre elas; selecionar texto pode devolver a página ao início. O responsável também considera a interface ainda distante do protótipo. Validação prévia da ideia não equivale a aceite visual final.

## Reprodução

Arquivo: `tests/Trama.IntegrationTests/Knowledge/NotebookScrollingRegressionTests.cs`. Somente conteúdo fictício; nenhum banco pessoal foi aberto.

Seis casos descobertos e compilados no WSL:

| Comportamento | Resultado antes da correção |
| --- | --- |
| Roda para baixo sobre prosa | Falha: posição permanece em 650 |
| Roda para cima sobre prosa | Passa, proteção |
| Roda para baixo sobre código | Falha: posição permanece em 650 |
| Roda para cima sobre código | Passa, proteção |
| Seleção por arraste numa linha visível abaixo do início | Falha: posição vai de 900 para 0 |
| Seleção e formatação abaixo do início, preservando histórico | Falha: a seleção já desloca de 900 para 0 |

O último caso também especifica manutenção da seleção após negrito e desfazer sem alterar o texto. Essas verificações posteriores ainda não são alcançadas no RED.

Comando no espelho Linux:

```sh
dotnet test tests/Trama.IntegrationTests -c Release --no-restore --filter FullyQualifiedName~NotebookScrollingRegressionTests --logger 'console;verbosity=normal'
```

## Direção da correção, após aprovação

Permitir rolar a página com o ponteiro sobre o editor; manter a superfície contínua sem introduzir barras verticais em cada bloco. Impedir que a sincronização da seleção/formatação desloque a página para o começo. Preservar edição, seleção, histórico e os comportamentos anteriores. Investigar o encaminhamento dos eventos e as solicitações de visibilidade antes de escolher a alteração mínima.

Nenhum código de produção alterado nesta etapa. Pausa obrigatória para aprovação destes seis casos conforme CODEX, seção 9.

## Verificação da base anterior

Nesta execução dos 529 casos anteriores, 527 passaram e dois falharam em autosave. Na repetição focada, o caso de listas passou; o de autosave com caderno oculto atrás de Início continuou falhando por prazo de espera. Nenhuma implementação nem teste anterior foi alterado. A causa dessa falha permanece em investigação; não apresentar a base atual como totalmente verde com base na execução histórica. Resolver ou esclarecer essa pendência antes da entrega GREEN.

## Interface e pacote

A captura recebida não mostra a navegação global Início/Cadernos e ainda apresenta Backup na lateral. As fontes atuais já possuem a navegação global e ocultam o backup lateral quando hospedadas nesse shell. O executável da pasta normal `artifacts/publish/windows` tem data anterior ao pacote isolado da entrega de Início. Isso indica que a captura pode ser de uma versão anterior; confirmar usando o pacote isolado antes de atribuir toda a diferença ao layout atual.

A fidelidade visual ao protótipo permanece pendente, incluindo ícones, apresentação dos cadernos e backup. Métricas e tarefas fictícias do protótipo não devem entrar no produto. Não atualizar automaticamente a publicação normal nem tocar no perfil pessoal para essa comparação.
