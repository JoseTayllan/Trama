# Biblioteca visual e rascunhos — GREEN

Data: 28 de outubro de 2026. Implementação dos 31 casos aprovados do [RED](015-library-navigation-red.md), disponível para revisão humana do diff.

## Resultado

A janela apresenta espaços, cadernos e páginas com criação, listagens, abertura e salvamento associado ao caderno selecionado. Não organizadas reúne documentos sem localização. O editor permanece contínuo, com texto, código e imagens.

Alterações pendentes disparam confirmação antes da navegação. Cancelar mantém editor e seleção; Salvar e continuar aguarda sucesso; Descartar não grava o rascunho anterior. Falha ao abrir o destino preserva o editor atual. Fechar o diálogo sem escolha retorna Cancelar.

CreateCatalogEntry centraliza validação de nomes. SqliteLibraryActions cria um contexto por operação e coordena conteúdo e associação em uma transação. Os repositórios anteriores agora participam de transação externa sem assumir sua propriedade. Não houve nova migração nem dependência.

O [capítulo de estudo](../estudos/14-biblioteca-visual-implementada.md) explica uso, revisão de conteúdo, composição e limites.

## Verificação final executada

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/library-navigation-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/library-navigation-build --no-build --logger "trx;LogFilePrefix=library-navigation-green-final" --results-directory artifacts/library-navigation-green/final-results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

**157 aprovados: 36 unitários e 121 de integração, sem ignorados.** Build com zero avisos e zero erros; formatação aprovada. Nenhum teste aprovado foi alterado. Evidências TRX em `artifacts/library-navigation-green/final-results`. A CI remota não foi executada.

Os 31 novos casos comprovam rastreamento de alterações, preservação de edição posterior ao snapshot, escolhas de navegação, espera pelo salvamento, falhas sem perder rascunho, bindings, criação com nomes inválidos, reabertura em outra sessão e rollback conjunto de conteúdo/localização para destino inexistente.

## Revisão e QA adicional

A revisão conferiu propriedade das transações, descarte de contextos, cópia do snapshot, remoção dos eventos de blocos antigos, destino capturado por editor e seleção confirmada só após leitura bem-sucedida. O editor permanece intacto quando cancelar ou falhar a navegação.

Uma prévia externa usou o contêiner real e um banco SQLite isolado fora do perfil pessoal. Foram exercitados campos/botões da View, criação de espaço/caderno, escrita e gravação, abertura pela seleção das listas, confirmação por Cancelar/Salvar/Descartar e fechamento sem resultado. A seleção foi restaurada ao cancelar; salvar persistiu antes da troca; descartar não escreveu a página anterior.

As prévias ampla, compacta e do diálogo foram inspecionadas. A seleção das listas usa os pincéis semânticos do Trama, incluindo estados do ContentPresenter do tema. A abordagem de estilos de template segue as [orientações oficiais de Control Themes](https://github.com/AvaloniaUI/Avalonia/wiki/Porting-to-Control-Themes). Não foram carregados estilos arbitrários de usuário.

O roteiro de QA não integra a suíte de testes do produto. Teclado, foco por atalhos, leitores de tela, escala do Windows e seletor nativo de imagens não foram conferidos interativamente. A prévia renderizada não abriu nem migrou o banco pessoal.

## Riscos e limites materiais

- A proteção cobre navegação interna; fechar a janela principal ainda não solicita confirmação. Não há autosave ou recuperação de rascunho após interrupção do processo.
- O contador de revisão é conservador: desfazer manualmente até o mesmo texto não recalcula equivalência com o snapshot salvo.
- Banco e arquivos não compartilham transação. Anexos importados para rascunhos abandonados podem ficar sem referência; a coleta continua pendente.
- Não há renomeação/exclusão visual, mover páginas pela interface, paginação das listagens ou resolução de conflitos entre múltiplos escritores.
- Uma falha ao atualizar a lista depois de salvar é informada separadamente; a gravação já confirmada continua sendo sucesso.

A composição preserva ICreateNote, INotebookActions e os editores anteriores. O novo parâmetro de prompt é opcional; sem interface de confirmação, o comportamento padrão é Cancelar. Os testes de compatibilidade anteriores continuam aprovados.

## Documentação e esforço

README, CODEX, roadmap e trilha de estudo foram atualizados. Um agente, sem delegação nem novas dependências. Duas execuções completas da suíte, verificações de build/formatação e um roteiro visual isolado com correções locais. Não há medição de tokens por tarefa disponível. Manter a próxima fatia limitada a uma lacuna — como proteção no fechamento — evita reexplorar a biblioteca inteira.
