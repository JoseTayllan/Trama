# Realce de código e paletas semânticas

29/10/2026. Implementação após aprovação dos seis casos de CodeHighlightingRegressionTests. Os seis passaram sem alteração; a suíte completa passou 304 casos. A conferência visual com dados sintéticos também passou. A aparência final fica para revisão do responsável.

## Por que o código parecia ter poucas cores

O AvaloniaEdit reconhece grupos lexicais como MethodCall, ValueTypeKeywords, Comment e String. O Trama já recoloria comentários, strings e números, mas atribuía a cor de palavras-chave a todos os outros grupos nomeados. Assim, `if`, `int` e `Salvar()` acabavam com a mesma cor.

O ajuste mantém os grupos existentes e acrescenta FunctionBrush e TypeBrush. No tema atual:

| Grupo | Aparência | Exemplo |
| --- | --- | --- |
| Palavras-chave | Lilás | `if`, `return` |
| Tipos reconhecidos pela gramática | Azul claro | `int` em C# |
| Chamadas reconhecidas pela gramática | Amarelo | `Salvar()` |
| Strings | Verde | `"Estudar"` |
| Números | Laranja | `42` |
| Comentários | Azul acinzentado | `// revisar` |

É realce lexical, não uma análise semântica completa como a de uma IDE. A gramática de cada linguagem determina quais nomes são identificados; não prometemos reconhecer todas as classes, declarações, funções nativas ou construções modernas. Em Python, por exemplo, uma função nativa pode pertencer ao grupo de palavras-chave da definição fornecida pela biblioteca.

## Como funciona no código

1. [NotebookCodeEditor](../../src/Trama.Desktop/Views/NotebookCodeEditor.cs) seleciona a definição por linguagem e carrega o XML do AvaloniaEdit.
2. Os grupos nomeados são associados aos brushes semânticos; não se modifica o texto digitado.
3. [Palette.axaml](../../src/Trama.Desktop/Themes/Palette.axaml) centraliza as novas cores TramaColorSyntaxFunction e TramaColorSyntaxType.
4. [NotebookEditorView](../../src/Trama.Desktop/Views/NotebookEditorView.axaml) conecta os brushes por DynamicResource.
5. O controle observa substituição e mutação dos brushes enquanto está na árvore visual; retira as assinaturas ao sair e as refaz ao entrar.

Essa estrutura deixa a personalização futura desacoplada do conteúdo. A conferência visual alterou o recurso de funções durante a execução: a cor mudou sem modificar o código ou a cor de tipos. Isso não significa que já exista uma tela de preferências ou persistência de temas personalizados.

## Testes e limites

[Seis regressões](../../tests/Trama.IntegrationTests/Knowledge/CodeHighlightingRegressionTests.cs) verificam diferenciação C#/Python, tipos C#, independência da cor de funções ao mudar palavras-chave, cores dos literais e texto simples. Quatro falharam no RED pelo comportamento ausente; os seis passaram depois da implementação, mantendo os casos aprovados intactos.

Build Release sem avisos/erros, formatação e 304 testes passaram no Linux/WSL. O harness externo com Skia conferiu bindings reais da view, atualização dinâmica e preservação do texto, e gerou uma prévia. Um novo pacote Windows foi publicado em pasta isolada. A revisão humana da aparência e o teste independente do colega continuam pendentes.

Não houve alteração no banco, migração, conteúdo das páginas ou formato dos backups.

## Dependências entre publicação Windows e desenvolvimento Linux

Uma publicação `win-x64` pode acrescentar grafos específicos de runtime aos locks NuGet. Isso fez a restauração Linux em modo travado falhar com NU1004. O [helper WSL](../../tools/run-wsl.sh) agora mantém apenas o grafo genérico desses locks **na cópia Linux**, preservando versões e hashes. A restauração continua usando `--locked-mode`; os locks do projeto Windows ficam intactos. Python 3 é necessário para essa preparação, além do ambiente já documentado no [guia WSL](../operacao/wsl/README.md).

## Print Windows para WSL: resultado separado

A investigação consultou somente nomes de formatos. O Windows anunciou Bitmap e PNG; o Wayland anunciou image/bmp; X11 e Avalonia anunciaram somente formatos de texto, mesmo após reinício autorizado do WSL. Isso localiza a incompatibilidade na integração usada pelo aplicativo Linux. Não foi lida nem salva a imagem do usuário.

O adaptador de clipboard não foi alterado nesta entrega. Uma correção futura deve testar aquisição de BMP no Wayland, conversão para PNG gerenciado, limites, cancelamento, falhas e preservação do rascunho antes de implementação. Evitar adicionar processos em segundo plano ou depender de comandos de shell arbitrários. Por enquanto, usar o exe Windows validado, ou salvar o print como PNG/JPEG e usar + Imagem no Linux.

## Perguntas para estudar

1. Por que um token reconhecido pela gramática não equivale a um símbolo identificado semanticamente?
2. Por que observar a mutação do brush, além da troca de sua referência?
3. Como o recurso dinâmico evita gravar preferências de aparência dentro da página?
4. Por que remover um grafo de runtime da cópia Linux é diferente de atualizar versões de pacotes?
5. Por que uma imagem disponível no Wayland pode não estar disponível na API de clipboard X11?

Veja o [relatório da entrega](../reviews/030-code-highlighting-green.md) e o [RED anterior](../reviews/029-code-highlighting-red-and-wsl-clipboard.md).
