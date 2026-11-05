# 75 — Datas de término: regras implementadas

Data: 05/11/2026. Implementação após aprovação dos 18 casos; diff para revisão.

AgendaTaskValidation calcula a data final efetiva como PlannedEndDate ou PlannedDate. Rejeita data anterior; compara as horas somente quando as datas são iguais. Assim, 05/11 às 22h até 06/11 às 02h é válido sem trocar os horários. Fim sem início continua inválido e ausência de hora final permanece permitida. Sem data final explícita, o comportamento anterior de mesmo dia é preservado.

ApplicationCloseCoordinator usa CompletedAt da decisão quando informado; caso contrário, consulta o relógio para manter o encerramento automático. Antes de chamar o serviço, rejeita instante futuro e data local anterior ao dia planejado de qualquer tarefa selecionada. A comparação de futuro usa o instante DateTimeOffset; a comparação de dia usa a data no offset informado, coerente com a regra existente do serviço. O offset é preservado na gravação pelo armazenamento já existente de LastCompletedAt.

Validar antes da chamada evita concluir parcialmente um lote inválido. As ações Cancelar e Manter abertas continuam saindo antes dessa validação. Falhas devolvem feedback e liberam a pausa de autosave pelo finally existente. Nenhum novo serviço ou dependência foi introduzido.

## Verificação

Os 18 casos aprovados passaram intactos: 14 unitários e quatro integrações. Os 57 arquivos anteriores de testes/configuração e os contratos da especificação foram conferidos por hashes. Suíte completa: **787 aprovados, 131 unitários e 656 integrações**, sem falhas/ignorados. Build Release sem avisos/erros e formatação aprovados no WSL, usando perfis fictícios.

```bash
dotnet build Trama.slnx -c Release --no-restore
dotnet format Trama.slnx --verify-no-changes --no-restore
dotnet test Trama.slnx -c Release --no-build
```

## Limites e próxima fatia

Esta entrega implementa regras e encaminhamento. A data planejada ainda não tem coluna/mapeamento SQLite nem controles no formulário: o repositório atual não deve ser usado para salvar PlannedEndDate não nulo, pois ainda não o preserva. O diálogo real ainda não oferece edição de CompletedAt; mantém o padrão automático. Não apresentar as duas opções como funcionalidades disponíveis no aplicativo.

Próximo passo: testes de persistência/migração/backup da data final planejada, preservando tarefas e versões antigas. Depois, testes dos campos compactos de planejamento e data/hora real no diálogo. Consulta semanal de intervalos, validação em relação à hora inicial exata e histórico de conclusões não integram esta especificação. Confirmação/tela da lixeira seguem pendentes.

Sem migração, publicação Windows, perfil pessoal, Git ou manual/PDF alterados. Para estudar: por que comparar somente horas falha à meia-noite? Por que regras corretas em memória não garantem persistência? Como validar um lote antes de produzir efeitos?

[Especificação aprovada](74-datas-de-termino-especificacao.md) · [Relatório](../reviews/082-agenda-cross-day-green.md).
