# Validação de backup e restauração

Em 17/11/2026, foram adicionados dois testes de integração com perfis temporários. O primeiro cria e restaura um backup após mover o perfil de origem. Confere tarefa, vínculo com a página, término planejado, conclusão real, 75 minutos de execução, fechamento semanal, reflexão e data de edição. Os casos existentes também verificam imagens e tags.

O segundo restaura um pacote versão 1 com banco na migração 12 e verifica que o destino recebeu todas as migrações. A tarefa e a página foram preservadas, o banco de origem ficou idêntico e o destino manteve ausentes os vínculos, as horas realizadas e a data de edição sem dados históricos.

No WSL, o build Release, a verificação de formatação, os 24 testes focados e a suíte completa de 1.358 testes passaram. No Windows local, o Controle de Aplicativos bloqueou Avalonia.Generators.dll com 0x800711C7 antes da execução. O Verify do GitHub será a confirmação para o commit 2. A etapa altera testes e documentação.
