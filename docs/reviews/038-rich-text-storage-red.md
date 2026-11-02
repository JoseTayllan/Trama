# Persistência da formatação: RED para revisão

30/10/2026. Continuação autorizada para preparar a próxima fatia. Novo arquivo: [RichTextVersioningTests.cs](../../tests/Trama.IntegrationTests/Knowledge/RichTextVersioningTests.cs). Código de produção, contratos e os testes anteriores permanecem intactos; manual/PDF sem alterações.

## Comportamento especificado

Os 21 casos cobrem gravação/reabertura/atualização de páginas enriquecidas como versão 3; remoção de atributos com retorno à versão de conteúdo correspondente; formatos 1/2 válidos sem promoção; valores inválidos sem sobrescrita; versão futura recusada; backup/restauração com texto/código/imagem; restauração independente do escritor; rejeição de atributos novos indevidamente rotulados como 1/2, com limpeza e preservação da origem. Ver [especificação completa](../estudos/28-persistencia-de-atributos-especificacao.md).

Páginas e pacote de backup têm versões distintas. A proposta mantém o pacote como formato 1 e atribui 3 às páginas com tamanho, cor ou marca-texto. Os testes usam SQLite real, conexões novas e diretórios temporários exclusivos; o pacote independente usa snapshot SQLite e hashes válidos.

## Execução

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

Resultado final:

| Grupo | Passaram | Falharam | Total |
| --- | --- | --- | --- |
| Unitários anteriores | 77 | 0 | 77 |
| Integração anterior | 276 | 0 | 276 |
| Novos de persistência | 8 | 13 | 21 |
| Total | 361 | 13 | 374 |

Restore com lock aprovado; build Release com zero avisos/erros; formatação aprovada. Nenhum caso ignorado. O retorno 1 do comando é esperado pela especificação em RED, sem falha dos 353 anteriores.

Falhas esperadas: o escritor declara 2 em vez de 3; leitura/atualização de páginas 3 são recusadas; backup/restauração recusam 3; conteúdo enriquecido indevidamente rotulado como 1/2 é aceito ou ignorado. As oito proteções que passam cobrem formatos antigos válidos, rejeição de valores inválidos antes da gravação e bloqueio de versões futuras. Não houve erro de compilação, infraestrutura ou SQL na especificação final.

Houve uma execução inicial com 18 casos e uma final com 21 após incluir três casos adicionais para conteúdo rotulado como versão 1, além da versão 2. A segunda execução verifica a mudança da especificação, sem repetição posterior sem motivo.

## Revisão e sequência

Conferir o diff externo `trama-rich-storage-red/testes-e-plano.diff`, os TRXs `unit.trx`/`integration.trx` e a auditoria `audit.json` nos artefatos desta sessão. Os 21 casos aguardam aprovação explícita conforme seção 9 do CODEX; não implementar antes da revisão. Após aprovação, coordenar repositório e validação de backup, mantendo os testes congelados.

A implementação futura deve preservar também bancos anteriores sem a tabela de estilos, conforme a cobertura já existente. Não editar migrações antigas para contornar compatibilidade. Os novos controles e a identidade visual continuam fora desta fatia, preservando Espaço → Caderno → Páginas como requisito.

Esforço moderado: um agente, sem subagentes, duas execuções completas devido à ampliação dos casos; sem medição de tokens ou custo. Na implementação, executar primeiro a classe afetada e repetir a suíte completa na validação final. Não houve acesso ao banco pessoal nem alteração de segurança.
