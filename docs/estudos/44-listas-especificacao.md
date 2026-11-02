# 44 · Listas: especificação para revisão

Estado em 31/10/2026: testes preparados, implementação ainda não iniciada. A aprovação dos casos é necessária conforme a seção 9 do [CODEX](../../CODEX.md).

Atualização posterior: os 32 casos e a versão 4 foram aprovados e implementados; consulte o [capítulo 45](45-listas-implementadas.md). O restante deste documento preserva a especificação e as evidências da fase RED.

## Objetivo e contrato proposto

Adicionar listas com marcadores e numeradas à prosa, usando a barra compartilhada de formatação. A opção sem lista remove apenas a estrutura de lista; texto, negrito, itálico, tamanho, cores e marca-texto permanecem.

Marcadores são apresentação, sem inserir caracteres artificiais no texto salvo. Cada entrada pertence ao início de um parágrafo, por posição UTF-16. Exemplo de metadados aditivos no bloco:

```json
{"Lists":[{"Offset":0,"Kind":"bullet"},{"Offset":3,"Kind":"numbered"}]}
```

Para `Um\nDois`, os inícios são 0 e 3; para `Um\r\nDois`, são 0 e 4. Um último item vazio pode começar no comprimento do texto. Rejeitar posições fora dos inícios de parágrafo, posições duplicadas e tipos desconhecidos. `none` é opção do controle, não um tipo persistido.

Páginas com listas passam a exigir versão de conteúdo **4**. Manter os formatos anteriores: 1 para conteúdo simples, 2 para negrito/itálico e 3 para os atributos adicionais. Ao remover listas, calcular a versão pelos atributos restantes. O formato do pacote de backup permanece 1. O armazenamento físico e eventual migração serão definidos na implementação; nenhum esquema foi alterado nesta etapa.

## Comportamento do editor

Aplicar ao parágrafo do cursor ou aos parágrafos cobertos pela seleção. Enter continua uma lista; em item vazio, encerra a lista sem acrescentar outra linha vazia. Dividir ou unir parágrafos recalcula posições e preserva estilos. A numeração é derivada da sequência de itens, sem contaminar o texto.

O seletor `ProseListPicker` pertence à barra existente e oferece sem lista, marcadores e numeração. Não deve formatar código nem recriar os editores ao recolher a barra. Mudanças participam de desfazer/refazer e continuam recuperáveis após Salvar/autosave na página aberta.

## Especificação executável

Arquivo: [ParagraphListsTests.cs](../../tests/Trama.IntegrationTests/Knowledge/ParagraphListsTests.cs).

| Grupo | Casos | O que protege |
| --- | ---: | --- |
| Contrato | 13 | JSON, posições UTF-16, tipos válidos, item vazio e atributos existentes |
| Editor | 13 | Cursor/seleção, conversão, Enter, união, histórico, autosave e barra compacta |
| Persistência | 6 | Reabertura, versão, código/imagem, lixeira e backup/restauração |
| Total | 32 | 29 falhas esperadas e três proteções aprovadas |

Os testes compilam contra o código atual. A desserialização atual descarta os metadados desconhecidos, a validação não os verifica e o seletor ainda não existe. Os casos de interface param nessa ausência; as verificações posteriores de edição só serão exercitadas quando o controle existir. Não tratar o RED como prova de todos os passos internos desses fluxos.

Execução completa no WSL: 486 casos, 457 aprovados, 29 falhas esperadas, nenhum ignorado. Os 454 anteriores passam. Build e verificação de formatação passaram. Consulte o [relatório 052](../reviews/052-paragraph-lists-red.md).

## Limites e fechamento

Esta fatia cobre listas simples. Checklist, sublistas, estilos alfabéticos e numeração configurável ficam para evolução posterior. A conferência visual de quebra de linha, recuos e numeração em janelas largas/compactas será necessária após implementar; testes de contrato não a substituem.

Depois das listas, a proposta de fechamento inclui exportação básica, validação integrada Windows/Linux e revisão do diff. São etapas pendentes, não funcionalidades entregues. O [roadmap](../../rodmap.md) concentra esse acompanhamento. Manual e PDF não foram regenerados.
