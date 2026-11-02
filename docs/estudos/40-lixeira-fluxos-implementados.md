# Lixeira: primeiros fluxos implementados

Atualização: as correções complementares foram aprovadas e implementadas na [entrega integrada](42-lixeira-entrega-integrada.md), com 454 testes aprovados. Este capítulo registra o primeiro GREEN e seus limites naquele momento.

31/10/2026. Implementação dos 18 casos aprovados. Espaços, cadernos e páginas podem ir à lixeira e voltar com seus IDs, estilos, tags, código e imagens. Esta fatia ainda não encerra a entrega: os [testes complementares](41-lixeira-seguranca-especificacao.md) identificam correções pendentes antes da versão final.

## Persistência e contratos

`ILibraryTrashActions` é um contrato novo e aditivo, separado de renomeação/movimentação. Oferece prévia do impacto, envio, listagem e restauração. Os construtores anteriores de LibraryViewModel foram preservados; a composição injeta a lixeira por um novo overload.

A sexta migração cria `LibraryTrash`. Ela guarda tipo/ID do item, lote da exclusão, indicação de raiz, título e data UTC. Nenhuma tabela de conteúdo é removida ou esvaziada. Excluir um espaço reúne seus descendentes ativos numa transação; os itens anteriormente excluídos pertencem a outros lotes. Restaurar remove apenas as marcações do lote escolhido. Essa separação evita ressuscitar exclusões anteriores.

`LibraryTrashVisibility` permite consultar bancos históricos que ainda não têm a tabela. Catálogo, leitura e gravação de páginas, tags e renomeações consideram a lixeira; a busca estruturada filtra páginas excluídas. A gravação verifica a marcação dentro da transação para recusar snapshots atrasados. A revisão complementar encontrou um caminho legado da busca que ainda precisa aplicar a mesma regra.

## Fluxo da interface

Excluir respeita o rascunho e pausa autosave usando o mecanismo de navegação existente. A confirmação informa o impacto e mantém Cancelar como ação padrão. Depois da exclusão, o editor antigo é descartado antes de reabilitar a navegação. A restauração atualiza as listas sem substituir o editor de outro rascunho.

O diálogo Lixeira lista raízes das exclusões, diferenciando Espaço/Caderno/Página e data. Restaurar fica bloqueado com orientação quando a origem continua excluída. Não existe exclusão permanente ou esvaziamento automático nesta fatia. Os botões Excluir são ações recuperáveis; a lixeira fica no rodapé da lateral.

## Evidências e limites

Os 18 casos passam intactos; a suíte inteira chegou a **444 aprovados**, zero falhas/ignorados, antes de acrescentar os testes complementares. Build e formatação conferidos. Um harness SQLite/Skia com perfil fictício validou confirmações, página excluída antes do espaço, bloqueio de restauração, origem restaurada antes da página, lixeira vazia e conteúdo reaberto. As capturas foram inspecionadas.

Somente bancos de teste/fictícios receberam a sexta migração nesta execução. O perfil de uso não foi aberto pela implementação nova. Não foi gerado pacote final de distribuição: faltam as correções de backup e compatibilidade legada descritas no capítulo seguinte. Manual/PDF permanecem intactos. Ver [relatório GREEN da fatia](../reviews/049-library-trash-green.md).

Para estudar: diferencie apagar conteúdo de remover a marcação de exclusão; siga o ID do lote na cascata; observe por que uma restauração de pai não deve abranger todos os seus filhos indiscriminadamente; compare atualização de lista com substituição do editor.
