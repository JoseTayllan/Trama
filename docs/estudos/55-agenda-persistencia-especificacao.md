# 55 — Agenda: persistência e recuperação em revisão

Data: 02/11/2026. RED para revisão; ainda não há armazenamento funcional da Agenda.

**Atualização:** os 24 casos e as regras foram aprovados em 02/11. A implementação e os resultados posteriores estão no [capítulo 56](56-agenda-sqlite-e-recuperacao.md). O RED abaixo permanece como histórico.

## Contratos e limites

O caso de uso do capítulo 54 permanece intacto. `IAgendaTaskReader` acrescenta consultas por identidade e período inclusivo. `SqliteAgendaTaskRepository` implementa os contratos de escrita/leitura, com métodos vazios para compilar os testes. A guarda de contexto nulo já funciona; as operações lançam NotImplementedException.

O contexto pertence ao chamador, seguindo os repositórios existentes. Ao integrar o desktop, criar/descartar um contexto por operação; não registrar DbContext como singleton nem entregar entidades rastreadas aos ViewModels.

Nenhuma migração, tabela, implementação de consulta ou validação de backup foi criada nesta etapa. Os testes descrevem o comportamento que deverá existir após aprovação.

## Proposta de armazenamento

Uma nova migração deve acrescentar AgendaTasks sem reescrever as seis anteriores. Armazenar ID, título, descrição, dia planejado, horários opcionais, categoria e estado. Preservar caracteres, espaços, quebras de linha e precisão do horário.

Salvar pelo mesmo ID atualiza a tarefa e mantém as outras. Leitura ausente retorna null. Cada leitura deve refletir alterações já confirmadas em outro contexto, sem devolver fotografia desatualizada de uma entidade rastreada.

O intervalo inclui as duas datas. Ordem: dia crescente, tarefas com horário antes das sem horário, início crescente e desempate estável por título/ID. Período invertido é rejeitado. Cancelamento deve atravessar a camada sem ser convertido em sucesso. Falha de escrita em banco somente leitura retorna PersistenceFailed pelo caso de uso e preserva o original.

Os valores numéricos de categoria/estado deverão manter seu significado ao persistir. Não reordenar enums depois da migração sem estratégia explícita de compatibilidade.

## Migração e backup

Os testes partem da migração LibraryTrash para conferir evolução do banco e preservação de páginas. Um pacote anterior, construído com manifesto e hash válidos, deve ser restaurado em outra pasta, atualizado e manter as notas; a origem não pode ser alterada.

O pacote continua na versão 1, pois já transporta o banco SQLite e permite atualização por migrações na restauração. Não mudar formatos 1/2/3/4 dos cadernos para acrescentar outra entidade independente.

Backup novo deve restaurar tarefas e páginas. Conteúdo inválido da Agenda deve impedir publicação ou restauração, mesmo com hash coerente. A ausência da tabela Agenda em um backup antigo válido não é corrupção: restaurar e migrar antes de consultar.

As fixtures podem inserir dados inválidos em um banco fictício ignorando constraints, para especificar leitura e recuperação segura mesmo diante de corrupção. Essa técnica é exclusiva dos testes e não deve entrar no produto.

## Casos e resultados

`AgendaStorageTests.cs` reúne 24 casos: reabertura em três categorias/estados, atualização, ausência de horário, limites/ordem da consulta, ausência de tarefa/período vazio/invertido, três cancelamentos, banco somente leitura, três leituras corrompidas, atualização por outro contexto, migração e quatro fluxos de backup/recuperação, além da guarda de contexto.

No RED executado, 21 casos falham em métodos ainda não implementados e dois exigem uma migração posterior às seis existentes. Uma proteção estrutural passa. Várias asserções posteriores ainda não são alcançadas; isso não prova persistência, compatibilidade ou validação de backups.

Os 101 unitários e 32 testes anteriores de NotebookStorageTests/BackupStorageTests passaram. Integrações de interface não foram repetidas; a instabilidade anterior do autosave permanece registrada. Testes anteriores e contratos já aprovados ficam preservados.

## Para estudar

- Por que criar uma migração em vez de recriar o banco?
- Por que conferir hash não substitui validar os dados do banco?
- Por que um banco antigo válido é diferente de um banco corrompido?
- Como uma entidade rastreada pode esconder uma atualização feita por outro contexto?
