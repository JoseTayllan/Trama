> Histórico RED: casos aprovados e implementados, com 887 testes aprovados. Ver [entrega para revisão](091-agenda-trash-ui-green.md). Aceite visual pendente.

# Agenda — botões e diálogos da lixeira em RED

Data: 06/11/2026. Especificação para revisão conforme CODEX.md, seção 9.

22 novos casos: 21 falhas esperadas e uma guarda passando. Build Release sem avisos/erros e formatação aprovados. Base anterior: 865 aprovados (131 unitários e 734 integrações), sem falhas/ignorados. Src/tests anteriores intactos por hashes; nenhuma implementação, migração ou teste antigo alterado.

Controles propostos: excluir tarefa aberta e abrir lixeira da Agenda. Confirmação com título, recuperação e Cancelar como padrão/X seguro; decisões de rascunho pelo diálogo existente; bloqueio de segunda confirmação. Listagem de todos os dias, metadados, seleção inicialmente vazia, restauração escolhida e rascunho independente preservado. Feedback/Atualizar após falha de carga, nova tentativa após falha de restauração, proteção ao fechar enquanto ocupado e ações visíveis em janela compacta.

Falhas RED correspondem aos botões/diálogos ainda ausentes. O caso legado sem lixeira já passa. A primeira compilação acusou xUnit2031 e foi corrigida na nova especificação antes da execução válida; não é evidência de falha comportamental. Não houve enfraquecimento de testes antigos.

Riscos: confirmação duplicada, descarte indevido do rascunho, falsa indicação de lixeira vazia quando a consulta falha, seleção obsoleta, tentativa repetida de restauração e janela fechada durante gravação. Reutilizar o fluxo validado e diferenciar estados vazio/erro/carregando. A implementação visual precisa de conferência real além dos testes headless.

CI: responsável confirmou sucesso da correção NU1004 em 06/11. Execução remota não consultada pelo agente; isso não certifica testes nativos neste Windows pessoal. Sem nova publicação, perfil pessoal, Git, dependências ou manual/PDF. Inspeção limitada dos novos testes não encontrou credencial literal ou logging novo.

Esforço moderado, sem novos subagentes: uma rodada focada e uma da base, além do ajuste de compilação dos testes. Reutilizar diálogos, paleta e fluxo existentes reduz iterações na implementação. Tokens/custo exato não medidos. Aprovação dos testes e implementação pendentes; sem exclusão permanente.

[Estudo](../estudos/83-lixeira-agenda-interface-especificacao.md).
