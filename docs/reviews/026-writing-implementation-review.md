# Passo 5 — implementação em revisão; validação final bloqueada

29 de outubro de 2026. Código implementado após aprovação dos 28 testes. A entrega não tem GREEN final confirmado por bloqueio do Windows no carregamento da DLL desktop.

## Resultado implementado

Negrito/itálico por seleção com botões N/I e Ctrl+B/Ctrl+I; estilos combináveis sem alterar o conteúdo puro. Colar print por botão/Ctrl+Shift+V, mantendo Ctrl+V textual; arquivos de imagem arrastados são importados em sequência, aceitando sucessos parciais. Streams são descartados, operações concorrentes bloqueadas e rascunhos preservados. A política existente de PNG/JPEG até 10 MiB continua no armazenamento gerenciado.

InlineTextDocument valida/copia intervalos, combina/remover flags e desloca estilos ao editar. NotebookProseEditor usa AvaloniaEdit já instalado, renderer por Typeface e altura medida da página. A tabela nova NotebookTextStyles armazena JSON com FK ao bloco, sem alterar o esquema das tabelas anteriores. Páginas simples mantêm formato de conteúdo 1; páginas formatadas usam 2. A migração final é `20261029201153_InlineTextStyles`; as quatro anteriores permanecem intactas.

Restauração aceita prefixo completo conhecido de migrações e atualiza o staging antes de publicar. O banco de origem não é atualizado por essa operação. Leitura/persistência validam estilos, e backup recusa referências ou dados inválidos. O pacote de backup continua em formato 1.

## Verificações e cronologia relevante

1. Os 28 novos casos passaram após a primeira implementação de modelo/persistência.
2. A suíte completa encontrou duas regressões anteriores: altura de escrita longa e acesso à coluna nova em esquemas antigos. Foram corrigidas com altura medida e tabela separada. A migração nova ainda não distribuída foi regenerada; as anteriores não foram alteradas.
3. A rodada `writing-corrected` passou **298/298**, com 49 unitários e 249 integrações, sem ignorados.
4. O harness visual identificou falta de foco por ausência do StyleKey herdado. Corrigido para TextEditor. Uma expectativa do harness sobre a borda inicial foi alinhada à herança de estilo do caret; os testes aprovados não foram modificados.
5. O harness passou botões/atalho, sobreposição, deslocamento na edição, colagem normal de texto, bitmap do clipboard, evento Drop, reabertura, backup/restauração com estilos e imagens e escrita longa. Exit code 0; imagens `rich-writing.png` e `rich-page-reopened.png` inspecionadas.
6. A revisão final otimizou a composição de intervalos, preservou drag de texto não relacionado e reforçou a validação de metadados da página. Build final passou sem avisos e formatação final passou.
7. Ao executar a suíte completa final e a rodada extra do harness, o Windows bloqueou Trama.Desktop.dll com **0x800711C7**. Uma repetição do harness já compilado confirmou o bloqueio. Não houve tentativa de alterar políticas ou contornar o bloqueio.
8. A rodada final separada de regras/persistência passou **18/18** (13 unitários e cinco integrações). Ela não substitui a suíte completa nem a execução desktop final.

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=writing-final" --results-directory artifacts/writing-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

TRX em `artifacts/writing-green/results/`. Os resultados `writing-corrected` registram o GREEN intermediário. Os resultados `writing-final` registram falhas de carregamento e descoberta incompleta por dependência bloqueada: o runner informou 49 unitários aprovados, 121 integrações aprovadas e 72 falhas, com parte da descoberta prejudicada. Não converter esse resultado em 298 aprovados. `writing-final-storage` registra os 18 finais aprovados. A CI remota não foi executada.

## Diagnóstico do Windows

Eventos CodeIntegrity 3077/3033 apontam a DLL do Trama nas saídas de teste e do harness. Campos do evento:

```text
PolicyName: VerifiedAndReputableDesktop
PolicyGUID: {0283ac0f-fff1-49ae-ada1-8a933130cad6}
Requested Signing Level: 2
Validated Signing Level: 1
Status: 0xc0e90002
```

O responsável confirmou que administra o Windows pessoal. A primeira leitura do log foi negada no sandbox; a consulta elevada, somente leitura, confirmou a política. Nenhuma configuração de proteção, certificado ou assinatura foi modificada.

A [FAQ oficial da Microsoft](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions) informa que o Smart App Control não oferece exceção por aplicativo. A [documentação de desenvolvimento](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview) descreve o caminho de assinatura confiável. Isso exige decisão do responsável sobre um ambiente de execução autorizado ou distribuição assinada; não é autorização para enfraquecer proteção do computador automaticamente.

## Conferência e limites

O harness usa Avalonia.Headless/Skia, SQLite e imagem sintéticos, com cliques/teclas simulados e clipboard do ambiente headless. Não acessa clipboard, banco ou prints pessoais. A rodada que passou importou uma imagem do clipboard e uma por evento Drop. A ampliação para duas colagens consecutivas foi impedida antes da execução; permanece pendente junto com a repetição final.

Sem validação Windows nativa de clipboard, drag/pickers e escala. Sem histórico integrado de desfazer/refazer de formatação, HTML/RTF ou estilos de digitação com seleção vazia. Escrita muito extensa e muitos intervalos não têm benchmark; a composição percorre intervalos ordenados e a renderização depende do TextView.

Todos os testes aprovados devem permanecer intactos. A auditoria compara SHA-256, preservação das migrações anteriores, links locais e padrões de credenciais nos arquivos envolvidos. Nenhum segredo ou log de conteúdo pessoal foi acrescentado. A varredura é limitada aos arquivos textuais alterados.

Snapshot, `writing-green.diff`, `audit.json`, harness e imagens ficam no diretório externo `trama-writing-green`, sob a raiz de visualizações desta sessão. O nome do diretório não indica GREEN final: foi escolhido no início da implementação e o bloqueio está registrado neste relatório. A pasta do projeto continua sem Git.

## Esforço e próximos passos

Um agente, sem subagentes. Esforço alto neste recorte: regras de intervalos, migração compatível, integração visual e diagnóstico do bloqueio Windows. Houve testes focados, rodadas completas para regressões, ajustes do harness e leitura dos eventos de integridade. Tokens/custo monetário não medidos. Para economizar na retomada, resolver primeiro o ambiente e repetir os comandos existentes, sem reexplorar o projeto ou adicionar escopo.

Pendente: decisão do responsável sobre a execução autorizada no Windows; repetir suíte completa/harness final; conferir integrações nativas; revisar o diff antes de considerar o passo 5 pronto. Nenhum dos cinco passos deve ser anunciado como encerrado enquanto essa validação estiver pendente.
