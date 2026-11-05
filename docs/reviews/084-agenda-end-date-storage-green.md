# Agenda — data final persistida

Data: 05/11/2026. Para revisão do diff.

Após aprovação dos 24 casos e da migração, PlannedEndDate foi mapeado e persistido. Décima migração 20261105224911 adiciona somente coluna opcional; nove anteriores intactas. Leitor recompõe e valida a data antes de entregar snapshots. Conclusão, lixeira e backup reutilizam a validação existente; pacote de backup permanece 1. Tarefas antigas não recebem datas inventadas.

Verificação WSL: 24 focados aprovados intactos; suíte completa **811 aprovados (131 unitários, 680 integrações)**, zero falhas/ignorados. Build Release sem avisos/erros e formatação passaram. Testes anteriores e migrações históricas conferidos contra a base. Cobertura inclui legado, dados corrompidos com hashes válidos, exclusão/restauração, conclusão atômica, cancelamento, rollback e isolamento.

Revisão do código: alteração restrita ao registro, mapeamento, gravação/leitura e arquivos gerados da migração. Datas antigas null mantêm regra anterior; limpeza persiste null. Validação do backup não usa filtro que esconda excluídos. Não há alteração do filtro por dia inicial, das regras de conclusão ou de LastCompletedAt nesta fatia.

Limites/riscos: reverter a migração perde datas explícitas; nenhum downgrade aplicado. Formulário e diálogo ainda sem novos campos, portanto não apresentar a funcionalidade visual como concluída. Semana visual e confirmação/tela da lixeira pendentes. Sem nova publicação/execução Windows ou teste em perfil pessoal. Inspeção limitada do diff não encontrou segredo literal ou logging novo; sem dependências, Git ou manual/PDF alterados.

Esforço moderado, sem novos subagentes: uma rodada focada e uma completa. Geração EF teve chamada interrompida antes de produzir arquivos; estado conferido antes de repetir. Um patch de normalização foi rejeitado pela ferramenta e reaplicado sem alterar a lógica. Reutilizar fixtures e separar os campos visuais reduz retrabalho. Tokens/custo exato não medidos. Revisão humana do diff pendente.

[Estudo](../estudos/77-data-final-persistida.md) · [RED](083-agenda-end-date-storage-red.md).
