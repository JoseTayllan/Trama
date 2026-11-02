# Lixeira: migração, backup e autosave

Atualização: os dez casos foram aprovados e passaram após as correções. Ver [entrega integrada](42-lixeira-entrega-integrada.md). Abaixo permanece o registro da especificação e dos três casos em RED antes da implementação.

31/10/2026. Segunda fatia de testes, preparada após os 18 fluxos aprovados passarem. Contém somente `LibraryTrashSafetyTests.cs` e documentos adicionais; a implementação dos fluxos ficou congelada durante a especificação. Nenhuma correção dos novos casos foi antecipada.

## Dez casos para revisão

| Casos | Objetivo | Resultado esperado nesta etapa |
| --- | --- | --- |
| 1 | Atualizar o esquema anterior mantendo conteúdo rico, anexos, tags e localização | Proteção passando |
| 1 | Backup/restauração de exclusões em lotes distintos, sem o perfil original | Proteção passando |
| 1 | Restaurar pacote anterior à lixeira e atualizar seu esquema | Proteção passando |
| 1 | Recusar backup com atributos/versão inconsistentes numa página excluída | RED |
| 1 | Recusar backup com raiz da lixeira cuja entidade não existe | RED |
| 1 | Ocultar nota legada excluída da busca e restaurar sua identidade | RED |
| 2 | Cancelar envio/restauração sem mudar o estado anterior | Proteções passando |
| 1 | Falhar no meio da cascata e reverter todas as marcações | Proteção passando |
| 1 | Disparar o debounce real após exclusão e impedir que autosave reative/sobrescreva a página | Proteção passando |

São bancos temporários com SQLite real. Um trigger sintético provoca a falha de cascata; um relógio de teste dispara o mesmo mecanismo de debounce do produto. O pacote histórico é construído com o esquema anterior e o formato 1 existente; não depende de um arquivo pessoal. Os testes não mudam interfaces internas para conseguir compilar.

## Por que há três falhas

A busca reúne páginas estruturadas e notas antigas. A primeira fatia filtrou a parte estruturada, mas o laço legado ainda inclui o item excluído. O caso novo falha no resultado retornado pela busca.

O backup preserva o banco e os anexos, inclusive itens excluídos. Porém, o validador usa a leitura pública que oculta a lixeira: acaba sem verificar atributos/versão das páginas escondidas. Além disso, a tabela polimórfica de exclusões não tem FK direta para os três tipos; a integridade de suas referências precisa ser verificada explicitamente. Os dois casos falham porque o backup é aceito quando deveria ser recusado.

## Correções propostas após aprovação

Aplicar a filtragem também às notas legadas. Manter leitura pública ocultando excluídos e oferecer um caminho interno de validação que examine o conteúdo armazenado integralmente. Validar referências e a consistência básica dos lotes na criação/restauração de backup, preservando compatibilidade com pacotes anteriores. Não alterar os testes aprovados, remover conteúdo, mudar o formato do pacote sem necessidade nem usar dados pessoais para validar a migração.

Depois das correções: executar os dez casos, os 18 fluxos, a suíte toda e o harness; preparar um pacote isolado para revisão. A entrega completa depende desse GREEN e da revisão humana do diff. Ver [relatório](../reviews/050-library-trash-safety-red.md).
