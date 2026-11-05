# Agenda — serviço integrado para revisão

Data: 03/11/2026. Os 15 casos foram aprovados explicitamente e preservados.

## Alteração

Dois arquivos de produto: SqliteAgendaActions e DesktopComposition. O serviço guarda opções de conexão, abre banco existente e cria/descarta contexto por operação. A composição fornece IAgendaActions para o perfil já inicializado. Gravação reutiliza SaveAgendaTask; consultas reutilizam SqliteAgendaTaskRepository.

Sem nova regra de tarefa, migração, contrato modificado, tela ou navegação funcional da Agenda. Perfis pessoais, manual/PDF/README e Git intactos.

## Verificação

- 15 testes aprovados passaram intactos no filtro AgendaServiceTests.
- Suíte completa: 598 aprovados, sendo 101 unitários e 497 integrações; zero falhas/ignorados.
- Build Release sem avisos/erros e dotnet format --verify-no-changes passaram no WSL.
- Verificados cancelamento antes de I/O, banco ausente sem arquivo novo, validação, isolamento entre perfis, novas instâncias, atualização externa e registro do serviço.
- Uma escrita rejeitada por trigger não apareceu na gravação seguinte; seis gravações simultâneas de tarefas distintas preservadas.
- Revisão: nenhum contexto/conexão aberto fica armazenado no singleton; await using encerra cada contexto e o token segue para as camadas existentes.

Comando completo:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

## Limites

Diff e uso Windows da futura Agenda ainda precisam de revisão. Não há formulário/lista nem proteção de rascunhos da Agenda nesta fatia. A concorrência de IDs distintos não adiciona controle de conflito para o mesmo ID.

O timeout histórico de autosave não se reproduziu na suíte completa atual; nenhuma correção específica dele foi realizada. Preservar os relatos anteriores.

Esforço baixo, sem subagentes: uma implementação, teste focado e uma rodada completa com build/formatação. Reutilizar a fachada nos testes de formulário e reservar os testes SQLite para as fronteiras necessárias.

Estudo: [capítulo 58](../estudos/58-agenda-servico-implementado.md).
