# 057 · Início e Cadernos — implementação para revisão

01/11/2026. Aprovação dos 17 casos recebida antes da implementação. Os testes aprovados e os anteriores permaneceram intactos. Esta é a primeira fatia da Agenda: navegação global e integração dos Cadernos; a Agenda funcional segue para as próximas etapas.

## Alterações

- Janela principal entra em Início depois da preparação do perfil; antes disso, os comandos de navegação não são habilitados.
- Barra superior com estado ativo, Cadernos, Agenda desabilitada e Backup global.
- Cartão do acervo e atalho para a página/rascunho em andamento na sessão, com dados reais da biblioteca.
- Host da biblioteca preservado por visibilidade, mantendo instâncias do editor, seleção, rolagem e histórico.
- Apresentação hospedada da biblioteca sem marca/Backup duplicados; modo independente anterior preservado.
- Documentação e painel do projeto atualizados para implementação em revisão.

## Verificações

| Verificação | Resultado |
| --- | --- |
| Casos novos focados | 17 aprovados |
| Suíte completa | 77 unitários + 452 integrações = **529 aprovados**, nenhum ignorado |
| Build | Release sem avisos/erros no WSL |
| Formatação | `dotnet format --verify-no-changes` aprovado pelo helper |
| Editor preservado | Mesma instância visual, texto/estilos/listas, seleção, rolagem, busca e desfazer/refazer |
| Salvamento e fechamento | Autosave oculto; Salvar/Descartar/Cancelar ao fechar por Início |
| Roteiro SQLite/Skia | Navegação, retomada, autosave, backup e fechamento; capturas 1380×1000 e 860×680 |
| Windows | Pacote win-x64 self-contained isolado para validação interativa |

Comando focado: filtro `FullyQualifiedName~HomeModuleNavigationTests` no projeto de integração. A suíte completa foi executada por `tools/run-wsl.sh test`, com restore locked, build e format verify. A compatibilidade do caminho de editor independente foi mantida sem alterar seus contratos.

## Revisão crítica e limites

Conferida a diferença entre ocultar uma área e substituir o conteúdo: a seleção de módulo não altera `Workspace`. Isso evita perder a pilha nativa do AvaloniaEdit. A assinatura de notificações é removida da biblioteca anterior quando a referência muda.

A proteção de fechamento continua considerando a biblioteca, inclusive com Início visível. O backup reutiliza o fluxo e os bloqueios existentes. A tela não introduz escrita adicional no banco nem novas regras de salvamento. Recursos de cor são semânticos e dinâmicos, conforme a base de temas.

O atalho Continua página cobre a sessão atual; não implementa persistência da última página entre reinicializações. Não há consultas de páginas recentes, tarefas, horários, totais semanais ou calendário funcional nesta fatia. A Agenda está desabilitada e não há métricas fictícias.

Riscos residuais: aprovação estética, uso interativo no Windows e outros DPI; acessibilidade e desempenho com acervos muito grandes não foram auditados integralmente. Ao entregar Agenda, sua edição e seu fechamento precisarão de cobertura própria.

Não houve alteração de dependências, migrações, versões de conteúdo ou dados pessoais. Publicação habitual, políticas Windows, README de apresentação e manual/PDF foram preservados. Nenhum comando Git foi executado.

## Entrega e revisão

O diretório externo `trama-home-navigation-red/green` reúne snapshot, hashes, TRX, capturas, diff e o pacote Windows com `Testar-Inicio.cmd`. O perfil é fictício. A revisão do diff e a validação de uso da fatia seguem pendentes; os 529 testes aprovados não equivalem a aceite visual ou encerramento do módulo de Agenda.

Sem novos subagentes. Esforço moderado: implementação sequencial, execução focada, suíte completa e roteiro visual. Para a próxima rodada, reutilizar este pacote e conferir a navegação antes de ampliar para tarefas da Agenda. Não há medição de tokens ou custo monetário.
