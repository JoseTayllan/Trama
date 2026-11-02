# 055 · Exportação e painel HTML — implementação para revisão

01/11/2026. Aprovação explícita dos 26 casos e limites recebida antes de implementar. Os testes aprovados foram preservados. Listas já validadas no uso; o módulo ainda aguarda aceite final.

## Atualização: validação de uso recebida

Em 01/11/2026, o responsável confirmou: “exporte e painel html validado”. Exportação e painel passam a ter aceite de uso registrado. Essa confirmação não informa a plataforma específica e não constitui revisão explícita do diff. O aceite final do módulo e a conferência registrada de ambos os ambientes continuam pendentes. Os resultados abaixo preservam a execução da implementação; nesta atualização somente documentação/status e HTML gerado foram alterados, sem repetir os testes de produto.

Confirmação posterior, na mesma data: “validado nos 2 ambientes”. A validação de uso em Windows e Linux/WSL está registrada. Resta apenas a revisão explícita do diff para encerrar formalmente esta versão. As pendências de ambiente mencionadas anteriormente neste relatório são históricas; não foram executados novos testes de produto nesta atualização documental.

## Entrega

- Markdown de uma página aberta, inclusive rascunho, com título, texto, ênfase, listas, código e imagens relativas.
- Validação de destino novo/absoluto; staging, publicação sem sobrescrita e limpeza em falha/cancelamento; streams descartados.
- Diálogo responsivo com escolha de pasta, feedback e cancelamento, preservando snapshot, histórico e política de autosave.
- Injeção conectada à composição e aos editores recriados pela biblioteca; construtores anteriores preservados.
- `roadmap.html` offline com filtros/busca, tema e notas locais. Gerador `tools/build_roadmap.py` deriva tabelas/checklists de `rodmap.md`; layout em `tools/roadmap.template.html`.
- Estudo 47 e atualização de roadmap, README e CODEX. Nenhuma migração ou dependência nova. Manual/PDF não regenerados.

## Verificações

| Verificação | Resultado |
| --- | --- |
| Casos novos focados | 26 aprovados, sem alterar os casos |
| Suíte completa | 77 unitários + 435 integrações = **512 aprovados**, nenhum ignorado |
| Build/formatação | Release sem avisos/erros; format verify aprovado no WSL |
| Ajuste da revisão | Metadados de cor/tamanho não dividem uma mesma ênfase em delimitadores adjacentes; suíte final e demonstração conferidas |
| Roteiro integrado SQLite/Skia | Criar/editar/formatar/salvar, busca por tag, mover, lixeira/restauração, backup/restauração e exportação do perfil restaurado |
| Interface da exportação | Capturas ampla/compacta; rascunho não foi salvo pela exportação e histórico permaneceu disponível |
| Painel em navegador | Filtros, busca sem acentos, estado vazio, tema, notas locais; 1440/900/390 px sem overflow horizontal ou requisições externas |
| Windows | Publicação self-contained win-x64 em cópia isolada para validação interativa pelo responsável |

Comandos: `tools/run-wsl.sh test` pelo WSL e teste focado por `FullyQualifiedName~MarkdownExportWorkflowTests`. Após o ajuste de ênfase, a suíte foi novamente executada com `dotnet test Trama.slnx -c Release`, acompanhada de `dotnet format --verify-no-changes`. Os artefatos externos contêm TRX e snapshots.

## Revisão e limites

Conferidos os caminhos de exportação na composição da biblioteca, nos construtores antigos e na fixture de integração. O exportador não acessa o perfil pessoal nem grava o banco: recebe snapshot e leitor de imagens. O diálogo captura os dados antes de executar e pausa autosave, retomando a política anterior ao fechar.

O nome do destino não deriva do título. A pasta final não é sobrescrita; imagens têm nomes por GUID e extensão por assinatura PNG/JPEG. A limpeza se limita ao staging criado pela operação. Se o processo cair ou o disco/permissões falharem durante a limpeza, resíduos temporários podem exigir tratamento posterior; não se declara transação completa de sistema de arquivos.

Escapes de texto/legenda/título e cerca de código reduzem interpretação indevida do conteúdo exportado. A saída não contém scripts nem CSS derivados do texto. Markdown não preserva tamanho/cor/marca-texto específicos, conforme os limites aprovados. Uma página por operação; sem exportação em lote ou PDF.

O painel é local e estático até regenerar. Notas pessoais no navegador são separadas dos status oficiais; um armazenamento bloqueado não impede consultar o painel. Não há métricas ao vivo nem percentual geral de conclusão do Trama.

Pendências: validação interativa Windows, revisão explícita do diff e aceite integrado do módulo. DPI/acessibilidade e desempenho com páginas enormes não foram auditados integralmente. O executável e o perfil de revisão ficam isolados; publicação habitual, dados pessoais e políticas de segurança não foram substituídos.

## Evidências e esforço

Diretório externo `trama-markdown-export-red/green`: diff, hashes dos testes, TRX, screenshots do painel e diálogo, roteiro integrado, pacote Windows e launcher `Testar-Exportacao.cmd`. Sem Git nesta pasta, o diff usa o snapshot anterior à implementação. Links locais e varredura textual dos trechos adicionados são conferidos; essa varredura é limitada, não auditoria completa de segredos.

Sem novos subagentes. Implementação, revisão e documentação feitas sequencialmente pelo coordenador. Esforço moderado, com teste focado, suíte completa e conferência visual. Economia para a próxima rodada: usar este pacote e o roteiro integrado para validar; não repetir a exploração da arquitetura. Sem medição de tokens ou custo monetário.
