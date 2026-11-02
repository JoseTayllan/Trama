# Passo 5 — imagens e formatação em RED

29 de outubro de 2026. Especificação para aprovação. O responsável escolheu negrito/itálico por seleção por pergunta assíncrona; implementação ainda não autorizada pelos testes novos.

## Escopo

28 casos novos: 13 unitários de texto/intervalos, dez de coordenação e cinco de persistência/compatibilidade. Cobrem clipboard/drop, propriedade de streams, falha parcial do lote, concorrência, preservação de rascunho, seleção com negrito/itálico, ajuste de intervalos ao editar, migração e restauração de backups do esquema anterior. Ver a [especificação completa](../estudos/19-imagens-e-formatacao.md).

Somente contratos foram criados: InlineTextDocument/flags/intervalos, propriedades vazias nos blocos e métodos provisórios em NotebookEditorViewModel.Writing. A propriedade Styles em NotebookTextBlock preserva os parâmetros do construtor anterior. A classe ViewModel foi dividida como partial, mantendo o mesmo estado. Não foram criados migração, controle rico, adaptadores de clipboard ou eventos de drop; nenhuma dependência foi adicionada.

## Verificação

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --logger "trx;LogFilePrefix=writing-red-final" --results-directory artifacts/writing-red-final/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Build sem erros/avisos; formatação final aprovada. Suíte final: **298 casos, 270 aprovados, 28 falhas esperadas, nenhum ignorado**. Os 36 unitários anteriores e as 234 integrações anteriores continuam passando.

As falhas se dividem em 18 NotImplementedException diretas, três verificações que receberam NotImplementedException em vez de ArgumentException, duas validações que ainda não lançam exceção, uma falha da cópia defensiva de estilos, duas comparações de estilos não persistidos e duas asserções de migração/atualização ainda ausentes. Nenhuma falha final de compilação ou infraestrutura. Vários casos param na primeira operação: as asserções posteriores ainda dependem do GREEN.

A primeira verificação de formatação apontou apenas o inicializador do novo teste de persistência, ajustado antes da revisão. O primeiro ensaio da suíte encontrou um bloqueio de arquivo no descarte de uma fixture nova: a fábrica de design usava pooling. O teste foi corrigido para contexto temporário com Pooling=false e a suíte foi reconferida. Nenhum teste anterior foi alterado; não houve mudança de produção para ocultar essa falha.

TRX em `artifacts/writing-red-final/results/`. Não foi acessado banco pessoal, clipboard real ou arquivo pessoal de imagem. O modelo de clipboard dos testes é controlado. A leitura do XML dos pacotes instalados confirmou disponibilidade de bitmap no clipboard Avalonia; o adaptador ainda não foi escrito.

## Riscos e revisão

O ponto principal é acompanhar os estilos quando o texto muda, sem alterar o conteúdo puro nem enfraquecer a busca. Outro risco é invalidar os backups existentes ao acrescentar a migração: o caso do esquema anterior exige atualização na cópia de staging e manutenção da origem com quatro migrações. A definição do formato de conteúdo e a ligação ao controle visual precisam ser revisadas no GREEN.

O lote de imagens aceita sucessos parciais e não salva a página automaticamente. A fonte retorna stream pertencente ao chamador. Colagem comum de texto/código deve continuar funcionando; o roteamento exato de atalhos e a integração Windows ainda não estão testados. Desfazer/refazer integrado, HTML/RTF e outras formas de texto enriquecido ficam fora da especificação atual.

O diff e snapshot anterior estão no diretório externo `trama-writing-red`, sob a raiz de visualizações da sessão. A auditoria compara hashes dos testes anteriores, migrações, links locais e padrões de credenciais literais nos arquivos envolvidos. Nenhum log foi acrescentado.

Um agente, sem subagentes, realizou investigação focada, especificação, revisão e verificações. Esforço moderado: uma preferência obtida do responsável, build, duas execuções completas e verificação/correção de formatação. Tokens e custo monetário não medidos. Para economizar no GREEN, trabalhar primeiro nos 13 casos puros de texto, depois na coordenação/persistência, deixando a suíte completa para a integração.

Próxima decisão: aprovação dos 28 testes antes da implementação, conforme CODEX, seção 9, “PARAR para revisão”.
