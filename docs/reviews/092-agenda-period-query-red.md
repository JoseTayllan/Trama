> Histórico RED: os 25 casos e contrato foram aprovados e implementados; suíte de 912 aprovados. Ver [entrega para revisão](093-agenda-period-query-green.md). A tela semanal continua pendente.

# Agenda semanal — leitura de períodos em RED

Data: 06/11/2026. Especificação para revisão conforme CODEX.md, seção 9.

25 casos novos: 24 falhas esperadas e uma guarda passando. Build Release sem avisos/erros e formatação aprovados; 887 anteriores passaram separadamente. Src/tests anteriores intactos por hashes. Somente interface mínima de leitura e novos testes; nenhuma implementação SQL/facade/registro, migração ou mudança de teste antigo.

IAgendaPeriodReader.ListOverlappingAsync retorna tarefas ativas que cruzam o intervalo inclusivo, preservando snapshot, identidade, horários opcionais e ordem existente. Sem data final vale o dia inicial; todos os estados são retornados, itens da lixeira não. ListAsync mantém filtro pelo início. Erros de faixa, cancelamento, banco ausente, dados selecionados inválidos, perfis e registro na composição estão especificados.

As falhas correspondem à ausência do contrato no facade e de seu registro. A guarda protege o filtro diário. Não interpretar esse RED como falha do SQL já existente nem criar implementação antes de aprovar os casos. Riscos: ocultar tarefas iniciadas antes da semana, duplicar identidades, modificar contratos atuais ou tratar pendência como planejamento infinito.

Aceite do responsável em 06/11: lixeira e acesso à exclusão funcionam como esperado; ambiente não informado e sem inferir revisão formal do diff. Proposta semanal de segunda a domingo, categorias/estados e cartões por datas será detalhada nas próximas fatias. Esta entrega ainda não tem tela semanal.

Inspeção limitada dos arquivos novos não encontrou segredo literal ou logging. Perfis fictícios. Sem Git, perfil pessoal, dependência, migração, publicação ou manual/PDF alterados. Esforço moderado, sem subagentes: uma rodada focada e uma da base. Reutilizar snapshots, campos e fachada existentes reduz retrabalho; tokens/custo exato não medidos. Aprovação dos testes e implementação pendentes.

[Estudo](../estudos/85-semana-consulta-por-sobreposicao.md).
