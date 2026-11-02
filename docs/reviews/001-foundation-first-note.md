# Fundação e especificação da primeira nota

Data: 27 de outubro de 2026. Situação: **pronto para revisão humana, sem implementação funcional da nota**.

Este é o registro histórico da etapa RED. Os testes foram posteriormente aprovados e o caso de uso passou ao GREEN. Consulte a [entrega seguinte](002-first-note-green.md) para o estado atual; os resultados abaixo preservam a evidência anterior.

## Escopo realizado

Criada a solução `Trama.slnx`, com Domain, Application, Infrastructure e Desktop, além de projetos unitários e de integração. O desktop abre uma janela com a identidade inicial e mensagem de fundação. Não existem editor funcional, gravação de notas, API HTTP, assistente, cofre ou banco pessoal.

SDK 10.0.401 e pacotes centralizados; nullable, analisadores e tratamento de advertências como erros. Foram adicionados formatação, locks NuGet, ferramenta EF local, feed explícito e configuração de CI Windows. Nenhum repositório Git foi inicializado.

## Especificação para revisão

Os testes estão em `tests/Trama.UnitTests/Knowledge/CreateNoteTests.cs`. A operação usa um repositório controlado de teste; não há persistência SQLite de notas nesta fatia.

| Cenário | Resultado esperado após implementação | Casos |
| --- | --- | --- |
| Apenas título | Criar nota com ID não vazio e uma gravação | 1 |
| Apenas corpo | Criar nota com ID não vazio e uma gravação | 1 |
| Título e corpo | Criar sem exigir projeto ou tags | 1 |
| Texto com espaços e código | Preservar exatamente espaços e quebras de linha | 1 |
| Campos vazios ou nulos | Falha de validação, nenhuma nota e nenhuma gravação | 4 |
| Campos apenas com whitespace | Falha de validação sem gravação | 5 |
| Erro de armazenamento | Falha de persistência, sem retornar criação bem-sucedida | 1 |

Total: 14 casos em sete métodos. Título ou corpo precisa conter texto. Os campos nulos são aceitos no contrato de entrada para a validação, mas sucesso com um campo nulo ainda não tem cenário específico nesta revisão. Não antecipar regras adicionais sem atualizar a especificação.

`CreateNote` contém somente o esqueleto executável necessário aos testes e lança `NotImplementedException`. Não está registrado nem conectado à interface. Nenhuma regra de criação foi implementada.

## Evidências observadas

| Verificação | Resultado |
| --- | --- |
| Restauração NuGet | Pacotes restaurados; bundle SQLite atualizado sem suprimir auditoria |
| `dotnet build Trama.slnx --configuration Release --no-restore` | Sucesso, zero avisos e zero erros |
| `dotnet format Trama.slnx --verify-no-changes --no-restore` | Sucesso |
| Testes de integração da fundação | 4 aprovados |
| Testes unitários da primeira nota | 14 executados e 14 falhas por `NotImplementedException` |
| Janela nativa | Processo `Trama.Desktop` com título Trama e respondendo |
| `dotnet ef migrations list ... --no-connect` | Nenhuma migração; contexto identificado sem criar banco |

Os quatro testes técnicos verificam janela headless e recurso da logo, leitura/escrita no SQLite real em memória e rejeição de caminho de banco relativo ou ausente. A abertura nativa também exercitou a composição de dependências. Não foi feita validação visual detalhada de acessibilidade ou layout.

**Limite do RED:** as falhas comprovam que a operação está ausente, não que cada regra específica já foi exercitada e rejeitada individualmente. Não houve falhas de compilação, descoberta ou dependência na execução consolidada. As asserções de conteúdo, validação e persistência serão alcançadas no GREEN após aprovação. Este estado não é uma funcionalidade pronta nem uma suíte aprovada.

Resultados locais em `artifacts/test-results/`: `foundation.trx` registra os testes técnicos; `first-note-red.trx` registra os casos da nota. O comando completo retornou código 1, conforme o estado acima. A CI remota não foi executada e também deve falhar enquanto a especificação permanecer RED.

## Segurança e dependências

A restauração inicial detectou GHSA-2m69-gcr7-jv3q na biblioteca nativa transitiva SQLitePCLRaw.lib.e_sqlite3 2.1.11. Foi adicionada referência explícita ao bundle 3.0.5 e o provedor foi novamente validado, mantendo a auditoria ativa. Referência: https://github.com/advisories/GHSA-2m69-gcr7-jv3q.

Os pacotes e licenças declaradas podem ser consultados nas páginas oficiais: https://www.nuget.org/packages/Avalonia.Desktop/11.3.19, https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.0, https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.9 e https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/3.0.5. Os manifests restaurados declaram MIT para Avalonia, EF Core e CommunityToolkit; xUnit 2.9.3 e SQLitePCLRaw declaram Apache-2.0. A distribuição futura deve conservar avisos e revisar também as licenças transitivas.

Não há chaves de integração necessárias para esta entrega. Não foram adicionados logs de conteúdo de notas. O futuro diretório dos dados pessoais e a proteção do cofre permanecem pendentes das etapas correspondentes.

## Responsabilidades e esforço

- Coordenador e implementador: criou infraestrutura e shell, fixou dependências, executou checks e preparou o diff.
- Agente de arquitetura e documentação: analisou dependências e criou o README.
- Agente de testes: escreveu os 14 casos da nota, sem alterar produção nem executar implementação.
- Agente revisor: revisou segurança e limites da fundação; apontou cobertura da composição e risco de apresentar CI verde. A abertura nativa verificou a composição; o estado RED foi documentado.

Foram usados três subagentes, uma rodada de exploração e uma rodada de autoria/revisão. As verificações foram repetidas para corrigir a dependência SQLite e a ordem dos imports, e depois para incluir os testes unitários. Esforço qualitativo moderado; não há medição de tokens ou custo monetário disponível. Na próxima etapa, reutilizar os contratos e o mapa de arquitetura e limitar o trabalho ao caso de uso da nota.

## Decisão necessária

Revisar os testes e os contratos da primeira nota. A aprovação permite implementar somente o caso de uso com o repositório definido pelo contrato, mantendo os testes aprovados intactos. Editor, persistência real, migrações e reabertura de notas continuam em entregas posteriores. A pausa de implementação decorre do fluxo TDD solicitado pelo responsável e registrado na seção 9 do CODEX.
