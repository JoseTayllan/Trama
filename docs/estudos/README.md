# Central de estudos do Trama

Esta central reúne as explicações das decisões e do código construído no projeto. A proposta é estudar engenharia de software a partir de exemplos reais do Trama, ligando cada conceito aos arquivos que o demonstram.

O [README principal](../../README.md) explica como preparar e executar o aplicativo. O [CODEX](../../CODEX.md) define as convenções e o fluxo de trabalho. Esta pasta explica **por que as partes existem, como se conectam e o que ainda falta construir**.

Os procedimentos de ambiente e distribuição ficam separados na [central de operação](../operacao/README.md), com guias de WSL, publicação Windows e PDF para os testadores.

## Trilha de leitura

| Ordem | Material | O que estudar |
| --- | --- | --- |
| 1 | [Arquitetura e fundação](01-arquitetura-e-fundacao.md) | Camadas, MVVM, injeção de dependências, contratos, SQLite e comandos |
| 2 | [Primeira nota e TDD](02-primeira-nota-e-tdd.md) | Especificação executável, RED e GREEN, validação e tratamento de falhas |
| 3 | [SQLite migrações e durabilidade](03-sqlite-migracoes-e-durabilidade.md) | Testes em disco, esquema, migrações e limites do RED atual |
| 4 | [Repositório contratos e erros](04-repositorio-contratos-e-erros.md) | Escrita e consulta por EF Core, falhas, cancelamento e GREEN do adaptador |
| 5 | [Diretório e inicialização local](05-inicializacao-local.md) | Política de caminho e preparação do banco implementadas e testadas |
| 6 | [Editor e composição do desktop](06-editor-e-composicao.md) | Primeiro editor conectado ao SQLite, com sete casos aprovados |
| 7 | [Temas e interface](07-temas-e-interface.md) | Recursos semânticos, refinamento visual e preparação para paletas personalizadas |
| 8 | [Caderno em blocos](08-caderno-em-blocos.md) | Rascunho implementado em memória, identidade, ordem e preservação dos blocos |
| 9 | [Persistência do caderno e anexos](09-persistencia-do-caderno-e-anexos.md) | Migração, transação, reabertura e importação local de imagens implementadas nos adaptadores |
| 10 | [Especificação do editor contínuo](10-editor-continuo-do-caderno.md) | Histórico dos testes e das decisões aprovadas antes da implementação |
| 11 | [Interface do caderno implementada](11-interface-do-caderno-implementada.md) | Uso, bindings, casos de uso, sintaxe, recursos de imagem e limites atuais |
| 12 | [Espaços, cadernos e páginas](12-espacos-cadernos-e-paginas.md) | Catálogo persistente, movimentação, páginas não organizadas e compatibilidade implementados |
| 13 | [Especificação da navegação](13-navegacao-da-biblioteca.md) | Planejamento aprovado e histórico RED da biblioteca e dos rascunhos |
| 14 | [Biblioteca visual implementada](14-biblioteca-visual-implementada.md) | Uso, navegação, confirmação, revisões do editor e transação coordenada |
| 15 | [Cinco passos e proteção ao fechar](15-cinco-passos-e-protecao-ao-fechar.md) | Plano solicitado e fechamento seguro implementado, com confirmação e espera pela gravação |
| 16 | [Gestão da biblioteca implementada](16-gestao-da-biblioteca.md) | Renomeação, movimentação, preservação dos rascunhos e vínculo do editor com o destino |
| 17 | [Busca e tags implementadas](17-busca-e-tags.md) | Consulta textual, metadados, normalização, migração compatível e proteção de rascunhos |
| 18 | [Backup e restauração implementados](18-backup-e-restauracao.md) | Snapshot SQLite, WAL, imagens, manifesto, integridade e restauração em perfil novo |
| 19 | [Imagens e formatação](19-imagens-e-formatacao.md) | Clipboard/drop, estilos, edição e compatibilidade validados no WSL |
| 20 | [Desenvolvimento no WSL](../operacao/wsl/README.md) | Comandos, fontes, perfil separado, testes e continuidade com a proteção Windows ativa |
| 21 | [Realce de código e paletas](21-realce-de-codigo-e-paletas.md) | Diferenciação lexical, recursos dinâmicos, locks entre plataformas e diagnóstico do clipboard Wayland/X11 |
| 22 | [Salvamento automático: especificação aprovada](22-salvamento-automatico-especificacao.md) | Pausa reiniciável, relógio controlado, revisões e oito casos aprovados |
| 23 | [Salvamento automático implementado](23-salvamento-automatico-implementado.md) | Agendamento, revisões, foco, descarte, ciclo de vida e persistência real |
| 24 | [Desfazer/refazer: especificação aprovada](24-desfazer-refazer-especificacao.md) | Histórico de texto/estilos, atalhos, isolamento e treze casos aprovados |
| 25 | [Desfazer/refazer implementado](25-desfazer-refazer-implementado.md) | Pilha nativa, operações de estilos, replay agrupado e integração com autosave |
| 26 | [Identidade e formatação dos cadernos](26-identidade-e-formatacao-dos-cadernos.md) | Hierarquia explícita Espaço → Caderno → Páginas, conceito aceito, especificação e etapas de compatibilidade/TDD |
| 27 | [Atributos de texto implementados](27-atributos-de-texto-implementados.md) | Tamanho, cor e destaque no modelo; intervalos, validação, herança na edição e 353 testes aprovados; integração visual/persistência pendente |
| 28 | [Persistência de atributos: especificação](28-persistencia-de-atributos-especificacao.md) | 21 casos e regras de versões aprovados; histórico RED e contrato de compatibilidade |
| 29 | [Persistência de atributos implementada](29-persistencia-de-atributos-implementada.md) | Versões de conteúdo, leitura consistente, SQLite e backup com atributos; esquema antigo e 374 casos aprovados |
| 30 | [Controles de formatação: especificação](30-controles-de-formatacao-especificacao.md) | 25 casos aprovados e histórico RED da barra, seleção/foco, atributos, limpeza, histórico, autosave e renderização |
| 31 | [Controles de formatação implementados](31-controles-de-formatacao-implementados.md) | Barra compartilhada, snapshots de atributos, recursos de cor, correção de altura, SQLite/Skia e 399 testes aprovados |
| 32 | [Identidade e hierarquia: especificação](32-identidade-e-hierarquia-especificacao.md) | 15 casos aprovados e histórico RED do contexto, renomeações/movimentação, transições e janela compacta |
| 33 | [Identidade e hierarquia implementadas](33-identidade-e-hierarquia-implementadas.md) | Contexto derivado, notificações, composição contínua, nomes longos, editor preservado e 414 testes aprovados |
| 34 | [Alinhamento com a referência visual](34-alinhamento-com-a-referencia-visual.md) | Busca lateral, criação recolhida, cabeçalho único, margem e comandos existentes; correção visual para revisão |
| 35 | [Refino responsivo: especificação](35-refino-responsivo-especificacao.md) | Margem única, painéis recolhíveis, filtros e ações por menu; 12 casos para revisão antes da implementação |
| 36 | [Refino responsivo implementado](36-refino-responsivo-implementado.md) | Marcadores alinhados, painéis recolhíveis, menu Bloco, seleção verde nos três níveis e 426 testes aprovados |
| 37 | [Campos e criação na lateral](37-campos-e-criacao-na-lateral.md) | Campos uniformes e criação no “+” dos cabeçalhos, preservando comandos e Expander existentes |
| 38 | [Fechamento dos cadernos e lixeira](38-fechamento-dos-cadernos-e-lixeira.md) | Estado atual, diagnóstico WSL e regras propostas para exclusão recuperável; implementação pendente |
| 39 | [Lixeira: especificação](39-lixeira-especificacao.md) | 18 fluxos de espaços/cadernos/páginas para revisão; cascatas, restauração, critérios e limites da primeira fatia |
| 40 | [Lixeira: fluxos implementados](40-lixeira-fluxos-implementados.md) | Os 18 aprovados passam; persistência por lote, interface e migração, com correções complementares pendentes |
| 41 | [Lixeira: segurança e compatibilidade](41-lixeira-seguranca-especificacao.md) | Dez casos de migração/backup/legado/autosave: três RED e sete proteções para revisão |
| 42 | [Lixeira: entrega integrada](42-lixeira-entrega-integrada.md) | Fluxos dos três níveis, correções aprovadas, migração/backup/autosave e 454 testes aprovados |
| 43 | [Listas e fechamento do módulo](43-listas-e-fechamento-do-modulo.md) | Lixeira validada no uso, proposta de listas por parágrafo e próximos critérios de fechamento |
| 44 | [Listas: especificação para revisão](44-listas-especificacao.md) | 32 casos em RED, contrato por parágrafo, versão 4 proposta e limites antes da implementação |
| 45 | [Listas implementadas](45-listas-implementadas.md) | Metadados de parágrafo, histórico, Enter, margem visual, conteúdo versão 4 e 486 testes aprovados |
| 46 | [Exportação Markdown: especificação](46-exportacao-markdown-especificacao.md) | Página aberta e imagens relativas; 26 casos para revisão, portabilidade, rascunho e recuperação de falhas |
| 47 | [Exportação e painel implementados](47-exportacao-markdown-e-painel.md) | Snapshot do rascunho, Markdown/imagens, publicação de arquivos, painel offline e 512 testes aprovados |

Leia os capítulos na ordem na primeira passagem. Depois, use os links para revisar um assunto enquanto examina o código correspondente. Aprovar testes de uma funcionalidade não significa que persistência, editor e todos os módulos previstos já estejam entregues.

## Como estudar com o projeto

1. Leia a pergunta ou o objetivo do capítulo.
2. Abra os arquivos citados e acompanhe o caminho de uma entrada até seu resultado.
3. Relacione os testes ao comportamento que eles protegem.
4. Explique a decisão com suas próprias palavras e registre suas dúvidas.
5. Consulte as limitações: um exemplo técnico não comprova uma funcionalidade completa.

Os exercícios são opcionais. Responder perguntas, desenhar o fluxo e formular cenários não exige alterar código. Se quiser implementar uma experiência, mantenha-a separada da entrega em andamento e respeite a revisão de testes prevista no CODEX.

## Próximos assuntos da trilha

Estes temas entram quando houver implementação correspondente e ainda não representam capítulos disponíveis:

- Diretório pessoal, inicialização do banco e composição do desktop com EF Core.
- Editor, bindings, comandos e estados da interface.
- Anexos, integridade dos arquivos, backup e restauração.
- Agenda, recorrências e separação entre planejamento e execução.
- Assistente local, limites de ferramentas e avaliação de respostas.
- Machine Learning aplicado ao Trama, conjuntos de avaliação e eventual ajuste de modelos.

## Convenção para novos capítulos

Nomear os arquivos como `NN-assunto.md` e adicionar cada material à tabela acima. Explicar objetivo, conceitos, arquivos reais, fluxo, evidências de teste, limites e perguntas de revisão. Diferenciar código existente, proposta e evolução futura.

Não copiar resultados de testes de uma etapa anterior como se fossem verificação da versão atual. Preferir referências ao código e aos relatórios da entrega, preservando a distinção entre aprendizado e comprovação de funcionamento.
