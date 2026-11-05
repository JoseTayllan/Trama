> Histórico RED: casos aprovados e implementados; suíte com 865 aprovados. Ver [fluxo implementado](088-agenda-trash-workflow-green.md). O patch de separação do RED é histórico e não deve ser usado no commit atual.

# Agenda — fluxo da lixeira em RED

Data: 05/11/2026. Especificação para revisão conforme CODEX.md, seção 9.

21 novos casos: 20 falhas esperadas e uma guarda passando. Build Release sem avisos/erros e formatação passaram. Base anterior: 844 aprovados, sem falhas/ignorados; arquivos anteriores de src/tests intactos por hashes. Nenhuma implementação, migração ou teste antigo alterado.

Fluxo proposto: registro na composição, confirmação antes de resolver rascunhos, exclusão recuperável de todos os estados, Save/Discard/Cancel somente para o editor afetado, preservação em falhas, revalidação após confirmação, bloqueio de reentrada/navegação/fechamento e restauração preservando planejamento/estado/instante real. Lista da lixeira abrange todos os dias; falha de refresh após exclusão não pode ressuscitar editor nem negar a mutação realizada.

Limites: confirmações/diálogos nativos ainda não especificados nesta fatia; nenhuma interface nova. Persistência já existe e não recebe migração nova. Falhas RED correspondem a composição/sobrecarga ausentes, sem falha de compilação. Riscos principais são perda de rascunho, reentrada, seleção antiga e confusão entre falha da mutação e do refresh.

Inspeção limitada dos testes não encontrou segredo literal ou logging novo. Perfis fictícios. Sem Git executado, publicação, dependência, banco pessoal ou manual/PDF alterados. Esforço moderado, sem novos subagentes: uma rodada focada e uma da base. Reutilizar o fluxo existente de rascunhos e separar testes nativos reduz retrabalho; tokens/custo exato não medidos. Revisão e implementação pendentes.

Responsável forneceu status e branch feat/agenda-semanal. Instruções de commit usam checkpoint-validado.patch para retirar apenas esta especificação do índice, mantendo o trabalho na pasta; não representam commit executado. Conferir aplicação com --check e revisar índice antes do commit.

[Estudo](../estudos/80-lixeira-agenda-fluxo-especificacao.md).
