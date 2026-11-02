# Esquema de notas SQLite em revisão

Data: 27 de outubro de 2026. Situação: **testes RED preparados e aguardando aprovação antes da implementação**.

Registro histórico: os dez testes foram posteriormente aprovados e o esquema passou ao GREEN. Consulte o [relatório seguinte](004-sqlite-schema-green.md) para o estado atual; os resultados abaixo preservam a evidência RED.

## Recorte e plano

O próximo avanço de persistência foi dividido para permitir uma revisão concreta:

1. Aprovar os testes do esquema e da durabilidade por reabertura de conexão.
2. Implementar modelo e migração inicial de produção, mantendo os testes aprovados intactos.
3. Planejar e testar o repositório concreto, a consulta e a tradução de falhas para a aplicação.
4. Definir diretório pessoal, composição e interface nas fatias correspondentes.

Nesta entrega foram escritos somente testes e documentação. Nenhum arquivo de produção foi alterado. Não foram criados stubs, migrações, tabelas de teste manuais, banco pessoal ou implementação de repositório.

## Especificação proposta

Arquivo: [NoteSchemaTests.cs](../../tests/Trama.IntegrationTests/Knowledge/NoteSchemaTests.cs).

| Método ou cenário | Casos |
| --- | --- |
| Banco novo recebe uma migração de produção | 1 |
| `Notes` possui `Id` TEXT obrigatório e chave primária, `Title` e `Body` TEXT obrigatórios | 1 |
| Texto preservado após fechar contexto e abrir nova conexão, incluindo título/corpo isolados e Unicode/código | 3 |
| Identificador duplicado rejeitado sem substituir o original | 1 |
| Nulidade rejeitada em cada coluna obrigatória | 3 |
| Reaplicar migrações preserva o conteúdo existente | 1 |

Total: seis métodos, dez casos. Bancos temporários em disco, exclusivos por teste; conexão sem pooling; SQL parametrizado; limpeza limitada aos arquivos criados pelo teste. Não há acesso a dados pessoais.

## Resultados observados

- Build Release: sucesso, zero avisos e zero erros.
- Formatação: `dotnet format Trama.slnx --verify-no-changes --no-restore` passou.
- Projeto unitário: 14 casos anteriores aprovados.
- Projeto de integração: quatro casos técnicos anteriores aprovados e dez casos novos falhando.
- Total consolidado: 18 aprovados e 10 falhas esperadas do esquema ausente.
- TRX: `artifacts/test-results/schema-red/schema-red.trx` e `application-green.trx`.

O teste de migração encontra histórico vazio; o teste de colunas não encontra `Notes.Id`. Os demais dependem da tabela que ainda não existe. Nos casos de nulidade, o código de erro encontrado é de tabela ausente, não da restrição esperada. Portanto, constraints, preservação e reabertura ainda não foram comprovadas. Não houve erro de compilação, descoberta, acesso a temporário ou carregamento do SQLite nessa execução.

A suíte completa e uma futura execução da CI devem ficar vermelhas até o GREEN desta fatia. A CI remota não foi executada. Os testes anteriores e seus contratos permanecem intactos.

## Revisão riscos e esforço

O coordenador escreveu e executou os testes. Dois subagentes revisaram o recorte de arquitetura e os riscos de isolamento; um terceiro revisou os testes. Todos atuaram em leitura. Não houve implementação delegada.

Esforço qualitativo moderado, com uma rodada de planejamento e uma de revisão. Não há medição de tokens ou custo monetário. Para reduzir trabalho futuro, reutilizar estes testes e concentrar a implementação em mapeamento e migração, sem antecipar o editor.

O esquema é proposto para aprovação; nomes de tabela e colunas passam a fazer parte desta especificação. Repetir a mesma migração não cobre upgrades futuros, e SQL direto não cobre o adaptador de repositório. Esses limites devem permanecer explícitos na entrega GREEN.

Material didático: [SQLite migrações e durabilidade](../estudos/03-sqlite-migracoes-e-durabilidade.md).

## Aprovação necessária

Revisar os dez casos antes de implementar modelo e migração inicial. A pausa atende ao fluxo TDD solicitado pelo responsável, registrado na seção 9 do CODEX. Após a aprovação, implementar somente o suficiente para estes casos passarem sem enfraquecê-los.
