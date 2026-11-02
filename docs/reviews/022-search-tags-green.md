# Passo 3 — busca e tags implementadas

28 de outubro de 2026. GREEN após aprovação explícita dos 30 testes; diff disponível para revisão humana.

## Resultado e arquitetura

Busca global literal nas páginas salvas por título, texto, código e legenda, sem distinguir caixa ou acentos. Filtro de uma tag exata, resultados com localização, abertura protegida por Salvar/Descartar/Cancelar e revalidação do destino atual. Tags por página salva podem ser substituídas ou removidas sem gravar/descartar o rascunho textual.

IKnowledgeDiscovery está registrado em DesktopComposition. SqliteKnowledgeDiscovery usa contexto por operação, busca com leitura transacional e substituição de tags em transação. PageTags centraliza trim/NFC/minúsculas e deduplicação; o conteúdo salvo permanece intacto. LibraryViewModel.Discovery.cs separa a coordenação em uma classe parcial, compartilhando o mesmo estado e guardas de navegação.

A nova migração `20261029011618_PageTags` adiciona PageTags com chave composta PageId/Name e índice por Name. Seu identificador é gerado pelo EF em UTC; a data local desta entrega continua 28/10. As três migrações anteriores não foram reescritas. A associação admite tanto Notes quanto NotebookPages; validar existência pela aplicação evita impor FK incompatível com páginas antigas. Exclusão futura deve tratar os metadados.

Views adicionam barra de busca, filtro, resultados recolhíveis e diálogo de tags com uma entrada por linha. Fechar/cancelar mantém dados. O botão Tags habilita após o primeiro salvamento e a apresentação das tags acompanha a página aberta.

## Verificação

```powershell
dotnet ef migrations add PageTags --project src/Trama.Infrastructure --startup-project src/Trama.Desktop --configuration Release --output-dir Persistence/Migrations -- --database C:/Users/joset/www/Trama/artifacts/tag-design.db
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj --configuration Release --artifacts-path artifacts/window-close-build --no-restore --filter "FullyQualifiedName~SearchTags" --verbosity quiet
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=search-tags-final" --results-directory artifacts/search-tags-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

O comando de migração acima é um registro da geração já feita, não deve ser repetido para executar o aplicativo. Não houve atualização manual do banco pessoal. A inicialização normal aplica as migrações conhecidas conforme o fluxo existente.

- Focados: 30/30 aprovados.
- Suíte final: 230/230, 36 unitários e 194 integrações, nenhum ignorado.
- Build final: zero avisos/erros. Formatação final: sucesso.
- A primeira verificação de formatação detectou CRLF/BOM e namespace no novo arquivo gerado pelo EF; somente essa migração nova foi adequada. Build e suíte foram reconferidos depois.
- TRX em `artifacts/search-tags-green/results/`; CI remota não executada.
- Testes aprovados preservados por comparação SHA-256; auditoria também verifica migrações anteriores, links locais e padrões de credenciais literais nos arquivos envolvidos. Não foram acrescentados logs ou mensagens de exceção internas na UI.

Conferência externa com Avalonia.Headless/Skia, composição e SQLite reais em diretório sintético isolado: diálogo de tags, normalização, preservação de rascunho, título/código, tag exata, nenhum resultado, Cancelar/Salvar ao abrir, resultado movido para outro espaço após a consulta, salvamento no destino atual, fechar busca, cancelar/remover tags e reabrir SQLite. Exit code 0.

O primeiro ensaio do harness levantava apenas o evento Click, que não executa um Command do botão. Foi corrigido para usar MouseDown/MouseUp do Avalonia.Headless; a execução seguinte passou. Nenhuma alteração nos testes aprovados foi necessária. As imagens `page-tags.png` e `search-results.png` foram inspecionadas, com controles e diálogo legíveis e sem sobreposição.

## Riscos, revisão e esforço

O principal risco era confiar na localização antiga de um resultado. A abertura agora consulta o catálogo atual, prepara o editor com esse caderno e só então muda a seleção. Teste aprovado e conferência com SQLite demonstram o salvamento no destino correto. Tags são atualizadas separadamente do conteúdo; falhas mantêm o rascunho e a lista apresentada anteriormente.

A busca inicial percorre e carrega o acervo textual em memória, sem FTS/paginação ou benchmark de grande volume. Resultados permanecem uma fotografia até repetir Buscar. Não inclui OCR, busca semântica ou outros módulos futuros. A suíte cobre validação, pré-cancelamento e preservação dos dados; não injeta todos os possíveis erros de disco durante uma transação. Teclado e escala do Windows em uso interativo continuam sem conferência nativa.

Snapshot anterior, `search-tags-green.diff`, `audit.json`, harness e imagens ficam no diretório externo `trama-search-tags-green`, sob a raiz de visualizações desta sessão. A pasta do projeto continua sem Git.

Um agente, sem subagentes, executou implementação, revisão, testes e documentação em sequência. Esforço moderado: testes focados, duas execuções completas (a última após formatar a migração), verificações de build/formatação e duas rodadas do harness devido ao ajuste descrito. Tokens/custo monetário não medidos. Para economizar na próxima etapa, restringir o trabalho ao backup/restauração e reutilizar o mapa de persistência existente.

Pendente do responsável: revisão do diff. Próximo passo: especificar backup de banco/anexos e restauração isolada, com testes antes da implementação.
