# Refinamento do código e print no WSL - RED de 29/10/2026

## Validação manual informada pelo responsável

O usuário informou que validou as funcionalidades pelo executável Windows. No uso pelo WSL, os demais recursos funcionaram, mas copiar um print com Win+Shift+S no Windows e usar Colar print no Trama informou ausência de imagem. A conferência independente do colega ainda está pendente. Esse relato amplia a validação manual do exe; não é um teste automatizado nem comprovação em outros computadores.

## Realce: achado confirmado

NotebookCodeEditor recolore grupos de comentários, strings e números separadamente; todos os demais grupos nomeados recebem KeywordBrush. Nas definições do AvaloniaEdit, MethodCall e ValueTypeKeywords são categorias distintas, mas a aplicação as reduz à mesma cor usada em palavras-chave de controle. Isso explica parte da pouca diferenciação visual relatada. A apresentação final ainda depende de revisão visual; estes testes não fazem avaliação estética.

Proposta: cores semânticas separadas para chamadas de função e tipos primitivos, mantendo literais/comentários, seleção da linguagem e possibilidade futura de personalização. Preservar conteúdo e persistência. Este recorte usa regras lexicais do AvaloniaEdit; não promete análise semântica completa como uma IDE.

## Especificação executável para aprovação

Novo arquivo: [CodeHighlightingRegressionTests.cs](../../tests/Trama.IntegrationTests/Knowledge/CodeHighlightingRegressionTests.cs).

1. Chamadas de método C# distintas de palavras-chave de controle: RED.
2. Chamadas de função Python distintas de palavras-chave de controle: RED.
3. Tipos primitivos C# distintos de palavras-chave de controle, preservando alias/espaços da linguagem: RED.
4. Strings, números, comentários e palavras-chave mantendo quatro cores distintas: GREEN de proteção.
5. Alterar a cor de palavras-chave não recolorindo chamadas de função: RED.
6. Linguagem sem realce preservando o texto simples: GREEN de proteção.

Comando: `wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test`.

Build Release sem erros/avisos e formatação aprovados. Resultado: 304 casos executados, 300 aprovados e quatro falhas esperadas nos testes novos; 49 unitários e 251 integrações aprovados, quatro integrações falhando. Todos os 298 anteriores passaram. As falhas são asserções de cores iguais ou de recoloração indevida, sem erro de compilação/infraestrutura.

**Implementação não alterada.** Aguardar aprovação explícita dos seis casos, conforme seção 9 do CODEX. Após aprovação, implementar, repetir testes afetados, conferir visualmente e apresentar diff final.

## Print no WSL: diagnóstico ainda aberto

O adaptador atual pede DataFormat.Bitmap. O backend X11 do Avalonia 11.3.19 resolve esse formato para image/png ou image/jpeg; um formato como image/bmp não é convertido automaticamente por esse caminho. Isso é uma hipótese para investigar, não a causa confirmada neste computador.

Uma sonda externa com o backend real consultou somente os nomes dos formatos, sem ler ou salvar texto/imagem. Na consulta, foram anunciados Platform: TIMESTAMP, Universal: Text e Platform: TEXT. Não havia formato de imagem naquele instante. Como não foi garantido que a consulta ocorreu imediatamente após a captura, esse resultado não reproduz sozinho a falha relatada. Foi solicitado copiar novamente um print para repetir a consulta.

Fontes primárias: [mapeamento X11 do Avalonia 11.3.19](https://github.com/AvaloniaUI/Avalonia/blob/11.3.19/src/Avalonia.X11/Clipboard/ClipboardDataFormatHelper.cs) e [definição C# do AvaloniaEdit 11.3.0](https://github.com/AvaloniaUI/AvaloniaEdit/blob/11.3.0/src/AvaloniaEdit/Highlighting/Resources/CSharp-Mode.xshd).

Não instalar pontes de clipboard nem executar comandos Windows para ler imagens pessoais como correção presumida. Antes de corrigir, confirmar formatos e escrever regressão apropriada; validar depois com clipboard real. O teste headless anterior não comprovava a ponte Windows/WSLg. Enquanto se investiga, salvar o print em PNG/JPEG e usar + Imagem é uma alternativa manual a conferir no ambiente do usuário.

## Esforço e limites

Um agente, sem novos subagentes. Uma rodada da suíte após acrescentar seis casos e uma consulta de formatos. Nenhum custo monetário ou número de tokens foi medido. Para economizar, o próximo ciclo pode focar a classe nova antes da suíte final; reaproveitar o ambiente já preparado. Não alterar os testes aprovados nem anunciar GREEN com quatro falhas abertas. Riscos: definições lexicais variam por linguagem; observar mutação de paleta sem vazamento de assinaturas de eventos; não generalizar o relato Windows para outras máquinas.
