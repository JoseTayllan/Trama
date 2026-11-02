# Passo 2 — gestão da biblioteca implementada

28 de outubro de 2026. GREEN após aprovação explícita dos 29 testes; disponível para revisão do diff.

## Resultado

A interface renomeia espaços, cadernos e páginas e move páginas salvas para cadernos do mesmo espaço ou de outro. Preserva identidade, texto, código e referências de imagens. Renomear os pais mantém o rascunho sem confirmação; renomear/mover a página usa Salvar/Descartar/Cancelar. A página nova precisa ser salva primeiro.

SqliteLibraryManagement usa contexto por operação, valida títulos e muda somente o registro visível. Notas antigas continuam compatíveis; uma página estruturada com o mesmo ID prevalece e a nota histórica permanece intacta. A movimentação reutiliza PlacePageAsync e sua transação. Não há alteração de migrações, pacotes ou contratos de ILibraryActions.

LibraryViewModel prepara leituras, listas e editor antes da mutação, publica o estado depois do sucesso e evita leituras posteriores à escrita. O editor preparado para mover recebe o destino correto. NotebookEditorViewModel.RestoreSnapshot foi extraído da abertura existente para reutilizar a reconstrução limpa. Falhas de persistência mantêm editor e seleção; detalhes internos não aparecem nas mensagens.

Views usam recursos semânticos de cor, ações nos cabeçalhos e dois diálogos. O diálogo de renomeação rejeita branco; o de movimentação exige seleção e indica quando não há outro destino. Fechar ou cancelar os diálogos mantém os dados. A composição registra o novo port, mantendo a construção antiga do ViewModel compatível.

## Validação

```powershell
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --artifacts-path artifacts/window-close-build --no-restore --filter "FullyQualifiedName~LibraryManagement" --verbosity quiet
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=library-management-green" --results-directory artifacts/library-management-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

- Focados: 29/29 aprovados.
- Suíte completa: 200/200, 36 unitários e 164 de integração, nenhum ignorado.
- Build: zero erros e avisos. Formatação: sucesso.
- Os arquivos de testes aprovados foram preservados; auditoria por SHA-256 contra o snapshot anterior à implementação.
- Auditoria final: nenhum teste aprovado alterado, nenhuma migração alterada, nenhum link local quebrado e nenhum padrão de credencial literal encontrado nos arquivos envolvidos. A revisão não identificou logs novos ou mensagens internas expostas.
- TRX em `artifacts/library-management-green/results/`. CI remota não executada.

Conferência externa em Avalonia.Headless com Skia, composição real e banco/anexos sintéticos isolados: botões de renomear os três níveis, validação de branco, preservação de rascunho ao renomear pai, Cancelar e Salvar na confirmação, fechar o diálogo de destino, mover entre espaços, salvar depois de mover e reabrir SQLite. Texto atualizado, bloco de código e referência de imagem foram comparados; o arquivo da imagem também foi reaberto.

O harness externo terminou com exit code 0. Imagens `rename-space.png`, `move-destination.png` e `library-managed.png` foram inspecionadas: sem corte nos diálogos ou nos controles novos. A imagem sintética é mínima e não representa um print pessoal. A execução não substitui validação interativa de teclado e escala do Windows.

## Revisão e limites

O principal risco identificado era salvar no caderno anterior depois de mover. Corrigido ao preparar e substituir o editor com o destino; protegido pelo teste aprovado e pela conferência SQLite. Outro risco era falhar uma recarga depois de uma escrita confirmada: as leituras foram colocadas antes da mutação e o novo estado é preparado em memória.

O contrato mantém os nomes técnicos de páginas existentes e não amplia ILibraryActions. Nenhum segredo é necessário ao recurso; não foram acrescentados logs ou exposição de mensagens internas. A auditoria do diff procura credenciais literais e verifica links locais, migrações e integridade dos testes. Trata-se de varredura limitada, não de auditoria de histórico/binários.

Exclusão, movimentação dos próprios cadernos, backup, autosave e coordenação entre múltiplas instâncias permanecem fora desta entrega. Os diálogos permitem nomes repetidos porque a identidade é o GUID. Arquivos de imagens não são regravados ao mover ou renomear.

## Artefatos e esforço

Snapshot anterior, `library-management-green.diff`, `audit.json`, harness e imagens ficam no diretório externo `trama-library-management-green`, sob a raiz de visualizações da sessão. O repositório continua sem Git.

Um agente executou implementação, revisão, testes e documentação em sequência, sem subagentes. Uma rodada de implementação, 29 testes focados, uma suíte completa de 200, build, formatação e uma conferência externa com controles reais. Esforço moderado; tokens e custo monetário não foram medidos. Para economizar na etapa 3, manter o escopo em busca/tags e preparar somente seus testes antes da revisão TDD.

Pendente do responsável: revisão do diff desta entrega. Próximo passo do plano: especificar busca por título/conteúdo e tags antes da implementação.
