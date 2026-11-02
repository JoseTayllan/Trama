# 053 · Listas implementadas — revisão final pendente

31/10/2026. Aprovação explícita recebida para os 32 casos e página versão 4, preservando formatos 1/2/3 e pacote de backup 1. Os testes foram mantidos intactos.

## Implementação

- Application: metadados por parágrafo, validação, seleção e transformação dos offsets ao editar.
- Desktop: seletor Sem lista/Marcadores/Numerada, Enter, remoção/conversão, margem visual e histórico integrado ao texto/estilos. Preservado o mecanismo de autosave.
- Infrastructure: leitura/escrita de página 4 e envelope de estilos/listas na tabela de metadados existente; escrita antiga permanece em array para páginas sem listas. Nenhuma migração, versão de pacote ou dependência nova.
- Documentação: capítulo 45, referência no índice/README, estado atual no CODEX e roadmap com listas em revisão e próximas entregas separadas.

## Verificações

| Verificação | Resultado |
| --- | --- |
| Primeira execução focada dos casos aprovados | 32 aprovados |
| Primeira suíte completa | 485 aprovados; timeout no novo caso de autosave |
| Autosave isolado após investigação | 1 aprovado, teste intacto |
| Suíte completa seguinte | 77 unitários + 409 integrações = 486 aprovados; nenhum ignorado |
| Suíte final após alinhar pela linha de base | 77 unitários + 409 integrações = 486 aprovados; build sem avisos/erros, formatação aprovada |
| Restore/build/formatação | Helper WSL com restore locked, Release e format verify |
| Demonstração SQLite/Skia | Janela ampla/compacta; Enter e desfazer/refazer preservando metadados |
| Publicação Windows | Pacote win-x64 self-contained gerado em cópia isolada; execução interativa pendente |

O timeout inicial teve prazo de cinco segundos; não houve redução da pausa de autosave nem mudança dos testes para fazê-lo passar. Sua causa não foi determinada. As execuções aprovadas seguintes não provam ausência de variação temporal em outras máquinas.

Comando de referência: `wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test`. Na investigação, filtros selecionaram os 32 casos e depois somente o caso de autosave. A tentativa intermediária de ajustar o alinhamento usou um nome de enum inexistente e falhou na compilação; foi corrigida para a API real `VisualYPosition.Baseline`, sem alterar a especificação.

## Revisão crítica e riscos

Conferidos os caminhos de snapshot, mudança de texto, seleção/foco e replay do histórico. A mesma validação do leitor protege reabertura e backup. A versão impede que um build antigo sobrescreva a página 4. Ajustada a margem para tamanho maior e alinhamento pela linha de base real; cor deriva do tema.

Riscos residuais: conferir aparência no Windows e em outros DPI; listas extensas ainda não têm benchmark; o teste temporal de autosave apresentou um timeout inicial. Checklist, sublistas e numeração configurável não estão incluídos. O fechamento do módulo ainda depende da exportação e da conferência integrada acordada no roadmap.

Não foram alterados perfis pessoais, publicação habitual ou políticas de segurança do Windows. Manual/PDF e guias do colega permanecem intactos. O pacote de revisão usa `Testar-Listas.cmd` e perfil fictício separado.

## Evidências, revisão e esforço

Evidências externas em `trama-paragraph-lists-red/green`: snapshots, hashes dos testes, TRX, capturas, diff e pacote Windows. O snapshot fica fora do produto, pois não existe `.git` nesta pasta. A revisão humana do diff e a validação do uso continuam pendentes; aprovação dos testes não substitui esses aceites.

Sem novos subagentes. O coordenador implementou, revisou e documentou sequencialmente. Esforço moderado: execução focada, investigação temporal, suíte completa e ajustes visuais. Para economizar na próxima fatia, reutilizar o mapa atual e rodar filtros enquanto houver mudanças locais, deixando a suíte completa para a integração. Sem estimativa monetária ou de tokens por falta de medição.
