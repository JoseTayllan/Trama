# Escrita rica: GREEN para revisão

30/10/2026. Implementação após aprovação explícita dos 25 casos. Nenhum teste aprovado foi alterado. Barra compartilhada, operações de atributos/limpeza, foco/seleção, estados mistos, histórico e renderer conectados. Ver [estudo](../estudos/31-controles-de-formatacao-implementados.md).

## Validação final

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
dotnet restore src/Trama.Desktop/Trama.Desktop.csproj -r win-x64 --locked-mode
```

Restore travado, build Release sem avisos/erros e formatação aprovados. **399 aprovados, zero falhas, zero ignorados**: 77 unitários e 322 de integração, incluindo os 25 casos aprovados intactos.

Harness SQLite/Skia com dados sintéticos: popup com foco na opção, seleção preservada, teclado undo/redo, autosave real e nova sessão, substituição/mutação/remoção de recursos, limites da toolbar em 1340/860 e altura de prosa/trechos. Capturas finais inspecionadas. Contraste mínimo 5,882:1 nas 36 combinações avaliadas da paleta padrão.

Publicação Windows win-x64 self-contained em cópia de fontes/saída isoladas passou. Checagem nativa final: janela com título Trama e Responding=true; fechamento solicitado ao processo de teste e concluído, usando perfil sintético separado. Nenhuma política de segurança foi modificada; publicação habitual e banco pessoal não foram sobrescritos. A primeira observação ocorreu antes da janela estar disponível; a checagem final aguardou a inicialização e confirmou prontidão.

## Achados tratados

- Capturar também foco já tratado pelo AvaloniaEdit para manter o alvo da barra.
- Preservar o alvo ao focar opções em popup com outra raiz visual.
- Evitar edição/histórico durante sincronização dos seletores, seleção vazia ou valor idêntico.
- Remover assinaturas ao trocar editor/árvore visual e observar mudanças de recursos/brushes.
- Corrigir sobreposição de texto grande: atualizar AvaloniaEdit 11.3.0 → 11.4.1, que troca a altura fixa pela medição automática. Avalonia e demais versões permanecem; ver fontes oficiais no estudo.
- Ampliar seletores após inspeção para evitar corte dos rótulos; conferir novamente o layout final.

A restauração de dependências inicialmente removeu grafos RID da publicação anterior. Foram preservados os grafos originais e restaurados byte a byte os locks sem mudança genérica. Somente os locks do desktop/integração mudam para AvaloniaEdit e sua referência transitiva; resto do grafo permanece fixado.

## Artefatos e limites

Na pasta externa `trama-rich-editor-green`: `implementacao.diff`, `audit.json`, `unit.trx`, `integration.trx`, `contrast.json`, `qa-final/qa-success.txt`, capturas ampla/compacta, `native-smoke-final.json`, `windows/Trama.Desktop.exe` e `Testar-Formatacao.cmd`. O launcher usa `windows-test-profile`, com conteúdo sintético. A cópia do banco sintético foi feita por backup SQLite, sem copiar banco pessoal ou WAL ativo.

Revisão humana do diff e uso interativo permanecem pendentes. A composição visual completa com Espaço → Caderno → Páginas continua a próxima etapa. Não foram alteradas migrações, formato do backup ou manual/PDF. Não confundir o smoke nativo com validação interativa de todos os atalhos/DPI no Windows.

Esforço moderado a alto: um agente, sem subagentes; duas rodadas de testes focados, três verificações completas justificadas por correção de foco, atualização do editor e ajuste final de largura, além de conferência visual/nativa. Sem medição de tokens/custo monetário. Economia na próxima etapa: reutilizar o harness e concentrar mudanças na composição visual, preservando modelo e persistência estabilizados.
