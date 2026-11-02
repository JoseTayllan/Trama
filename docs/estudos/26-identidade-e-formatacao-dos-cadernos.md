# Cadernos Trama: identidade e formatação

30/10/2026. Direção conceitual aceita pelo responsável, com hierarquia explícita Espaço → Caderno → Páginas. Modelo, persistência, controles e composição passaram ao GREEN após aprovação dos respectivos testes; suíte atual com 414 aprovados. Ver [modelo](27-atributos-de-texto-implementados.md), [persistência](29-persistencia-de-atributos-implementada.md), [barra](31-controles-de-formatacao-implementados.md) e [identidade/contexto](33-identidade-e-hierarquia-implementadas.md). Revisão estética/interativa permanece pendente. Manual e PDF sem alterações.

## Objetivo e diagnóstico

Escrever conteúdo longo com hierarquia, cor nas letras e marca-texto por seleção, combináveis com negrito e itálico. A página deve parecer um caderno de estudos do Trama: leitura contínua, navegação clara e ferramentas discretas. A direção é futurista, com foco em legibilidade, sem depender de brilho, animações ou decoração constante.

No diagnóstico inicial, o editor repetia ações e barra N/I em cada bloco, com tamanho 16 e metadados apenas de negrito/itálico. A escrita rica acrescentou atributos/barra compartilhada e a composição posterior trouxe contexto explícito, superfície contínua e margem. A estrutura de blocos continua útil para dados/código e permanece preservada, sem converter ou perder páginas existentes.

Referências reais: [InlineTextDocument](../../src/Trama.Application/Knowledge/InlineTextDocument.cs), [editor de prosa](../../src/Trama.Desktop/Views/NotebookProseEditor.cs), [view](../../src/Trama.Desktop/Views/NotebookEditorView.axaml), [ações de escrita](../../src/Trama.Desktop/ViewModels/NotebookEditorViewModel.Writing.cs), [repositório SQLite](../../src/Trama.Infrastructure/Persistence/SqliteNotebookRepository.cs), [validação de backup](../../src/Trama.Infrastructure/Persistence/LocalBackupService.Validation.cs) e [paleta](../../src/Trama.Desktop/Themes/Palette.axaml).

## Direção visual proposta

A navegação precisa distinguir os três níveis por rótulo, recuo e relação visual, além de cor. Exemplo: **Espaço: Estudos → Caderno: Curso C# → Página: Interfaces e contratos**. Um espaço contém vários cadernos; cada caderno contém várias páginas; cada página recebe texto, código e imagens. Texto/código/imagem são blocos da página, não novos níveis da biblioteca.

Manter a página selecionada identificada na lateral e mostrar os três níveis no topo do editor. Não apresentar nomes de espaços/cadernos/páginas como se fossem uma lista plana. A ação de criar deve dizer se cria espaço, caderno ou página e informar o destino; resultados de busca com nomes repetidos precisam mostrar sua localização. Recolher a lateral em janela estreita não deve apagar o caminho da página aberta. Cadernos aninhados não fazem parte desta hierarquia.

- Biblioteca com hierarquia Espaço → Caderno → Página e faixa de identificação do caderno. Usar a marca existente, sem substituir a logo.
- Uma superfície contínua de escrita; evitar moldura e barra permanente em cada parágrafo.
- Margem com um traço discreto e marcadores de seção. Na primeira entrega, marcadores identificam blocos e não prometem geração automática de sumário.
- Uma barra de formatação do editor, vinculada ao último bloco de prosa ativo. Controles preservam a seleção ao receber foco.
- Código como exemplo técnico dentro da página, com linguagem e realce atuais; sem contadores de execução, botão de executar ou decoração de console que sugira execução inexistente.
- Imagens integradas ao fluxo, com legenda e proporção preservada.
- Ações de mover/remover ficam acessíveis por botão de contexto e teclado, inclusive sem mouse. Não esconder funções essenciais apenas em hover.
- Em janela estreita, recolher a navegação secundária e reorganizar a barra; preservar área de escrita, Salvar, foco e mensagens reais de erro/salvamento.

A prévia visual anexada à entrega é um estudo estático com conteúdo sintético. Não comprova funcionalidades nem substitui a validação do Avalonia em Windows e WSL.

## Primeira entrega: formatação por seleção

| Recurso | Comportamento proposto |
| --- | --- |
| Tamanho | 12, 14, 16, 18, 24 e 32 unidades lógicas do Avalonia; padrão herdado do tema. Não rotular como pontos tipográficos. |
| Cor da letra | Padrão/automática e cores semânticas nomeadas: verde-água, azul, violeta, âmbar e coral. |
| Marca-texto | Sem destaque e destaques nomeados: âmbar, verde, azul e violeta, com combinações de primeiro plano legíveis. |
| Negrito e itálico | Continuam combináveis e preservam os demais atributos. |
| Limpar formatação | Remove os atributos apenas da seleção; conteúdo e demais trechos permanecem. |
| Seleção vazia | Não modifica conteúdo nem histórico; orientar a selecionar um trecho. Estilo para digitação futura fica fora desta primeira entrega. |
| Seleção com estilos mistos | Mostrar estado misto, sem atribuir um valor único incorreto. Aplicar um atributo mantém os demais. |

Cores semânticas do conteúdo e cores da interface são responsabilidades distintas. Salvar identificadores estáveis de cor, resolvidos pelo tema, permite adaptar o contraste quando a paleta mudar. Não salvar um objeto `Brush`, chave arbitrária de recurso ou cor da sintaxe como estilo de texto. A seleção nativa e o marca-texto precisam continuar visualmente distinguíveis.

A barra oferece cores predefinidas. Seletor hexadecimal livre e editor de paletas permanecem no plano de personalização. O modelo aprovado usa campos opcionais `FontSize`, `Foreground` e `Highlight` nos intervalos serializados: tamanhos da tabela, primeiro plano com identificadores `teal`, `blue`, `violet`, `amber`, `coral`, e destaque com `amber`, `green`, `blue`, `violet`. Ausente/nulo significa herdar o padrão ou não destacar. Modelo, persistência, controles e renderização estão conectados. As 36 combinações avaliadas da paleta padrão tiveram contraste mínimo 5,882:1; preferências de cor livres ainda não foram implementadas.

## Contratos, persistência e compatibilidade

Cor e tamanho não são flags booleanas como negrito/itálico. Usar atributos opcionais por intervalo e manter compatibilidade com os construtores/consumidores atuais. A canonicalização deve juntar intervalos somente quando todos os atributos forem iguais. Inserção, substituição e remoção precisam atualizar todos os atributos segundo a regra de herança já usada pela edição atual.

O formato atual reconhece páginas 1, 2 e 3; os estilos são JSON em `NotebookTextStyles`. Atributos novos exigem 3 para impedir que versões antigas os ignorem e apaguem no próximo salvamento. Escritor, leitor e backup foram coordenados na entrega de persistência. Páginas sem os novos atributos continuam usando a versão anterior aplicável. O pacote de backup permanece 1, independentemente da versão das páginas.

Não editar migrações antigas nem presumir que mudar JSON exige nova tabela. Definir a necessidade de migração após testes de compatibilidade. Instalações antigas devem rejeitar com mensagem clara um conteúdo que não conseguem interpretar, preservando o arquivo e os dados. Backups antigos continuam restauráveis; restauração não pode remover silenciosamente formatação desconhecida.

Histórico deve capturar tamanho, cor e destaque junto com texto/negrito/itálico. Manter limite e isolamento existentes, autosave após dois segundos e histórico preservado após Salvar. Nenhum teste anterior aprovado pode ser enfraquecido para adaptar o novo modelo.

## Ordem de trabalho e testes de aceite

1. **Modelo de formatação:** preparar testes RED de tamanho, cores, destaque, sobreposição, remoção parcial, limpar, intervalos, herança na edição e texto UTF-16/emoji. Usar contratos aditivos mínimos; falha de compilação não vale como RED. Parar para revisão dos testes antes de implementar.
2. **Persistência e recuperação:** testes RED com SQLite real isolado para salvar/reabrir atributos; preservar páginas 1/2; tratar versão 3 e dados inválidos; restaurar backups antigos e novos. Implementar somente após aprovação correspondente.
3. **Editor e histórico:** testes RED de seleção/foco na barra, aplicar no bloco correto, estados mistos, undo/redo de todos os atributos, autosave, navegação e isolamento. Implementar e conferir renderização após aprovação.
4. **Identidade visual:** aplicar a direção proposta em Avalonia, mantendo recursos semânticos e pontos de personalização. Conferir leitura longa, rolagem, tamanhos mistos, seleção, código, imagens e operação por teclado em janela ampla/estreita.
5. **Entrega para revisão:** build, format, suíte pertinente, conferência visual e pacote Windows de teste separado. Apresentar diff e riscos; atualizar estudos/roadmap, mantendo manual/PDF congelados.

A organização por espaços/cadernos/páginas permanece. Depois deste refinamento, retomar organização básica pendente, especialmente lixeira/recuperação, modelos e relações, antes de avançar para agenda e assistente. Histórico estrutural de adicionar/remover/mover blocos continua uma evolução separada.

## Riscos e estado da entrega

Os principais riscos são perda de atributos ao salvar/restaurar, desfazer incompleto, cor ilegível em outro tema e seleção perdida ao usar a barra. Conteúdo não pode desaparecer por uma mudança visual. A proposta trata esses riscos na ordem dos testes, sem trocar o motor do editor ou acrescentar dependências agora.

Foram revisados fontes, modelos, persistência, backup e regras locais. A entrega inicial foi documental. Na continuação foram preparados e executados [28 testes do modelo](../../tests/Trama.UnitTests/Knowledge/RichTextAttributesTests.cs): o [RED histórico](../reviews/036-rich-text-attributes-red.md) teve 27 falhas esperadas e uma proteção passando. Após aprovação explícita, o modelo foi implementado sem alterar os casos; resultado atual: 353 aprovados, build sem avisos/erros e formatação aprovada. Ver [relatório GREEN](../reviews/037-rich-text-attributes-green.md).

Os casos do modelo entram pela serialização de intervalos que já existe e exercitam construção, canonicalização e edição, sem stubs para uma API futura. As entregas posteriores acrescentaram 21 casos de persistência, 25 de controles e 15 de contexto/composição, aprovados antes de implementar. A suíte atual tem 414 aprovados. A composição foi conferida em janela ampla/compacta e pacote Windows isolado; a revisão humana do resultado continua pendente.

A direção busca uma identidade própria de composição, navegação e interação. Esta análise técnica não é uma avaliação jurídica de marcas, direitos autorais ou do conjunto visual de outro produto. Não prometer ausência de risco jurídico com uma mudança de cores.

Para estudar: por que tamanho e cor não cabem em um enum de flags? Por que dois intervalos em negrito com cores diferentes não podem ser unidos? Por que uma página precisa declarar uma versão quando ganha atributos? Como tema e formatação do conteúdo podem mudar independentemente?
