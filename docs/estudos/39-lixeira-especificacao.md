# Lixeira de espaços, cadernos e páginas: especificação

31/10/2026. O responsável incluiu explicitamente os espaços e autorizou seguir para a especificação. Esta entrega contém somente testes e documentos, sem exclusão implementada, alteração de dados pessoais, novos contratos de produção ou migração.

## Fluxos da primeira fatia

Os 18 casos em `LibraryTrashWorkflowTests.cs` usam a interface real com SQLite temporário, importação de imagem, formatação e tags. Não introduzem serviço falso, implementação vazia ou tipos de produção apenas para compilar. Os pontos de acesso esperados são ações de enviar o espaço/caderno/página à lixeira, confirmação com cancelar, acesso à lista da lixeira e restauração do item selecionado. Nomes dos controles identificam as ações, sem definir coordenadas ou detalhes estéticos.

| Grupo | Casos | Comportamento |
| --- | ---: | --- |
| Excluir e restaurar | 3 | Um caso por nível, preservando IDs, hierarquia, estilos, código, tags e imagem; outro espaço permanece ativo |
| Cancelar confirmação | 3 | Nenhum nível é excluído ao cancelar |
| Cascata de espaço | 1 | Todos os cadernos/páginas ativos do espaço voltam juntos |
| Exclusões anteriores | 2 | Restaurar caderno/espaço não revive filhos excluídos antes |
| Pais na lixeira | 2 | Página/caderno não restaura enquanto o caderno/espaço pai estiver excluído; mostrar orientação |
| Busca | 1 | Itens excluídos somem de busca textual e tag exata; restauração recupera a busca |
| Rascunhos | 2 | Cancelar preserva editor/seleção/histórico; salvar antes preserva a versão recente ao restaurar |
| Persistência | 1 | Nova composição acessa a exclusão persistida no SQLite |
| Gravação atrasada | 1 | Snapshot antigo não consegue reativar uma página excluída |
| Proteções existentes | 2 | Fixture preserva atributos/anexo e diferencia cadernos homônimos em espaços diferentes |

## Como interpretar o RED

As ações de lixeira ainda não existem na interface. Os casos novos devem falhar com essa ausência de comportamento visível, não por compilação, banco inexistente ou serviço faltando. Os passos posteriores dos fluxos ficam especificados, mas não são alcançados neste RED: preservação após restauração e cascatas só serão comprovadas quando a implementação permitir executar o percurso inteiro. Não alegar que a falha de cada regra interna de persistência foi isoladamente reproduzida.

## Fatias seguintes obrigatórias

Após revisão destes casos: definir contrato tipado de aplicação e representação persistente das exclusões por operação, com identificadores de grupo para preservar exclusões anteriores. Implementar a primeira fatia com transações, filtragem consistente de consultas e bloqueio de gravação em itens excluídos. Preservar APIs e testes anteriores.

Antes de considerar a lixeira entregue, complementar a cobertura específica de migração de bancos antigos, inclusão da lixeira no backup e restauração de pacotes antigos, cancelamento/falhas de persistência e concorrência real com autosave. Esses casos seguem o mesmo processo de revisão antes da implementação correspondente. Não tratar os 18 fluxos como cobertura completa desses riscos.

O plano exclui apagar definitivamente, esvaziar automaticamente e coletar imagens órfãs. Restaurar filhos exige pais ativos, com mensagem clara; não mudar o destino silenciosamente. Espaços, cadernos e páginas restauram com seus IDs originais. Ver [relatório](../reviews/048-library-trash-red.md) e [plano geral](38-fechamento-dos-cadernos-e-lixeira.md).
