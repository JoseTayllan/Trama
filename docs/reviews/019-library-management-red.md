# Passo 2 — gestão da biblioteca em RED

28 de outubro de 2026. Estado: especificação para aprovação; recurso não implementado.

## Escopo e revisão

29 casos novos: 15 de persistência e 14 de coordenação visual. Especificam renomeação dos três níveis, movimentação entre cadernos/espaços, preservação de conteúdo, notas antigas, validação, cancelamento, confirmação do rascunho, falha de gravação e salvamento posterior no destino correto.

Consultar a [explicação e regras propostas](../estudos/16-gestao-da-biblioteca.md). Sem exclusão, tags, backup ou mudanças de migração nesta etapa.

Os contratos provisórios estão em ILibraryManagementActions, SqliteLibraryManagement e quatro métodos do LibraryViewModel. Os métodos de operação lançam NotImplementedException. Não foram registrados no container nem conectados aos controles. O parâmetro opcional do ViewModel mantém a construção com dois argumentos já usada nos testes anteriores.

## Verificações

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/window-close-build --no-build --logger "trx;LogFilePrefix=library-management-red" --results-directory artifacts/library-management-red/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

- Build: sucesso, zero avisos e erros.
- Testes: 200 casos, 171 aprovados, 29 falhas esperadas, nenhum ignorado. As 36 unidades e 135 integrações anteriores continuam aprovadas.
- Falhas novas: 21 NotImplementedException diretas e oito verificações de exceção que receberam NotImplementedException em vez da exceção contratada. Nenhum caso falhou por compilação ou ausência de banco de teste.
- Formatação: sucesso após repetir fora do sandbox; a primeira tentativa teve acesso negado ao canal local do build host.
- A primeira tentativa de build usou um diretório novo com --no-restore e falhou por ausência de project.assets.json. A execução válida acima reutilizou a saída isolada com pacotes já restaurados.
- TRX em `artifacts/library-management-red/results/`. Nenhum dado pessoal ou banco do aplicativo foi usado.

Os testes permanecem RED deliberadamente. Não apresentar esta etapa como funcionalidade pronta. Os testes antigos devem permanecer intactos; os novos tornam-se a especificação congelada após aprovação.

## Riscos e limites

A interface ainda não possui as ações novas. A implementação precisa preservar o rascunho nas falhas e recriar ou atualizar o vínculo do editor após mover. Os casos de coordenação usam ações controladas; conferência da composição real e dos diálogos fará parte do GREEN. Falhas de recarga após uma escrita confirmada exigem atenção na revisão.

Diff unificado e snapshot anterior estão no diretório externo `trama-library-management-red`, sob a raiz de visualizações da sessão. O repositório continua sem Git. A auditoria final compara hashes dos testes existentes, verifica as migrações e procura padrões de credenciais literais somente nos arquivos alterados.

## Esforço

Um agente, sem subagentes. Uma rodada de especificação e execução da suíte completa; duas tentativas de build e duas de formatação pelos motivos registrados acima. Esforço moderado, concentrado em dois arquivos de testes. Tokens e custo monetário não foram medidos. Para economizar na próxima etapa, reutilizar estes contratos e começar pelos 29 casos afetados; executar a suíte completa ao concluir a implementação.

Próxima decisão necessária: aprovação destes testes antes de escrever a implementação, conforme CODEX, seção 9, passo 3: “PARAR para revisão”.
