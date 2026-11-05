# Agenda — SQLite e recuperação para revisão

Data: 02/11/2026. Os 24 casos e suas regras foram aprovados antes da implementação e preservados.

## Mudança

AgendaTaskRecord e mapeamento EF, sétima migração AgendaTasks, repositório de escrita/leitura, validação compartilhada e validação das tarefas no backup. Pacote 1 e versões dos cadernos 1/2/3/4 mantidos. Sem registro no desktop ou mudança de interface.

O repositório atualiza pelo ID, preserva os campos, consulta períodos inclusivos com desempate estável e usa leitura sem tracking. A validação do backup lê todas as tarefas e rejeita dados inválidos; restauração de backups anteriores aplica a migração na cópia sem tocar na origem.

## Verificação

- 24 testes aprovados da persistência passaram intactos.
- Suíte completa: 583 aprovados, sendo 101 unitários e 482 integrações; zero falhas/ignorados.
- Build Release sem avisos/erros e dotnet format --verify-no-changes passaram no WSL.
- Migração gerada com dotnet-ef 10.0.9 já fixado no manifesto; apenas criação da tabela nova e índice no Up. Codificação e namespace do arquivo novo ajustados ao editorconfig antes da verificação final.
- Bancos fictícios: migração a partir de LibraryTrash, reabertura, tarefas sem horário, precisão, atualização, ordenação, escrita somente leitura e validação de corrupção.
- Backups: tarefas/cadernos preservados, pacote 1, origem antiga intacta e rejeição de conteúdo inválido com hashes válidos.

Helper final:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Antes disso, o filtro FullyQualifiedName~AgendaStorageTests confirmou os 24 casos. Não houve alteração nos testes para obter GREEN.

## Revisão e limites

As seis migrações distribuídas são preservadas; apenas o snapshot e a migração nova evoluem o modelo. Contratos de snapshot, leitor e repositório foram mantidos. Regras da Application refatoradas para validação compartilhada, protegidas pelos 24 unitários já aprovados.

Instabilidade histórica de prazo em autosave não se reproduziu na rodada completa, mas esta alteração não demonstra sua causa ou correção. Não apagar os relatos anteriores.

Sem mudanças no perfil pessoal, segurança do Windows, manual/PDF/README ou Git. Sem executável novo, formulário ou composição da Agenda. Uso Windows e diff humano pendentes. Atualizações não detectam concorrência entre editores; o contexto continua pertencendo ao chamador.

Esforço moderado, sem subagentes: implementação, geração da migração, 24 casos focados e uma suíte completa executada após ajustar a formatação gerada. Reutilizar o repositório e os contratos na próxima fatia de serviço/formulário, sem antecipar semana visual e indicadores.

Estudo: [capítulo 56](../estudos/56-agenda-sqlite-e-recuperacao.md).
