# Agenda semanal — consulta de períodos implementada

Data: 06/11/2026. Para revisão do diff; modelo e interface semanais pendentes.

Após aprovação dos 25 casos/contrato, repositório e facade implementam IAgendaPeriodReader. Predicado de datas inclusivo, fim ausente vale início, filtro explícito da lixeira e ordenação existente. Composição expõe a mesma instância no mesmo perfil. ListAsync diário permanece intacto.

25 casos aprovados e contrato preservados por hashes. WSL: **912 aprovados (131 unitários e 781 integrações)**, sem falhas/ignorados; build Release sem avisos/erros e formatação aprovados. Outros testes, persistência/mapeamento, migrações e locks intactos. Perfis fictícios. Verificados limites, faixas externas, opcionais, unicidade/ordem, estados, lixeira, erros/cancelamento, corrupção selecionada, isolamento e composição.

Revisão do código: retorno AsNoTracking e contexto por operação, sem consultas agregadas ou joins que dupliquem IDs. Faixa invertida validada antes de I/O, cancelamento propagado e materialização validada pelo leitor compartilhado. Não há gravação, inferência de planejamento por status ou mudança no filtro diário. Inspeção limitada não encontrou segredo literal ou logging novo.

Protótipo solicitado gerado pela habilidade imagegen/ferramenta nativa e salvo em docs/prototipos/agenda-semanal-v1.png. Sete dias, continuidade da semana anterior, cores, filtros e editor lateral como proposta conceitual, sem alteração da interface em produção. Inspeção da imagem conferiu estrutura, datas e ações. Aceite visual dessa proposta permanece separado dos testes.

Limites: leitor trabalha com datas, não com duração por hora; o recorte visual por dia será outra especificação. Interface e filtros ainda não implementados. Sem nova publicação/execução Windows ou execução remota do CI nesta fatia. Nenhuma migração, dependência, perfil pessoal, Git ou manual/PDF alterados.

Esforço moderado, sem subagentes: uma rodada focada, uma suíte completa e uma imagem conceitual. Reutilizar facade, snapshots, validação e paleta reduz retrabalho na projeção semanal. Tokens/custo exato não medidos. Revisão humana do diff e aceite visual pendentes.

[Estudo](../estudos/86-semana-consulta-implementada-e-prototipo.md) · [Imagem](../prototipos/agenda-semanal-v1.png) · [RED](092-agenda-period-query-red.md).
