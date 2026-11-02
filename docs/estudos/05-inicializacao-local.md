# Diretório e inicialização local

Estado: **GREEN após aprovação: 50 testes passando**, incluindo os oito da inicialização. Ainda sem ligação automática ao desktop.

## Especificação

- Diretório padrão: pasta Trama dentro de LocalApplicationData do usuário, fora do checkout no uso padrão.
- InitializeAsync recebe um diretório absoluto explícito e retorna o caminho de `trama.db`.
- Cria o diretório necessário e aplica as migrações de produção, sem EnsureCreated.
- Repetir a inicialização preserva notas existentes.
- Caminhos relativos, vazios ou em branco produzem ArgumentException.
- Cancelamento prévio não cria o diretório.
- Um arquivo bloqueando o caminho produz IOException, sem alterar esse arquivo.

Código: [LocalDatabase](../../src/Trama.Infrastructure/Persistence/LocalDatabase.cs). Testes: [LocalDatabaseTests](../../tests/Trama.IntegrationTests/Knowledge/LocalDatabaseTests.cs). Todos os testes aprovados foram preservados.

## Implementação

DefaultDirectory apenas calcula o caminho; não cria a pasta. InitializeAsync valida o diretório absoluto, verifica o cancelamento antes de qualquer escrita, normaliza o caminho e cria a pasta. Constrói uma conexão SQLite para trama.db, aplica MigrateAsync e descarta o contexto antes de retornar o caminho.

Pooling desativado evita manter conexões e arquivos abertos após a inicialização. Erros de diretório e migração propagam para o chamador; não são ocultados como sucesso. Cancelamento no meio da operação ainda pode deixar pasta ou arquivo criado: a garantia testada de ausência de criação vale para token cancelado antes de começar.

## Resultado desta etapa

Build isolado sem avisos; formatação aprovada. Suíte completa: 36 casos de integração e 14 unitários aprovados, nenhum ignorado. Os oito testes passaram sem alterações após a implementação. O RED anterior permanece no histórico de diff e nos resultados de teste.

Os testes usam diretórios temporários exclusivos, descartam conexões e removem somente arquivos conhecidos, sem exclusão recursiva. O teste do caminho padrão consulta apenas a propriedade; não inicializa dados pessoais. TRX atual: `artifacts/test-results/local-storage-green/`; RED anterior: `artifacts/test-results/local-storage-red/`.

## Para estudar

O diretório decide **onde** guardar; as migrações decidem **qual esquema** preparar. O repositório decide **como gravar e consultar**. A inicialização deve ocorrer antes do primeiro uso do repositório, separada de seu construtor.

Repetir MigrateAsync é diferente de apagar ou recriar o banco. Esses testes cobrem inicialização sequencial e cancelamento prévio, não concorrência entre processos, backups ou rollback automático de futuras migrações.

A política e a inicialização estão implementadas. Registro no desktop, tratamento visual de falhas e editor exigem seus próprios testes. O aplicativo aberto e dados pessoais não foram modificados; a CI remota não foi executada. Revisão feita pelo coordenador, sem subagentes, conforme a preferência por economia. Próxima etapa: reutilizar esses componentes ao preparar o primeiro fluxo da interface.

[Voltar ao índice](README.md)
