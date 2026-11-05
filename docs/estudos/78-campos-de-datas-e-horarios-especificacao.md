# 78 — Campos de datas e horários: especificação visual

Data: 05/11/2026. Testes em revisão; interface ainda não alterada.

O responsável pediu os campos visuais de término e a substituição do seletor de horário que exige rolar listas. A proposta mantém a paleta do Trama e usa entradas compactas de texto HH:mm para início/fim, sem TimePicker. Datas continuam com seleção de calendário; data final planejada é opcional. No aviso de saída, o padrão continua Agora, com opção Informar data e hora e os mesmos horários por digitação direta.

## Comportamento proposto

PlannedEndDate deve carregar/gravar no formulário e compor o estado do rascunho. Criar outra tarefa ou salvar pelo botão limpa os campos sem perder o que foi gravado. Horários digitados válidos de 00:00 até 23:59 são convertidos sem listas de rolagem. Campo vazio significa ausência; texto parcial ou inválido permanece no editor, marca alterações e impede gravação, com feedback. Não converter entrada inválida silenciosamente em null nem substituir os dados anteriores.

Os contratos de StartTime/EndTime existentes devem continuar disponíveis para consumidores internos e sincronizar com StartTimeText/EndTimeText. A data final faz parte de carga, captura e baseline; cancelamento de navegação e falha de gravação mantêm os dados. A lista deve mostrar o dia final quando diferente, evitando exibir apenas 22h–02h como se fossem do mesmo dia.

O diálogo oferece UseCustomCompletionTime, CompletionDate e CompletionTimeText. No modo manual, data e hora são obrigatórias e entrada inválida desabilita a conclusão. Retornar a Agora ignora campos incompletos e mantém CompletedAt null, para o coordenador consultar o relógio. Cancelar/Manter abertas continuam independentes da validade dos campos. Os limites de futuro e dia inicial seguem validados pelo coordenador já implementado. A data informada preserva seu offset na composição do instante.

## Evidências e alteração antiga proposta

AgendaDateControlsTests tem **33 casos: 31 falhas esperadas e duas proteções passando**. Falhas são ausência de propriedades/controles, seletores antigos e resumo sem data final. Testes compilam sem contratos fictícios: reflexão exige propriedades públicas esperadas e falha por asserção se ausentes. Duas guardas protegem Agora e cadastro pendente somente com início.

Build Release sem avisos/erros e formatação passaram no WSL. A base anterior foi reexecutada: **811 aprovados (131 unitários e 680 integrações)**. Todos os arquivos anteriores de src/tests permanecem intactos por hashes. Casos nativos headless exercitam os controles, gravação SQLite, botão Salvar e janela compacta; isso não representa aprovação estética.

NativeForm_CreatesATaskInTheSameLocalProfile usa TimePicker em duas linhas. A proposta de diff muda somente essas entradas para TextBox.Text 18:00/19:00, preservando as asserções de persistência, valores e lista. O arquivo antigo não foi alterado; precisa de aprovação explícita junto dos novos casos.

```bash
dotnet test tests/Trama.IntegrationTests/Trama.IntegrationTests.csproj -c Release --no-build --filter FullyQualifiedName~AgendaDateControlsTests
dotnet test Trama.slnx -c Release --no-build --filter 'FullyQualifiedName!~AgendaDateControlsTests'
```

Próximo passo após aprovação: integrar ViewModels e campos compactos, aplicar somente o ajuste antigo aprovado, manter casos intactos, verificar suíte e capturas reais ampla/compacta. Não houve migração, código funcional, publicação Windows, perfil pessoal, Git ou manual/PDF alterados. Confirmação/tela da lixeira e semana visual seguem no plano.

Para estudar: por que texto parcial precisa integrar o rascunho? Por que uma hora inválida não pode virar campo vazio? Como preservar compatibilidade ao trocar um controle sem alterar o contrato do armazenamento?

[Relatório](../reviews/085-agenda-date-controls-red.md) · [Persistência implementada](77-data-final-persistida.md).
