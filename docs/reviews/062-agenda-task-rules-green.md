# Agenda — regras de gravação para revisão

Data: 02/11/2026. Os 24 casos e as regras foram aprovados explicitamente antes da implementação.

## Alteração

Somente `src/Trama.Application/Agenda/SaveAgendaTask.cs` altera comportamento nesta fatia. Armazena a dependência, valida ID/título/enums/intervalo e aguarda a escrita com o token recebido. Preserva o snapshot e diferencia falhas esperadas, cancelamento e defeitos inesperados. Não altera contratos ou testes aprovados.

## Verificação

- 101 testes unitários aprovados: os 24 novos e os 77 anteriores, nenhum ignorado.
- Build Release da solução sem avisos/erros e verificação de formatação no WSL.
- Revisão do fluxo: validação antecede escrita, resultado de sucesso vem após await, apenas AgendaPersistenceException é convertida em falha de persistência.
- Testes anteriores e teste novo preservados por hash; contratos de snapshot/repositório intactos.

Comandos no espelho Linux:

```sh
dotnet test tests/Trama.UnitTests -c Release --no-restore --logger trx --results-directory artifacts/agenda-rules-green --verbosity quiet
dotnet build Trama.slnx -c Release --no-restore --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

## Limites e riscos

Repositório controlado demonstra o caso de uso, sem persistência real. Sem nova migração, dependência, alteração da composição, tela ou executável. O consumidor ainda deve gerar/preservar o ID correto; o caso de uso não consulta versões anteriores. SQLite e recuperação precisam de testes antes da disponibilização funcional.

Integrações não foram reexecutadas nesta fatia isolada. A instabilidade anterior de autosave continua aberta e não é encerrada pelos 101 unitários. O total disponível passa a 559 casos; não declarar suíte completa GREEN com esse número.

Manual/PDF/README, perfis pessoais e Git intactos. Diff para revisão humana; implementação técnica é distinta do aceite final.

Esforço baixo, sem subagentes: uma implementação, um ciclo dos unitários e build/formatação. Próxima etapa deve reutilizar os contratos e concentrar exploração no SQLite e nos backups.

Estudo: [capítulo 54](../estudos/54-agenda-regras-implementadas.md).
