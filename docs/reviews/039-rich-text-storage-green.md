# Persistência dos atributos: GREEN para revisão

30/10/2026. Implementação autorizada pela aprovação explícita dos 21 casos e das regras de versões. Testes aprovados e anteriores preservados integralmente. Dois fontes de produção alterados: `SqliteNotebookRepository.cs` e `LocalBackupService.Validation.cs`.

## Resultado e validação

Páginas sem estilos usam 1; somente negrito/itálico usam 2; atributos novos usam 3. O leitor valida correspondência entre versão e conteúdo, localização dos estilos e intervalos. Gravação aceita atualização de 3 e recalcula a versão após remover atributos. O backup reutiliza o leitor em vez de manter uma lista duplicada de versões. Pacote continua 1; nenhuma migração foi criada/alterada.

Comando:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Restore travado aprovado; build Release com zero avisos/erros; formatação aprovada. **374 aprovados, zero falhas, zero ignorados**: 77 unitários e 297 de integração, incluindo os 21 aprovados. Uma execução completa após a implementação.

Checagem externa adicional confirmou leitura/gravação de texto/código em banco temporário com quatro migrações, sem tabela de estilos, e preservação após atualizar o esquema. A checagem verificou também que ler a página não alterou o esquema antigo. Resultado: sucesso. Não houve acesso a perfil pessoal.

## Revisão do diff

Revisão focada em validação antes da substituição, transação/cascata já existentes, leitura de metadados em páginas 1 quando a tabela existe, recusa de versões futuras e delegação da interpretação das páginas pelo backup. A consulta ao catálogo SQLite é constante, sem entrada do usuário ou logs de conteúdo. Nenhum segredo foi introduzido nos arquivos alterados.

Evidências externas da sessão: `trama-rich-storage-green/implementacao.diff`, `audit.json`, `unit.trx`, `integration.trx`, `SchemaSmoke.csproj` e `Program.cs`. Comparação por hashes preserva testes, locks, migrações e manual/PDF. Revisão humana do diff permanece pendente.

Não houve mudança visual, nova publicação Windows ou validação interativa de cores. A entrega conclui o recorte de persistência, não os controles da escrita rica. Próxima fatia: testes de seleção/ações, renderização, foco, estados mistos e histórico; conservar a hierarquia Espaço → Caderno → Páginas.

Esforço moderado: um agente, sem subagentes; uma rodada de implementação/revisão, uma execução completa da suíte e uma checagem isolada de esquema antigo. Sem medição de tokens ou custo monetário. Para economizar na próxima fatia, reutilizar as fixtures do editor e trabalhar primeiro nas ações de formatação, sem reexplorar armazenamento/backup.
