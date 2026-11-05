# Agenda — persistência da data final em RED

Data: 05/11/2026. Especificação para aprovação conforme CODEX.md, seção 9.

24 casos novos, 22 falhas esperadas e duas proteções passando. Build Release sem avisos/erros e formatação aprovados; 787 casos anteriores reexecutados e aprovados separadamente. Arquivos anteriores de src/tests intactos por hashes. Não há código funcional ou migração nova nesta fatia.

Proposta: décima migração com PlannedEndDate opcional, sem alterar nove anteriores; roundtrip, edição/limpeza, consulta pelo dia inicial, conclusão/reabertura/lixeira e perfis isolados. Backup permanece 1, preserva ativos/excluídos e rejeita datas inválidas na criação/restauração. Legados migram somente na cópia, sem inventar data final. Conclusão revalida dados persistidos para proteger o lote.

As falhas correspondem à perda de dados no roundtrip e à ausência da coluna/migração exigida por asserções explícitas. Duas guardas já passam: rejeição de data inválida ao salvar e regra legada de mesmo dia. Sem falha de compilação ou SQL usada como RED. Riscos: mapeamento ausente pode perder data; materialização/backup devem rejeitar corrupção mesmo escondida na lixeira; adicionar controles antes do armazenamento expõe esse problema.

Revisão limitada dos testes não encontrou credencial literal ou logging novo. Somente perfis sintéticos. Sem Git, dependências, migrações antigas, publicação Windows, manual/PDF ou implementação alterados. Esforço moderado, sem novos subagentes: uma rodada focada e uma da base; reutilizar convenções de fixtures e manter a interface na próxima fatia reduz iterações. Tokens/custo exato não medidos.

Revisão humana dos testes e autorização da migração pendentes. Campos visuais, confirmação/lixeira e visão semanal não implementados nesta entrega.

[Estudo](../estudos/76-data-final-persistencia-especificacao.md).
