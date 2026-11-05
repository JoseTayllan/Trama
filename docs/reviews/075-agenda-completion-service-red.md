# Serviço de encerramento — RED

**Atualização de 04/11/2026:** os 24 casos foram aprovados e implementados, preservados. Suíte completa com 703 aprovados; build/formatação passaram. [Entrega GREEN](076-agenda-completion-service-green.md). O texto abaixo preserva o histórico RED; diálogo ao sair ainda pendente.

**Base anterior reexecutada:** 679 aprovados (117 unitários e 562 integrações), zero falhas/ignorados. Os 24 novos casos continuam em RED separado; não declarar a suíte completa GREEN.

Data: 04/11/2026. Aguardando aprovação dos 24 casos.

Contratos novos IAgendaCompletionActions e SqliteAgendaCompletionActions, com validação de caminho e operações vazias compiláveis. Nenhuma consulta, gravação ou composição implementada. Oitava migração e repositórios anteriores intactos; sem alteração de tela/fechamento.

24 descobertos: 20 falhas esperadas, quatro proteções passando e zero ignorados. Dezenove falhas são operações não implementadas; uma verifica o serviço ausente na composição. Build Release sem avisos/erros e formatação passaram. Asserções posteriores à entrada vazia ainda não alcançadas; não declarar contexto por operação, concorrência ou integração funcionais antes do GREEN.

Escopo proposto: candidatas Pendentes/Em andamento de qualquer dia até o limite inclusivo, ordem existente, conclusão via caso de uso aprovado, contexto por operação e perfil isolado. Validar/cancelar antes de I/O, não criar banco vazio e refletir alterações externas. Revalidar seleção desatualizada no adaptador transacional. A consulta não decide encerramento nem usa o relógio sozinha.

Riscos para o GREEN: manter snapshots/entidades pendentes após falha, compartilhar contexto no singleton, inicializar perfil por engano e confundir último encerramento histórico com estado atual. O diálogo e a coordenação entre módulos serão testados em outra fatia.

Sem teste antigo, migração, formato, dependência, log novo, segredo literal, dado pessoal, Git ou publicação alterados. Manual/PDF e README preservados. Revisão de segredos limitada aos arquivos envolvidos.

Esforço moderado, sem novos subagentes: duas execuções RED, sendo a segunda após fortalecer o cenário de concorrência com tarefas em threads separadas, e uma rodada da base anterior. Reutilizar os adaptadores e os cenários de armazenamento aprovados reduz repetição. Tokens/custo exato não foram medidos.

[Estudo](../estudos/68-servico-de-encerramento-especificacao.md).
