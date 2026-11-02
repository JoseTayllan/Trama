# Esquema de notas SQLite no GREEN

Data: 27 de outubro de 2026. Situação: implementação autorizada, verificações locais aprovadas e diff pronto para revisão final.

## Implementação

- `NoteRecord` é o modelo de armazenamento interno à infraestrutura, com Id, Title e Body.
- `TramaDbContext.OnModelCreating` configura Notes, chave primária, tipos TEXT e obrigatoriedade das colunas. O identificador não é gerado pelo banco.
- O EF Core gerou `20261027211013_InitialNotes`, seu modelo de destino e o snapshot em `src/Trama.Infrastructure/Persistence/Migrations`.
- `Up` cria Notes. `Down` remove Notes e, portanto, destrói seus dados se executado; não foi aplicado a dados pessoais.

Não foram criados repositório, consulta da aplicação, editor ou inicialização automática do banco no desktop. Não há alteração dos contratos públicos existentes nem dependência nova. Nenhum teste aprovado foi alterado.

## Verificações

- Build Release: sucesso, zero avisos e zero erros.
- Testes unitários: 14 aprovados.
- Testes de integração: 14 aprovados, sendo quatro técnicos e os dez casos do esquema.
- Total: 28 testes aprovados, sem ignorados.
- Formatação dos arquivos gerados ajustada às convenções e verificada novamente.
- Resultados TRX finais em `artifacts/test-results/schema-green-final/`.

Os testes aplicam a migração em bancos temporários exclusivos, gravam SQL parametrizado e consultam por outra conexão. Agora alcançam as verificações de constraints e preservação que no RED estavam bloqueadas pela ausência da tabela. Isso não comprova a gravação por um repositório EF nem recuperação pela interface.

## Riscos e próximos limites

Não executar rollback em banco com dados pessoais sem autorização e backup recuperável. Esta migração cobre banco novo e reaplicação da mesma versão; upgrades futuros precisam de testes próprios. A política do diretório pessoal e da atualização do banco ainda não foi implementada. O workflow remoto não foi executado.

O próximo recorte é especificar e testar o adaptador de repositório, incluindo gravação, consulta e tradução de falhas. Essas regras precisam da revisão TDD correspondente antes de nova implementação.

## Revisão esforço e estudos

O coordenador implementou o modelo, gerou e revisou a migração, executou as verificações e atualizou a documentação e o material didático. Um agente revisor examinou mapping, migração e snapshot sem encontrar problema acionável. Outro foi acionado para documentação, mas sua tarefa foi encerrada sem alterações, e o coordenador assumiu o capítulo. Nenhum subagente modificou testes ou produção.

Dois subagentes acionados, uma revisão concluída; esforço qualitativo baixo a moderado. As verificações foram repetidas após ajustar a formatação gerada. Não há medição de tokens ou valor monetário. Para reduzir esforço na próxima etapa, manter atualizações documentais pequenas com o coordenador e concentrar a revisão independente no contrato do repositório.

Estudo: [SQLite migrações e durabilidade](../estudos/03-sqlite-migracoes-e-durabilidade.md). Histórico: [RED do esquema](003-sqlite-schema-red.md).
