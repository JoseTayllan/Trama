# Continuidade do desenvolvimento no WSL — 29/10/2026

O Ubuntu-24.04 existente recebeu SDK 10.0.401 no perfil barberdev e as bibliotecas libice6/libsm6. O Trama foi compilado nativamente no Linux, sem carregar as DLLs Windows bloqueadas. A proteção do Windows permanece ativa; o problema de assinatura/reputação do executável Windows não foi resolvido.

## Mudanças para revisão

- `tools/run-wsl.sh`: espelho Linux com marca de origem, fontes sincronizadas sem bin/obj ou dados pessoais; ações sync/test/run. O perfil de desenvolvimento é separado.
- `LocalDatabase`: normaliza IOException de criação do diretório para preservar o contrato Windows/Linux exigido por dois testes existentes.
- `AvaloniaClipboardImageSource`: mantém bitmap pertencente ao clipboard do próprio processo; descarta bitmap externo após conversão. O harness reproduziu a falha na segunda colagem antes da correção e passou depois.
- README, CODEX, roadmap e central de estudos registram ambiente, comandos e limitações.

## Validação

- `bash tools/run-wsl.sh test`: build Release, zero avisos/erros; dotnet format verify; 49 testes unitários e 249 de integração aprovados, zero ignorados.
- Testes aprovados preservados; nenhuma migração anterior alterada. Auditoria e diff completo ficam junto às evidências externas indicadas na entrega.
- Harness isolado Avalonia.Headless/Skia: controles e atalhos de negrito/itálico, edição com deslocamento de intervalos, texto, duas colagens da mesma imagem, evento de drop, salvar/reabrir, backup/restauração com estilos/imagens e texto extenso passaram.
- Janela real executada pelo WSLg com perfil `/home/barberdev/trama-codex/.local-profile`. Isso comprova inicialização, não a revisão humana da interface nem todos os controles do sistema.

## Riscos e pendências

Revisar o diff antes de considerar a mudança aprovada. Validar clipboard, drag e escala interativamente. O build nativo Windows continua sujeito ao Smart App Control. A biblioteca Linux não contém as notas pessoais do perfil Windows; não mesclar bancos nem copiar o banco aberto. O espelho substitui fontes sincronizadas: editar somente o repositório Windows. O script foi preparado para este Ubuntu/usuário e requer configuração prévia em outra máquina. Migração de estilos deve ser aplicada com backup em uso real. Undo/redo completo de formatação permanece fora do escopo.

## Esforço e economia

Um agente principal, sem novos subagentes. Esforço moderado: preparação de ambiente, uma rodada com duas diferenças de exceção, correção, conferência de clipboard e validação final. Não há medição de tokens ou custo monetário nesta execução. Para economizar, reutilizar este ambiente e rodar testes afetados durante ajustes; repetir a suíte completa somente na entrega ou diante de novos riscos. O relatório 026 fica preservado como histórico do bloqueio Windows.
