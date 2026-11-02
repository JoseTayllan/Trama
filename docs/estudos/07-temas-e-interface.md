# Interface e preparação para paletas personalizadas

## Objetivo e entrega

Refinamos o editor com uma barra lateral, hierarquia de títulos, superfícies discretas, campos mais legíveis e estados visuais consistentes. O conteúdo se adapta à altura da janela, mantendo o botão Salvar acessível no tamanho mínimo definido. A criação de notas e os contratos de persistência continuam os mesmos.

Esta entrega prepara a troca de cores. Ainda não há tela de personalização, seleção de temas ou gravação das preferências do usuário.

## Como as cores chegam à tela

O arquivo [Palette.axaml](../../src/Trama.Desktop/Themes/Palette.axaml) concentra as cores por função: fundo da janela, superfície, texto, borda, destaque e texto sobre destaque. Um nome como `TramaColorAccent` descreve o papel da cor, sem exigir que ela seja sempre verde.

Cada cor alimenta um pincel com `DynamicResource`. As Views também usam recursos dinâmicos para consumir esses pincéis. Assim, substituir uma cor atualiza os componentes já exibidos, sem reconstruir a janela.

```text
TramaColorAccent → TramaAccentBrush → botão, ícones e foco
```

O [App.axaml](../../src/Trama.Desktop/App.axaml) carrega a paleta e os [estilos compartilhados](../../src/Trama.Desktop/Themes/Controls.axaml). Esses estilos cobrem campos e botões, incluindo foco, passagem do mouse, pressionamento e desabilitação. Novas telas devem reutilizar esses papéis; evite cores hexadecimais diretamente nas Views.

Exemplo ilustrativo para uma futura aplicação de preferências, executado na thread da interface:

```csharp
Application.Current!.Resources["TramaColorAccent"] = Color.Parse("#C9B5FF");
```

O exemplo usa tipos de Avalonia e Avalonia.Media. Ele não representa um serviço de configuração já implementado. Uma paleta coerente altera também os papéis relacionados, como texto sobre destaque, destaque pressionado e fundos.

## O que falta para o usuário escolher as cores

- Especificar e revisar os comportamentos antes de implementar o editor de paletas.
- Oferecer prévia, aplicação, cancelamento e restauração do padrão.
- Persistir preferências no diretório local apropriado e carregá-las na inicialização.
- Validar valores e contraste entre texto, fundos, campos e estados de interação.
- Definir um formato controlado, como JSON com papéis permitidos; não carregar XAML arbitrário fornecido pelo usuário.
- Tratar preferências inválidas com retorno seguro à paleta padrão.

A base atual é escura. Uma futura paleta clara precisa de verificação de todos os papéis e controles. A barra de título nativa e as cores da imagem da logo não são recoloridas pelos recursos da aplicação.

## Verificação desta entrega

Build Release sem avisos, formatação aprovada e **57 testes existentes aprovados: 14 unitários e 43 de integração**, sem modificar seus arquivos. Não foram criados testes que apenas repetem valores de estilo.

Também renderizamos a janela com dados sintéticos em 1200 × 800 e 900 × 680 e uma alternativa violeta aplicada à mesma janela. As imagens foram inspecionadas, incluindo a visibilidade do botão Salvar. Essa prévia não acessou o banco pessoal e não substitui uma avaliação de teclado, escala do Windows e acessibilidade na execução nativa.

Os estados dos controles usam partes dos templates Fluent da versão instalada. Ao atualizar Avalonia, revisar os seletores contra os fontes oficiais de [TextBox](https://github.com/AvaloniaUI/Avalonia/blob/11.3.19/src/Avalonia.Themes.Fluent/Controls/TextBox.xaml) e [Button](https://github.com/AvaloniaUI/Avalonia/blob/11.3.19/src/Avalonia.Themes.Fluent/Controls/Button.xaml).

## Perguntas de estudo

1. Por que chamar um recurso de Accent em vez de Verde?
2. O que a cadeia de DynamicResource permite atualizar?
3. Por que trocar apenas a cor do botão pode prejudicar a leitura?
4. O que diferencia uma base para temas de um editor de paletas completo?
