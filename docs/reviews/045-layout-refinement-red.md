# Refino responsivo: RED para revisão

30/10/2026. Somente testes e documentação. Produção, banco, pacotes, testes anteriores, manual e PDF preservados. O responsável esclareceu que “busca na página” significa os filtros da busca lateral, sem pedido de Ctrl+F.

## Especificação

12 casos novos em `NotebookLayoutRefinementTests.cs`. Dez falham por comportamentos ausentes: marcadores visíveis de texto, linha única de alinhamento, atualização dos marcadores visíveis após mover/remover, controles de recolhimento lateral, modo compacto, filtros dentro da região rolável, expansão da formatação preservando seleção e menu que substitui as setas e aciona os comandos atuais. Dois passam como proteções: redimensionar conserva rascunho/editor e recolher os filtros por teclado mantém critérios sem modificar a página.

O recolhimento dos filtros por teclado funciona no ambiente de teste atual; não alegar que a falha relatada de interação foi reproduzida integralmente. A distribuição do espaço está inadequada, pois os filtros ficam fora da região rolável. A implementação deve conferir também clique e renderização com filtros abertos e fechados.

## Execução

O helper `tools/run-wsl.sh test` confirmou build Release sem erros/avisos, formatação e os 414 casos anteriores aprovados. A preparação dos testes teve correções de imports, referência a PageId e identificação da árvore lógica de controles recolhidos; erros de compilação/localização não foram usados como evidência RED.

Após essas correções, execução focada:

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~NotebookLayoutRefinementTests --logger trx --results-directory artifacts/layout-refinement-red --verbosity quiet
```

Resultado final: **12 descobertos, 10 falhas esperadas, dois aprovados, zero ignorados**. O total de casos definidos passa a 426; não há GREEN nesta entrega. A execução completa anterior e a focada final são evidências distintas.

Artefatos externos na pasta `trama-layout-refinement-red`: `unit.trx`, `integration.trx`, `refinement.trx`, `implementacao.diff`, `audit.json` e resumo das falhas. Fontes de produção e testes anteriores conferidos por hashes.

## Plano e riscos

Após aprovação: margem única, painéis recolhíveis sem recriar a prosa, busca dentro da rolagem lateral, menu textual de blocos e tratamento visual das barras. Preservar teclado, seleção, histórico, autosave e acesso a código longo. Conferir janela compacta, filtros abertos, menus e escalas antes de publicar uma versão isolada para revisão. A mudança visual das barras será verificada por renderização, sem testes artificiais de detalhes do template.

O menu proposto usa “Antecipar bloco”, “Adiar bloco” e “Remover bloco”. Lateral inicialmente recolhida na largura de 860 unidades e formatação expansível são comportamentos novos e aguardam aprovação desta especificação. Não implementar arraste nem busca dentro da página nesta etapa.

Um agente, sem subagentes; esforço moderado na preparação e depuração da especificação. Não há medição de tokens/custo. Economia: após aprovação, usar primeiro os 12 casos focados e somente então a suíte completa e capturas; reutilizar o perfil e o harness existentes. Revisão humana dos testes e do diff pendente, exigida pela seção 9 do CODEX.
