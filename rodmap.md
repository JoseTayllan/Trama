# Trama · Roadmap

Atualizado em **05/11/2026**. Acompanhe o módulo atual, as entregas disponíveis e os próximos passos.

[Abrir painel HTML](roadmap.html) · gerado deste arquivo por `python tools/build_roadmap.py`.

## Agora

| Item | Estado |
| --- | --- |
| Foco | Agenda e rotina semanal |
| Última entrega validada no uso | Campos de datas e horários da Agenda (05/11); ambiente não informado |
| Entrega em revisão | Fluxo da lixeira integrado; controles nativos a iniciar |
| Etapa atual | Fluxo no GREEN; revisão do diff e commit pelo responsável |
| Resultado novo | 21 casos aprovados intactos; suíte completa de 865 passou |
| Última suíte completa | 865 aprovados: 131 unitários e 734 integrações |
| Última suíte estável | 865 aprovados no fluxo da lixeira |
| Próxima etapa | Especificar confirmação e tela da lixeira; depois visão semanal |
| Referência principal | [Agenda: fluxo da lixeira implementado](docs/estudos/81-lixeira-agenda-fluxo-implementado.md) |
| Relatório atual | [Fluxo da lixeira implementado](docs/reviews/088-agenda-trash-workflow-green.md) |

A imagem conceitual, a direção da tela Início e os 17 testes foram aprovados antes de implementar. Início e Cadernos estão integrados para revisão, preservando o editor ao alternar. A Agenda diária está habilitada, com tarefas e horários opcionais; indicadores e semana visual permanecem pendentes.

Em 02/11, o responsável validou previamente a ideia, mas pediu maior fidelidade ao protótipo nos ícones, informações dos cadernos e backup. A correção de rolagem e seleção foi priorizada; aceite estético final permanece pendente.

A correção de rolagem foi aprovada em seguida. O refino visual está implementado para revisão, com informações reais da página aberta e estados de funcionalidades futuras. A implementação posterior da Agenda diária está em revisão; métricas ainda não estão implementadas.

## Etapas do módulo atual

| Ordem | Entrega | Estado | Critério para encerrar |
| --- | --- | --- | --- |
| 1 | Início e navegação | Implementado · revisão pendente | Validar Início, retomada, autosave e fechamento no uso; revisar o diff |
| 2 | Tarefas com persistência | Implementado · revisão pendente | Criar, editar, reagendar e mudar estado; reabrir sem perder os dados |
| 3 | Agenda semanal | A iniciar | Semana/dia, horários opcionais, categorias, cores e filtros em janela ampla/compacta |
| 4 | Planejado e realizado | A iniciar | Registros reais separados do planejamento e indicadores consistentes |
| 5 | Integração dos módulos | A iniciar | Vínculo com páginas, resumos reais do Início e backup/restauração da agenda |
| 6 | Validação e aceite | A iniciar | Suíte completa, uso Windows/Linux e revisão do diff antes da mesclagem pelo responsável |

### Entrega em foco: datas de término e lixeira da Agenda

- [x] Implementar e verificar regras, SQLite e serviço nas fatias anteriores.
- [x] Especificar e executar os 34 casos de formulário/navegação.
- [x] Receber aprovação explícita dos casos.
- [x] Implementar edição e lista por dia com os campos persistidos.
- [x] Integrar navegação e fechamento preservando os dois rascunhos.
- [x] Conferir regressões, capturas e gerar executável isolado; apresentar diff.
- [x] Permitir início sem fim e preparar novo formulário após salvar pelo botão.
- [ ] Validar o novo fluxo de cadastro e revisar o diff.
- [x] Refinar visualmente o formulário; conferência ampla/compacta e regressões.
- [x] Confirmar planejamento separado do encerramento real e preparar 17 casos RED.
- [x] Aprovar os 17 casos e implementar as regras do encerramento.
- [x] Refinar os seletores nativos de data e horário; conferência visual/teclado/popups.
- [x] Compactar campos, colocar data/horários em linha e conferir 43 casos afetados.
- [ ] Validar o visual compacto no uso e revisar o diff.
- [x] Especificar SQLite/migração/backup com 22 casos RED.
- [x] Aprovar os 22 casos e o modelo de último encerramento real.
- [x] Implementar e validar persistência, migração e backup do encerramento.
- [ ] Revisar o diff da persistência do encerramento.
- [x] Especificar serviço e consulta de candidatas com 24 casos RED.
- [x] Aprovar os 24 casos e implementar o serviço no perfil correto.
- [ ] Revisar o diff do serviço de encerramento.
- [x] Especificar seleção e diálogo coordenado com 35 casos RED.
- [x] Aprovar os 35 casos e implementar o fluxo de saída.
- [x] Conferir pausa de autosave, retenção dos rascunhos e fechamento efetivo.
- [x] Validar o fluxo do aviso e fechamento no uso (05/11).
- [ ] Confirmar ambiente de validação e revisar visual/diff.
- [x] Especificar armazenamento da lixeira com 31 casos RED.
- [x] Aprovar os 31 casos e os dois ajustes de contagem antiga.
- [x] Implementar e validar exclusão lógica, restauração e backup.
- [ ] Revisar o diff da persistência da lixeira.
- [x] Especificar término planejado em outro dia e data/hora reais informadas.
- [x] Aprovar os 18 casos de datas e implementar regras/encaminhamento.
- [ ] Revisar o diff das regras de término.
- [x] Especificar SQLite/backup da data final planejada com 24 casos RED.
- [x] Aprovar os 24 casos e a décima migração opcional.
- [x] Implementar e validar SQLite/backup da data final planejada.
- [ ] Revisar o diff da persistência da data final.
- [x] Especificar campos compactos de planejamento/conclusão real e horários HH:mm.
- [x] Aprovar 33 casos e ajuste de duas linhas antigas.
- [x] Aprovar e ajustar seletor adicional dos testes de diálogo.
- [x] Implementar campos e conferir visual/teclado em janela ampla/compacta.
- [x] Validar campos de datas e horários no uso (05/11).
- [ ] Revisar formalmente o diff dos campos de datas e horários.
- [x] Especificar fluxo de exclusão/restauração e proteção do editor com 21 casos.
- [x] Aprovar os 21 casos e implementar o fluxo.
- [ ] Revisar o diff do fluxo da lixeira e fazer checkpoint pelo responsável.
- [ ] Especificar e implementar confirmação/tela da lixeira.
- [ ] Iniciar a semana visual após os ajustes de tarefas.

Formulário compacto e fechamento coordenado conferidos em perfis fictícios. Regras, SQLite, serviço e diálogo estão integrados; saída normal resolve rascunhos antes de concluir a seleção, preserva o planejamento e impede autosave de descarte. Suíte completa com 738 aprovados, build/formatação e QA SQLite/Skia. Após o ajuste apenas textual do contador, os 35 casos e a formatação passaram novamente. O responsável validou o fluxo em 05/11, sem especificar o ambiente. Aceite visual e revisão formal do diff continuam separados. Pacote Windows isolado disponível; esta atualização não executou novos testes. O armazenamento da lixeira passou após aprovação dos 31 casos e dos dois ajustes antigos, com suíte completa de 769 aprovados. O responsável pediu término planejado em outro dia e data/hora real informada; essa ampliação terá especificação própria. A confirmação/tela de exclusão e a semana visual ainda não estão implementadas.

## O que já está disponível

| Área | Entregue |
| --- | --- |
| Organização | Espaço → Caderno → Página; criar, renomear e mover páginas |
| Escrita | Texto, código com sintaxe, imagens, legendas, prints e arquivos arrastados |
| Formatação | Negrito/itálico, tamanhos, cores, marca-texto e listas por parágrafo |
| Proteção da edição | Salvar, autosave após pausa, desfazer/refazer e proteção de rascunhos |
| Localização | Busca textual, tags e filtro de tag exata |
| Recuperação | Lixeira dos três níveis, restauração e backup com anexos |
| Portabilidade | Exportação da página em Markdown com imagens relativas |
| Apresentação | Seleção da hierarquia, margem de blocos, painéis recolhíveis e menu Bloco |
| Execução | Aplicativo Avalonia local, pacote Windows e desenvolvimento Ubuntu/WSL |
| Agenda diária | Criar, editar, reagendar, concluir/reabrir; lista por dia e rascunhos protegidos; em revisão |
| Navegação global | Início e Cadernos integrados, com retomada da página na sessão; implementação em revisão |

Cadernos e exportação foram validados no uso em Windows e Linux. A revisão formal do diff dessa entrega continua registrada separadamente no [relatório 055](docs/reviews/055-markdown-export-green.md). Iniciar o planejamento da Agenda não transforma essa revisão em aprovação implícita.

## Critério de conclusão desta versão

- [ ] Início e navegação validados, preservando a edição dos Cadernos.
- [ ] Tarefas e agenda semanal persistentes, validadas com datas, horários e categorias.
- [ ] Planejamento e atividade realizada separados, com indicadores verificáveis.
- [ ] Vínculo com páginas e recuperação da Agenda por backup/restauração.
- [ ] Suíte completa, uso em Windows/Linux, diff revisado e aceite do módulo.

Esses critérios se referem à primeira versão da Agenda. O histórico de aceite dos Cadernos permanece nos relatórios anteriores.

## Evolução futura

| Módulo | Objetivos |
| --- | --- |
| Agenda avançada | Recorrência, lembretes e integrações externas após a semana básica |
| Anotações avançadas | Modelos de estudo, checklist, sublistas e relações entre páginas |
| Painel do acervo | Protótipo aprovado; totais, espaços, últimas edições, tags e páginas não organizadas; implementar após a Agenda |
| Personalização | Editor de paletas com prévia e restauração do padrão |
| Marco de voz | Avaliar hardware, chat local, transcrição em português, síntese e interrupção |
| Resumo do dia | Agenda, clima e notícias com fontes, horários e estado offline |
| Finanças | Precisão decimal, categorias, vencimentos e histórico |
| Cofre | Criptografia, chaves, bloqueio, recuperação e proteção de dados sensíveis |
| Pesquisa e ML | Fontes verificáveis, BibTeX/RIS, OCR, busca semântica e avaliação de modelos |

Ações do assistente exigirão permissões e ferramentas delimitadas. Treinar um modelo próprio continua possibilidade de pesquisa.

## Pendências técnicas

| Pendência | Estado |
| --- | --- |
| Revisão formal dos Cadernos | Uso Windows/Linux validado; revisão explícita do diff ainda pendente |
| CI remota | Workflow configurado; resultado remoto não foi conferido pelo agente |
| Instalador e assinatura | Evolução posterior aos pacotes de revisão |
| Acessibilidade e DPI | Conferência contínua; sem auditoria completa declarada |
| Clipboard de imagem no WSL | Limitação conhecida; Windows validado |
| Arquivamento e exclusão permanente | Fora da lixeira recuperável atual |
| Gerador Word antigo | Corrigir a raiz IDEA antes de reutilizar; manual somente mediante pedido |

## Evidências e histórico

- [Integração dos módulos](docs/estudos/48-inicio-e-integracao-dos-modulos.md): proposta de navegação e escopo.
- [Especificação da primeira fatia](docs/estudos/49-inicio-navegacao-especificacao.md): casos aprovados e histórico RED.
- [Início implementado](docs/estudos/50-inicio-e-navegacao-implementados.md): estrutura, uso e limites da fatia.
- [Relatório GREEN](docs/reviews/057-home-navigation-green.md): 529 aprovados, conferência visual e revisão pendente.
- [Relatório RED](docs/reviews/056-home-navigation-red.md): resultados e limites da execução.
- [Central de estudos](docs/estudos/README.md): explicações das implementações.
- [Histórico dos Cadernos](docs/reviews/055-markdown-export-green.md): exportação, painel e aceite nos dois ambientes.
- [Roadmap anterior](docs/historico/rodmap-antes-das-listas-2026-10-31.md): decisões antigas preservadas.

## Executar e validar

Os [guias de operação](docs/operacao/README.md) explicam execução nativa, WSL e publicação Windows.

No terminal Linux, com o WSL preparado e a partir da raiz do projeto:

```bash
bash tools/run-wsl.sh run
bash tools/run-wsl.sh test
```

Git e GitHub são operados pelo responsável. O agente somente fornece os comandos quando solicitados; não cria branches, commits ou merges.

Regras de datas implementadas após aprovação dos 18 casos, preservados; suíte completa de 787 aprovados. A data final planejada continua sem persistência e controles. O diálogo atual usa o instante automático, embora o protocolo já aceite o informado. Próximas fatias: SQLite/backup e campos compactos; revisão do diff pendente.

Persistência da data final em revisão (05/11): 24 casos, 22 falhas esperadas e duas guardas passando; os 787 anteriores passaram separadamente. Código funcional/migração aguardam aprovação. Campos visuais permanecem indisponíveis.

Persistência da data final implementada após aprovação: 24 casos intactos e suíte completa de 811 aprovados. Décima migração preserva nove anteriores; backup continua 1. Campos visuais ainda indisponíveis; próxima etapa é sua especificação. Revisão do diff pendente.

Campos de datas/horários em revisão: 33 casos, 31 RED e duas guardas; base de 811 passou. Proposta troca seleção de horário por digitação HH:mm; nenhuma implementação alterada nesta fatia.

Campos implementados após aprovação: horários HH:mm por digitação, fim planejado e conclusão real informada. 844 testes e QA SQLite/Skia passaram; capturas ampla/compacta conferidas. Aceite visual no uso e revisão do diff pendentes; sem nova publicação Windows.

Aceite posterior, 05/11: o responsável confirmou “validado” para os campos de datas e horários. Validação no uso registrada; ambiente não informado. Revisão formal do diff permanece separada. Atualização somente documental, sem novos testes ou publicação.

Fluxo da lixeira em RED (05/11): 21 novos casos, 20 falhas esperadas e uma guarda; 844 anteriores passaram. Nenhuma implementação nova. Checkpoint de commit orientado ao responsável na branch feat/agenda-semanal; Git não executado pelo agente.

Fluxo da lixeira integrado após aprovação dos 21 casos, preservados; suíte completa com 865 aprovados. IAgendaTrashActions ligado à composição real. Controles/diálogos ainda pendentes; estado atual no GREEN para revisão e commit pelo responsável. Nenhum Git executado pelo agente.
