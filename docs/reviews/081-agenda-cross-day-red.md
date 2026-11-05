# Agenda — datas de término em RED

Data: 05/11/2026. Especificação para aprovação conforme CODEX.md, seção 9.

18 casos novos: oito falhas esperadas e dez proteções passando. Build Release sem avisos/erros e formatação aprovados. Base reexecutada: 769 aprovados, sem falhas/ignorados; os arquivos anteriores de testes permaneceram intactos por comparação de hashes.

Dois contratos mínimos opcionais permitem compilar os testes: PlannedEndDate no snapshot e CompletedAt na decisão do prompt. Não há implementação das novas regras, persistência ou controles. Falhas confirmadas: intervalos atravessando dias recusados, data planejada anterior aceita e instante manual ignorado no fechamento. A compatibilidade com horários opcionais e JSON legado está protegida.

Limites/riscos: suporte SQLite/backup à nova data ainda ausente; não expor os campos antes dessa etapa. O aviso ainda usa o horário automático. Conclusão informada não poderá estar no futuro nem antes do dia inicial; seleção múltipla deverá respeitar as regras de cada tarefa. Casos de horário exato inicial, calendário semanal e controles visuais exigirão a especificação das próximas fatias. A tela da lixeira ainda não está implementada. Sem nova publicação Windows.

Inspeção dos arquivos envolvidos não encontrou segredo literal ou logging novo. Sem perfis pessoais, Git, manual/PDF ou dependências novos. Esforço moderado, sem novos subagentes: uma rodada focada e uma da base nesta fatia. Houve correção da posição de um using antes da compilação válida; isso não foi contado como RED. Reutilizar os fixtures e executar testes focados até a implementação reduz custo. Tokens e valores não medidos.

Diff e hashes de aprovação estão nos artefatos trama-agenda-cross-day-red. Revisão humana dos testes e implementação pendentes.

[Estudo](../estudos/74-datas-de-termino-especificacao.md) · [Persistência da lixeira](080-agenda-trash-storage-green.md).
