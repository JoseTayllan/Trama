# Trama · Roadmap

Atualizado em **01/11/2026**. Estado atual e próximas entregas ficam aqui. Decisões e verificações antigas estão no histórico, sem repetir checklists.

[Abrir painel visual em HTML](roadmap.html) · funciona localmente, sem servidor. A fonte oficial continua neste arquivo; para regenerar o painel, execute `python tools/build_roadmap.py`.

## Agora

| Item | Estado |
| --- | --- |
| Foco | Fechamento da primeira versão dos cadernos |
| Última entrega validada no uso | Exportação Markdown e painel HTML; listas e lixeira também validadas |
| Entrega em revisão | Revisão formal do diff e aceite final do módulo |
| Etapa atual | Uso validado em Windows e Linux; falta somente a revisão formal do diff |
| Resultado novo | Os 26 casos aprovados passam intactos |
| Última suíte completa | 512 testes aprovados; build e formatação conferidos no WSL |
| Próxima etapa | Revisão do diff e aceite final da primeira versão dos cadernos |
| Revisão separada | Novas especificações e diffs de código exigem revisão humana |

Exportação e painel HTML foram validados no uso pelo responsável em 01/11/2026, após a validação das listas e da lixeira. O responsável também confirmou a validação nos dois ambientes: Windows e Linux/WSL. Resta a revisão explícita do diff para o encerramento formal. A ocorrência anterior de timeout do autosave permanece registrada no relatório das listas.

## Caminho para fechar os cadernos

| Ordem | Entrega | Estado | Critério para encerrar |
| --- | --- | --- | --- |
| 1 | Listas com marcadores e numeradas | Implementadas · uso validado | Revisão explícita do diff ainda pendente |
| 2 | Exportação básica | Implementada · uso validado | Aceite de uso registrado; revisão do diff permanece separada |
| 3 | Conferência integrada | Windows e Linux · uso validado | Roteiro técnico conferido e validação nos dois ambientes confirmada pelo responsável |
| 4 | Fechar a primeira versão | Revisão final pendente | Revisar diff e registrar aceite final do módulo |

Checklist, sublistas e estilos avançados de numeração ficam para outra fatia. PDF e modelos de estudo não bloqueiam a entrega inicial de listas.

### Entrega em foco: exportação

- [x] Definir a fatia: página aberta, Markdown e imagens; não exportar a biblioteca inteira nesta etapa.
- [x] Preparar 26 casos e executar o RED, sem alterar código de produção.
- [x] Receber aprovação dos 26 casos e dos limites do formato.
- [x] Implementar conteúdo, ênfase, listas, código e imagens com links relativos.
- [x] Proteger destinos existentes e limpar saída parcial em falha/cancelamento.
- [x] Conectar a interface preservando rascunho, histórico e banco de origem.
- [x] Conferir exportação e aparência ampla/compacta, executar suíte e preparar diff para revisão.
- [x] Receber validação do uso da exportação e do painel HTML.
- [ ] Receber revisão explícita do diff da entrega.

Referências: [especificação da exportação](docs/estudos/46-exportacao-markdown-especificacao.md) e [relatório RED](docs/reviews/054-markdown-export-red.md).

Implementação atual: [exportação e painel para estudar](docs/estudos/47-exportacao-markdown-e-painel.md) e [relatório GREEN](docs/reviews/055-markdown-export-green.md).

Listas concluídas tecnicamente e validadas no uso: [implementação para estudar](docs/estudos/45-listas-implementadas.md) e [relatório GREEN e riscos](docs/reviews/053-paragraph-lists-green.md). O diff ainda exige revisão explícita.

## O que já está disponível

| Área | Entregue |
| --- | --- |
| Organização | Espaço → Caderno → Página; criar, renomear e mover páginas |
| Escrita | Texto, código com sintaxe, imagens, legendas, prints e arquivos arrastados |
| Formatação | Negrito/itálico, tamanhos, cores e marca-texto por seleção; listas por parágrafo validadas no uso |
| Proteção da edição | Salvar manual, autosave após pausa, desfazer/refazer e proteção de rascunhos |
| Localização | Busca textual, tags e filtro de tag exata |
| Recuperação | Lixeira dos três níveis, restauração e backup com anexos |
| Portabilidade | Exportar página aberta em Markdown com imagens relativas; uso validado |
| Apresentação | Seleção verde da hierarquia, margem de blocos, painéis recolhíveis e menu Bloco |
| Execução | Aplicativo local Avalonia; pacote Windows isolado e ambiente WSL |

“Disponível” significa implementado no escopo documentado. Não presume revisão de todos os diffs ou validação humana de todos os cenários. A [entrega da lixeira](docs/estudos/42-lixeira-entrega-integrada.md) registra os 454 testes e seus limites.

## Critério de conclusão desta versão

- [x] Listas aprovadas, implementadas e validadas no uso.
- [x] Exportação inicial definida e validada no uso, incluindo o fluxo de imagens aprovado.
- [x] Ciclo técnico em perfil fictício SQLite/Skia: criar → editar → formatar → salvar → reabrir → buscar → mover → excluir/restaurar → recuperar backup → exportar. Aceite interativo permanece separado.
- [x] Conferência de uso nos ambientes Windows e Linux, confirmada pelo responsável.
- [ ] Diff final revisado e aceite do responsável registrado.

A organização básica pode evoluir com relações, projetos e modelos. Fechar os cadernos não significa concluir todos os módulos futuros.

## Evolução futura

| Módulo | Objetivos |
| --- | --- |
| Anotações avançadas | Modelos de conceito, resumo, exercício, dúvida, fichamento e experimento; checklist, sublistas e relações entre páginas |
| Personalização | Editor de paletas com prévia e restauração do padrão; a base de recursos dinâmicos já existe |
| Rotina e semana | Tarefas, recorrência, fuso, lembretes, planejado versus realizado e retrospectiva |
| Marco de voz | Avaliar hardware; chat substituível, transcrição em português, síntese e interrupção |
| Resumo do dia | Agenda, clima e notícias com fontes, horários, preferências e estado offline |
| Finanças | Precisão decimal, categorias, vencimentos e histórico; sem processamento de pagamentos |
| Cofre | Criptografia, chaves, bloqueio, recuperação e proteção contra exposição em busca/IA/logs |
| Pesquisa e ML | Fontes verificáveis, BibTeX/RIS, OCR, busca semântica, avaliações e eventual ajuste de modelos |

Ações do assistente exigem permissões proporcionais. Abrir aplicativos deve usar catálogo aprovado, sem execução arbitrária. Treinar um modelo próprio continua possibilidade de pesquisa, sem compromisso de treinamento do zero.

## Pendências técnicas

| Pendência | Estado |
| --- | --- |
| CI em repositório remoto | Ainda não validada remotamente |
| Instalador, assinatura e distribuição | Evolução posterior ao pacote de revisão |
| Acessibilidade e escalas/DPI | Conferência contínua; sem auditoria completa declarada |
| Clipboard de imagem no WSL | Limitação conhecida; Windows validado e WSL sem prioridade nesta correção |
| Arquivamento e exclusão permanente | Fora da lixeira recuperável atual |
| Gerador Word antigo | Corrigir a raiz IDEA antes de reutilizar; não regenerar manual sem pedido |

## Evidências e histórico

- [Central de estudos](docs/estudos/README.md): explicações por implementação.
- [CODEX](CODEX.md): arquitetura, comandos, TDD e regras de qualidade.
- [README](README.md): execução e visão do produto.
- [Histórico anterior](docs/historico/rodmap-antes-das-listas-2026-10-31.md): decisões e checklists detalhados preservados.
- [Ambiente WSL](docs/operacao/wsl/README.md): comandos e perfis separados.

Atualizar o quadro “Agora” e a entrega em foco a cada etapa. Manter resultados antigos nas referências; não apresentá-los como a última execução.

## Executar e validar

No PowerShell, para o WSL já preparado:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh run
```

Troque `run` por `test` para sincronizar, restaurar com lock, compilar, verificar formatação e testar. Feche a instância antiga para carregar uma atualização; ela não recebe hot reload. Windows e Linux mantêm perfis separados.

Para execução nativa e migrações, siga o [CODEX](CODEX.md). A versão atual já inclui biblioteca e editor; a ausência deles não é o estado esperado.


ASS: josé Tayllan Pinto Almeida
