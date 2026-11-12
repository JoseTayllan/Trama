# Vínculo de tarefas com páginas dos Cadernos

Data de referência do projeto: 11/11/2026.

Uma tarefa salva pode receber um vínculo no formulário da Agenda. O seletor lista as páginas ativas, com título e localização. É possível abrir a página pelos Cadernos ou desvinculá-la mantendo a página no acervo. A navegação usa o ID e localiza a página no caderno atual. O fluxo de confirmação dos Cadernos preserva rascunhos em caso de cancelamento. A página na lixeira pode ser aberta após a restauração.

A migração 13 cria AgendaPageLinks. A chave composta TaskId e PageId permite várias tarefas por página e várias páginas por tarefa. O formulário desta entrega gerencia um vínculo por tarefa. Tarefas de qualquer data podem apontar para a mesma página. A migração preserva tarefas e fechamentos existentes. A validação de backup verifica a referência à tarefa e a uma página estruturada ou nota antiga.

Build Release, formatação e modelo EF passaram no WSL. O teste isolado do SQLite confirmou três tarefas em dois dias compartilhando uma página, uma tarefa vinculada a duas páginas e rejeição de pares duplicados. Os quatro casos aprovados passaram intactos. A suíte completa passou com 1.329 testes, sendo 224 unitários e 1.105 de integração. A validação manual do fluxo permanece pendente.
