# Tamanho, cor e destaque no modelo de texto

30/10/2026. Primeira fatia implementada após aprovação explícita dos 28 casos. **353 testes aprovados**, build Release sem avisos/erros e formatação aprovada no WSL. Esta entrega prepara o modelo; a interface ainda não oferece os novos controles. A hierarquia de navegação prevista permanece Espaço → Caderno → Páginas.

## O que mudou

Em [InlineTextDocument.cs](../../src/Trama.Application/Knowledge/InlineTextDocument.cs), `TextStyleSpan` conserva o construtor posicional de três argumentos e recebe propriedades opcionais `init`: `FontSize`, `Foreground` e `Highlight`. Isso preserva construção/deconstrução antigas e permite copiar um intervalo com `with`, mantendo todos os atributos. A igualdade do record também considera as propriedades novas.

Os valores de cor são identificadores semânticos, não objetos de interface nem códigos de cor acoplados ao tema. Tamanho usa unidades lógicas, com valores 12, 14, 16, 18, 24 e 32. O construtor do documento valida tamanhos, cores, flags, limites e sobreposições. Um intervalo sem negrito/itálico é válido quando contém tamanho, cor ou destaque; um intervalo sem atributo algum continua inválido.

Propriedades nulas são omitidas na serialização, por `JsonIgnoreCondition.WhenWritingNull`. Assim, intervalos antigos continuam produzindo somente `Offset`, `Length` e `Style`. JSON antigo sem os novos campos continua carregando com valores nulos. Isso preserva o formato legado; não substitui o versionamento de páginas enriquecidas, que ainda será implementado na infraestrutura.

## Como a edição preserva atributos

`SetStyle` divide os intervalos nas bordas necessárias e altera somente as flags solicitadas. As cópias mantêm tamanho/cor/destaque. Remover o último negrito não apaga um intervalo que ainda possui outro atributo.

`ReplaceText` herda o intervalo completo na posição inicial da edição, seguindo a regra anterior: dentro do texto, consultar a posição inicial; ao inserir no fim, consultar o caractere anterior. Prefixos/sufixos sobreviventes mantêm metadados e recebem offsets/limites atualizados. Inserir em um trecho sem formatação não captura indevidamente a cor de outro intervalo.

A canonicalização só une intervalos vizinhos quando flags, tamanho, cor e destaque são iguais. Dois trechos em negrito, um azul e outro verde-água, continuam separados. Os offsets permanecem UTF-16, como no AvaloniaEdit e na implementação anterior; não são índices de grafemas.

## Evidências e limites

Os [28 testes aprovados](../../tests/Trama.UnitTests/Knowledge/RichTextAttributesTests.cs) ficaram intactos e passaram, junto com os 325 anteriores. São 77 unitários e 276 de integração, sem falhas ou casos ignorados. Foram exercitados serialização, combinação, intervalos adjacentes, edição, emoji, cópia defensiva, rejeição de valores inválidos e compatibilidade anterior. Ver [relatório](../reviews/037-rich-text-attributes-green.md).

A única alteração de produção está no modelo de texto. Não foram criados comandos de aplicar tamanho/cor, seletor visual, renderização de atributos, migrações, pacotes ou nova publicação Windows. O manual/PDF permanece intacto. A interface atual não cria os novos atributos, e esta entrega não declara suporte de persistência versionada para eles.

Próxima fatia: testes de salvar/reabrir e restaurar com proteção das versões de conteúdo, antes de disponibilizar a edição rica na interface. Depois, testar ações por seleção, limpar, estados mistos, foco e histórico completo; por fim, aplicar a identidade visual. Não mudar testes aprovados para adaptar a integração.

Para estudar: como `with` evita perder propriedades ao editar offsets? Por que a igualdade precisa considerar os atributos novos? Por que omitir nulos preserva o JSON antigo, mas não torna páginas enriquecidas seguras para aplicativos antigos?
