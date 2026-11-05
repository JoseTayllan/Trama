# 60 — Agenda: formulário e navegação implementados

Data: 03/11/2026. Implementação disponível para revisão do diff e validação de uso.

## Fluxo disponível

A Agenda tem lista por dia e formulário para criar, editar e reagendar tarefas. Título, descrição, data local, horários opcionais, categoria e estado usam o serviço SQLite existente. Concluir e reabrir são mudanças do campo de estado. Salvar mantém o formulário aberto e a identidade da tarefa. Reagendar não muda o filtro do dia: a tarefa pode sair da lista atual.

`AgendaViewModel` coordena os comandos e os rascunhos; `AgendaWorkspaceView` apresenta lista e formulário; `AgendaPresentationConverter` traduz categorias, estados e horários. `DesktopComposition` registra o ViewModel e `App` conecta o serviço ao perfil inicializado e à confirmação contextual da Agenda.

## Por que preservar o rascunho

O formulário compara seus campos com um snapshot da última gravação. Alternar módulos mantém os controles e os rascunhos. Nova tarefa, mudança de dia e abertura de outra tarefa resolvem alterações com Salvar, Descartar ou Cancelar. Falhas de consulta preservam o estado anterior.

Salvar captura uma revisão antes de aguardar o serviço. Edições posteriores continuam pendentes. O ID confirmado é mantido antes de atualizar a lista: se essa leitura falhar, salvar novamente atualiza a mesma tarefa. A interface informa falhas de consulta sem apresentar a lista vazia como confirmação de ausência de tarefas.

O fechamento consulta primeiro a Agenda e depois os Cadernos. Confirmar descarte não apaga imediatamente os campos: uma decisão posterior pode cancelar o fechamento. Gravação em andamento bloqueia reentrada. A mesma instância do editor dos Cadernos permanece viva para preservar seleção, histórico e autosave quando o módulo está oculto.

## Interface e limites

Lista e formulário têm rolagem própria. Em janela compacta, título e ações principais continuam acessíveis; os campos inferiores ficam na rolagem do formulário. Cores de categoria usam recursos semânticos dinâmicos da paleta. A data escolhida é local, sem conversão para UTC.

Não houve migração, dependência ou alteração de formato nesta fatia. Agenda semanal, recorrência, horas realizadas, vínculos com páginas e resumos reais do Início continuam pendentes. Não existe controle de conflito entre edições simultâneas da mesma tarefa.

## Evidências

Os 34 casos aprovados de formulário/navegação permaneceram intactos. A suíte completa final passou com **632 testes: 101 unitários e 531 integrações**, sem falhas ou ignorados. Build Release sem avisos/erros e verificação de formatação passaram no WSL.

Dois helpers antigos receberam somente a troca aprovada de `DateTime.UtcNow` para `Stopwatch`. Limites de 10 e 5 segundos e todas as asserções foram preservados. Houve discrepância entre durações e horários nos registros anteriores; um cronômetro monotônico evita depender de ajustes do relógio. A rodada final passou, sem alegar uma causa do sistema operacional comprovada ou alterar suas configurações.

QA com SQLite e renderização Skia em perfil fictício conferiu criação, edição, conclusão, reagendamento, troca de módulos, autosave oculto, paleta e janela compacta. O pacote Windows foi publicado separadamente com perfil fictício; a validação interativa desse pacote cabe ao responsável.

## Para estudar

1. Compare o snapshot de gravação com a revisão atual do formulário. Por que concluir o serviço não basta para marcar toda edição como salva?
2. Simule uma falha ao reler a lista depois do commit. Como a identidade impede duplicação?
3. Explique por que descartar um rascunho durante o fechamento não deve destruí-lo antes das outras decisões.

[Especificação aprovada](59-agenda-formulario-especificacao.md) · [Relatório de revisão](../reviews/068-agenda-form-green.md).
