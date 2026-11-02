# Fechamento dos cadernos e próxima etapa: lixeira

Atualização de estado: a [entrega integrada](42-lixeira-entrega-integrada.md) passou nos 18 fluxos e dez casos complementares aprovados, com 454 testes no total. Está disponível para revisão humana. O restante deste capítulo preserva o planejamento e o diagnóstico anteriores.

31/10/2026. O responsável validou o acabamento atual, incluindo TAG EXATA +, e pediu exclusão recuperável de espaços, cadernos e páginas. A inclusão de espaços foi explicitamente confirmada antes da especificação dos testes. Este documento registra planejamento; lixeira e restauração de exclusões ainda não existem no código. Backup/restauração de pacote é uma funcionalidade diferente, já existente.

## Estado observado

Biblioteca Espaço → Caderno → Página, criação, renomeação/movimentação, texto/código/imagens, formatação, histórico de prosa, autosave, tags/busca e backup/restauração estão implementados. Última suíte: 426 testes aprovados. O guia acumulou registros históricos; seus totais antigos não representam a quantidade atual.

O Windows contém os fontes em `C:/Users/joset/www/Trama`. O helper `tools/run-wsl.sh` sincroniza e compila o espelho `/home/barberdev/trama-codex`; fontes atualizados não atualizam uma janela já aberta. O diagnóstico encontrou o cabeçalho TAG EXATA no espelho. O helper foi iniciado com `run`, após confirmar a existência do banco do perfil Linux, sem copiar bancos Windows. O perfil Linux continua separado do Windows.

### Janela somente na barra de tarefas: resolvido em 31/10/2026

O responsável relatou que o Trama Linux aparecia na barra de tarefas sem apresentar a interface. Processo ativo, build sem erros e janela X11 de 1200×800 confirmados. A captura direta dos pixels do cliente X11 mostrou a interface completa, distinguindo a apresentação pelo WSLg de uma falha de carregamento do aplicativo. Elevar e redimensionar a janela não resolveu a apresentação no Windows.

Após autorização explícita e confirmação de que o trabalho estava salvo, foi executado `wsl --shutdown`; depois o helper foi iniciado novamente com `run`. O responsável confirmou que a interface voltou a aparecer. Não houve mudança no código, configuração de segurança, cópia/exclusão de banco ou alteração de esquema. O perfil Linux existente foi mantido. A causa interna exata do WSLg não foi determinada; não atribuir o problema ao banco ou ao renderer do Avalonia com base apenas nesse sintoma.

Se houver recorrência, distinguir primeiro se a janela desenha no X11. Reiniciar o WSL encerra todos os aplicativos Linux e desconecta sessões de VS Code; exige salvar o trabalho e obter autorização antes. Não encerrar outros aplicativos ou reiniciar o WSL automaticamente a cada abertura. Os scripts de diagnóstico desta sessão ficam fora do produto em `trama-wsl-render`; não são um novo mecanismo de inicialização do Trama.

## Fechamento proposto da primeira versão dos cadernos

1. Implementar lixeira de espaços, cadernos e páginas, com restauração e proteção de rascunhos.
2. Conferir o ciclo completo: criar, editar, autosalvar, reabrir, mover, excluir, restaurar e recuperar por backup, incluindo formatação e anexos.
3. Validar pacote Windows e execução Linux, perfis corretos e janelas compactas. Registrar limites remanescentes, como clipboard WSL.

Exportação em formato acessível e modelos de estudo são as próximas melhorias úteis. Links entre páginas, pesquisa semântica, OCR, assistente, finanças e agenda não precisam ser antecipados para fechar esta primeira versão do módulo. A organização básica completa do roadmap inclui escopo maior, como relações e exportação; finalizar o módulo de cadernos não conclui automaticamente essa etapa inteira.

## Regras propostas para a lixeira, a especificar em testes

- Excluir move para a lixeira, preservando IDs, texto, código, estilos, tags e anexos. Não apagar arquivos físicos ao mover para a lixeira.
- Espaços/cadernos/páginas na lixeira deixam de aparecer nas listas e na busca normais. O salvamento automático não pode trazer de volta um item excluído por gravar um rascunho atrasado.
- Excluir caderno inclui suas páginas ativas numa mesma operação. Páginas já excluídas separadamente não devem ser restauradas por acidente quando o caderno voltar.
- Excluir espaço inclui seus cadernos e páginas ativos. Cadernos/páginas anteriormente excluídos permanecem na lixeira quando o espaço é restaurado; outros espaços e cadernos homônimos não são afetados.
- Restaurar o caderno recupera os itens daquela exclusão e sua organização. Restaurar página cujo caderno continua na lixeira precisa de uma regra explícita: proposta inicial é restaurar o caderno antes, com mensagem clara, sem mover a página silenciosamente.
- Excluir a página aberta respeita Salvar/Descartar/Cancelar; falha ou cancelamento preserva a sessão. Confirmar a ação sobre o caderno e informar quantas páginas serão afetadas.
- Lixeira sobrevive ao reinício e deve entrar no backup com anexos necessários. Validar atualização de bancos antigos e restauração de backups anteriores.
- Exclusão permanente e esvaziamento automático ficam fora da primeira fatia. Espaços estão incluídos por solicitação explícita do responsável. Não apagar dados definitivamente nesta etapa.

## Sequência de trabalho

Consolidar os contratos de exclusão/restauração e a migração necessária; preparar a especificação executável por fatias; mostrar RED comportamental e solicitar a revisão de testes exigida pelo CODEX; implementar somente após aprovação; conferir integridade e compatibilidade; conectar a interface e entregar diff. Não usar falha de compilação ou ausência de um serviço como prova de RED comportamental.

Riscos centrais: autosave concorrente com exclusão, restauração parcial de cadernos, limpeza prematura de imagens e backups que não preservam itens excluídos. O objetivo é poder desfazer a exclusão sem depender de recuperar manualmente o banco inteiro.
