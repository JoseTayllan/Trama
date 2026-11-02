# Passo 3 — busca e tags em RED

28 de outubro de 2026. Especificação para aprovação. Nenhuma funcionalidade nova conectada à interface.

## Escopo

Busca global por trechos em título/texto/código/legenda, sem diferenciar caixa ou acentos; filtro de uma tag exata; tags normalizadas por página salva; abertura dos resultados com proteção dos rascunhos e revalidação da localização. Veja a [especificação completa](../estudos/17-busca-e-tags.md).

19 casos de persistência e 11 de coordenação. Os testes cobrem compatibilidade de notas antigas, deduplicação de páginas e tags, metadados, pontuação literal, substituição/remoção e isolamento de tags, entrada inválida, cancelamento, falhas seguras, confirmação de rascunho e salvamento no destino atual de um resultado antigo.

Somente contratos mínimos foram adicionados: IKnowledgeDiscovery/PageSearchResult, SqliteKnowledgeDiscovery e métodos/propriedades provisórios no LibraryViewModel. Os métodos novos lançam NotImplementedException. O port não está registrado na composição; o novo argumento do construtor é opcional. Não houve alteração de migrações, pacotes, controles ou testes anteriores.

## Execução

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=search-tags-red" --results-directory artifacts/search-tags-red/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Build: sucesso, zero erros/avisos. Formatação: sucesso. Testes: 230 no total, **200 aprovados, 30 falhas esperadas, nenhum ignorado**. Os 36 unitários e os 164 casos anteriores de integração continuam passando.

Motivos das falhas novas: 27 NotImplementedException diretas; duas verificações de exceção que receberam NotImplementedException em vez da exceção esperada; uma asserção de migração adicional ausente. Alguns cenários param já na preparação de tags ou na primeira busca; isso não comprova que todas as asserções seguintes foram executadas. O RED não decorre de erro de compilação nem de falta de infraestrutura de teste.

TRX em `artifacts/search-tags-red/results/`. Os bancos usados são temporários e sintéticos. Nenhum banco pessoal foi aberto ou migrado.

## Revisão, riscos e próximos passos

O principal risco é abrir um resultado com localização antiga e devolver a página ao caderno anterior no salvamento. A especificação exige consultar o destino atual. Outra fronteira é modificar tags sem gravar ou descartar o rascunho textual. A persistência deverá preservar a lista inteira em caso de falha; a suíte não injeta todos os possíveis erros de disco durante a transação.

A implementação precisa criar uma migração compatível; ainda não existe esquema de tags. Nenhum desempenho para grande volume foi medido, nem foram conferidos controles de busca/tags. O port é exclusivo do módulo Knowledge; não estender a pesquisa para o futuro cofre.

Snapshot, diff `search-tags-red.diff` e auditoria ficam no diretório externo `trama-search-tags-red`, sob a raiz de visualizações da sessão. A auditoria compara hashes dos testes anteriores e migrações, valida links locais e procura padrões de credenciais literais nos arquivos envolvidos. Não foram adicionados logs.

Um agente, sem subagentes; uma rodada de especificação, um build, uma execução completa da suíte e uma verificação de formatação. Esforço moderado; tokens e custo monetário não medidos. Para economizar no GREEN, começar pelos 30 casos afetados e executar a suíte completa ao concluir a integração.

Próxima decisão: aprovação dos testes. O CODEX, seção 9, exige “PARAR para revisão” antes de escrever a implementação.
