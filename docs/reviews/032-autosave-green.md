# Salvamento automático GREEN - 29/10/2026

## Resultado para revisão

Após aprovação explícita dos oito casos, o aplicativo grava a página após dois segundos sem alterações, preservando o botão Salvar. Build Release sem avisos/erros, dotnet format verify e **312/312 testes** passaram: 49 unitários e 263 de integração. Os testes aprovados foram preservados. Revisão humana do diff e uso interativo do novo pacote Windows permanecem pendentes.

## Mudanças

- Parcial AutoSave do editor: atraso cancelável por TimeProvider, gravações serializadas pelo fluxo existente, revisão posterior preservada, falhas sem loop automático ilimitado e cancelamento no Dispose.
- CanEditContent preserva foco, texto e formatação durante a gravação automática; a proteção anterior continua nas demais operações. Feedback diferencia snapshot anterior salvo de revisão atual pendente.
- Biblioteca pausa autosave durante decisão de navegação/fechamento, descarta o editor substituído e mantém ações de gravação ligadas ao caderno correto após mover.
- App ativa o modo automático pela composição. As assinaturas anteriores foram mantidas por sobrecargas; consumidores legados continuam manuais.
- View, README, roadmap, CODEX, guia Windows, estudo e PDF de instruções atualizados. O gerador do PDF foi editado antes de regenerar o documento.
- Sem migrações, novos pacotes, alteração de formatos ou acesso a banco pessoal.

## Validação realizada

1. Oito testes focados aprovados passaram após implementar o agendamento, sem alterações nos testes.
2. A primeira suíte completa passou 312 casos. Na revisão de interface, campos que dependiam somente de !IsBusy poderiam perder foco durante autosave; o modo automático passou a manter a edição disponível.
3. Harness externo Headless/Skia, com composição e SQLite reais: salvamento sem botão, estilos/código/imagem, atualização da lista, nova sessão e destino após mover passaram. Um prompt ficou pendente além do prazo; não houve commit antes de Descartar nem depois da navegação/fechamento. Editor antigo ficou descartado. Com persistência lenta controlada, foco e nova edição foram preservados e os dois snapshots ficaram corretos/serializados.
4. Após esse ajuste, suíte completa final, build e formatação passaram novamente.
5. Windows Release win-x64 self-contained publicado isoladamente. Processo apresentou janela Trama respondendo e criou banco de teste; isso confirma inicialização, não a revisão humana de todos os recursos do novo pacote.
6. PDF atualizado com instrução de autosave para novos pacotes e salvamento manual para versões anteriores; páginas renderizadas para conferência visual.

Artefatos externos: trama-autosave-green contém before.json, TRX, qa-success.txt, screenshot, pacote Windows e diff. Testar-Autosave.cmd seleciona windows-test-profile; não aponta para o perfil pessoal do usuário. A pasta windows inteira é o pacote portátil; o perfil de teste fica fora dela.

## Riscos e limites

Revisar aparência e testar a nova publicação Windows. O exe continua sem assinatura digital; aceitação em outras máquinas não foi validada. Autosave não substitui backup nem recupera alterações posteriores ao último commit concluído. Não foi implementado controle de conflito entre duas instâncias no mesmo perfil. Operações estruturais/importação ficam bloqueadas enquanto o editor grava. A proteção de fechamento mantém a regra anterior de cancelar uma tentativa quando já existe operação em andamento. Print no WSL não foi retomado por decisão do responsável.

## Esforço

Um agente, sem subagentes. Esforço moderado: um ciclo de implementação, revisão/ajuste para foco, dois passes completos da suíte, um harness de integração visual e publicação Windows. Não há medição de tokens ou custo monetário. Para economizar, reutilizar o harness de SQLite e rodar os oito casos afetados durante ajustes; repetir a suíte completa apenas na próxima entrega ou diante de falhas novas.

O [estudo](../estudos/23-salvamento-automatico-implementado.md) explica a implementação; a [especificação anterior](../estudos/22-salvamento-automatico-especificacao.md) preserva os requisitos aprovados.
