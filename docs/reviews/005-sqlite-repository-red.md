# Repositório SQLite em revisão de testes

Data: 27 de outubro de 2026. Situação: testes preparados e executados; implementação funcional aguardando aprovação do responsável.

Registro histórico: os 14 casos foram posteriormente aprovados e implementados sem mudanças nos testes. Consulte o [relatório GREEN](006-sqlite-repository-green.md) para o estado atual.

## Achados e plano

O esquema Notes e a migração já existem. INoteRepository permite somente gravação; CreateNote é seu consumidor atual. Foi adicionado INoteReader como contrato separado de consulta, preservando consumidores e testes anteriores.

SqliteNoteRepository é apenas um esqueleto executável com construtor validando a dependência. Seus métodos lançam NotImplementedException. Nenhuma lógica de armazenamento, nova migração, alteração de schema, registro no desktop ou editor foi implementado.

Após aprovação, o plano é implementar escrita por EF Core, consulta por ID e tradução das falhas esperadas, mantendo cancelamento distinto. O adaptador deverá retornar snapshots e manter NoteRecord dentro da infraestrutura.

## Especificação dos 14 casos

| Cenário | Casos |
| --- | --- |
| Gravação confirmada por SQL independente | 1 |
| Leitura de nota inserida por SQL independente | 1 |
| Reabertura com título, corpo e texto Unicode/código preservados | 3 |
| ID inexistente retorna nulo | 1 |
| Duplicidade traduz erro e preserva a nota original | 1 |
| CreateNote com banco somente leitura retorna falha sem sucesso | 1 |
| Escrita somente leitura preserva causa interna sem expor conteúdo na mensagem pública | 1 |
| Falha de consulta retorna exceção contratada, não nulo | 1 |
| Cancelamento prévio de escrita ou leitura propaga sem gravar | 2 |
| CreateNote bem-sucedido pode ser recuperado por outro contexto | 1 |
| CreateNote inválido não grava | 1 |

Arquivo: [SqliteNoteRepositoryTests.cs](../../tests/Trama.IntegrationTests/Knowledge/SqliteNoteRepositoryTests.cs). Bancos temporários e sintéticos; migrações de produção; sem pooling e sem exclusão recursiva.

## Evidências

- Build com saída isolada: sucesso, zero avisos e zero erros.
- Formatação verificada sem alterações: sucesso.
- Projeto de integração: 15 aprovados e 13 falhas, total 28.
- Projeto unitário: 14 aprovados, sem falhas.
- Consolidado: 29 aprovados e 13 falhas em 42 casos. Os 28 anteriores continuam verdes; a validação de entrada inválida é o único caso novo já verde.
- As 13 falhas novas decorrem do esqueleto sem implementação; isso não comprova cada regra específica de banco. Não houve falhas de compilação ou descoberta nessa execução.
- Resultados: `artifacts/test-results/repository-red/`.

O build normal encontrou o executável aberto e bloqueado. A validação foi concluída com `--artifacts-path artifacts/repository-build`, sem fechar a janela do usuário. Os testes usaram essa mesma saída; não foram executados contra uma compilação antiga.

## Riscos e limites

A consulta é um contrato interno aditivo e não uma API HTTP. Nenhum contrato anterior ou teste aprovado foi alterado. A suíte completa e uma eventual CI ficarão vermelhas até a implementação aprovada. A CI remota continua não executada.

Os testes não cobrem reutilização do contexto após falha, cancelamento de comando já em execução, listagem, edição, exclusão, busca ou interface. O adaptador não será responsável por executar migrações no construtor. A mensagem pública de falha não pode incluir texto da nota; a presença da causa interna não autoriza logs de conteúdo sensível.

## Revisão e esforço

O coordenador preparou contratos, esqueleto, testes e documentação e executou as verificações. Dois subagentes revisaram o recorte e os riscos em leitura; um deles também revisou os arquivos para a apresentação. Nenhum implementou lógica de armazenamento.

Esforço qualitativo moderado, uma rodada de planejamento e uma de revisão, sem medição de tokens ou custo monetário. A próxima etapa pode reutilizar o esquema e os testes, concentrando o trabalho nos dois métodos do adaptador.

## Decisão necessária

Aprovar os testes e os contratos para implementar AddAsync e GetByIdAsync, mantendo os testes aprovados intactos. Essa pausa atende à seção 9 do CODEX e ao TDD solicitado pelo responsável. Material didático: [repositório contratos e erros](../estudos/04-repositorio-contratos-e-erros.md).
