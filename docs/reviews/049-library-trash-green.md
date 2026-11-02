# Lixeira: GREEN dos 18 fluxos aprovados

31/10/2026. Implementação após aprovação explícita dos 18 casos. Todos preservados integralmente, junto dos 426 anteriores. A sexta migração foi gerada pelo dotnet-ef 10.0.9 fixado no projeto; migrations anteriores e dependências preservadas. Sem uso do banco pessoal, alteração de segurança ou atualização de manual/PDF.

## Implementação

Contrato novo ILibraryTrashActions; armazenamento por lote e raiz em LibraryTrash; cascatas e restauração transacionais; consultas ativas e bloqueio de snapshots excluídos; injeção opcional compatível com os construtores antigos; confirmação, pausa de autosave, diálogo de lixeira e atualização das listas. Não há remoção física de conteúdo/anexos nem esvaziamento permanente.

A revisão corrigiu o estado explícito IsEnabled do botão Restaurar, que não refletia sozinho a propriedade do comando no contrato testado. Também preservou o overload anterior do construtor de LibraryViewModel e aplicou as convenções ao arquivo de migração gerado. Não foram alterados testes aprovados para obter GREEN.

## Verificação da fatia

- Foco: 18 aprovados, zero falhas/ignorados.
- Suíte completa: **444 aprovados** (77 unitários, 367 de integração), zero falhas/ignorados.
- Build Release sem avisos/erros e formatação aprovados.
- Harness SQLite/Skia: confirmações reais, cascata, filho bloqueado até restaurar a origem, restauração por lote, lixeira vazia, navegação e IDs/estilos preservados. Capturas inspecionadas em perfil sintético.

Resultados TRX e diff desta implementação em `trama-library-trash-green`. Os testes complementares em RED foram acrescentados depois e têm relatório separado; não apresentar 444 aprovados como resultado de toda a árvore atual, que contém esses casos novos.

## Pendências materiais

Os dez casos de segurança/compatibilidade revelam três correções necessárias: notas legadas ainda podem aparecer na busca quando excluídas; o validador de backup pula o conteúdo rico de páginas excluídas; metadados de lixeira sem origem não são recusados pelo backup. Ver [relatório RED complementar](050-library-trash-safety-red.md). Nenhuma correção desses casos foi antecipada antes da revisão dos testes.

A primeira fatia está implementada; a funcionalidade completa e sua publicação final continuam pendentes das correções e da revisão humana do diff/UI. Não foi iniciada a aplicação nova no perfil de uso Windows/Linux. Esforço moderado, um agente, sem subagentes; reutilizar a fixture, o harness e o mapa de consultas nas correções reduz custo.
