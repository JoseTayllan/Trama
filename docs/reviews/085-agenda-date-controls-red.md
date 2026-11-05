# Agenda — campos compactos e horários por digitação em RED

Data: 05/11/2026. Especificação para revisão conforme CODEX.md, seção 9.

33 novos casos: 31 falhas esperadas e duas proteções passando. Build Release sem avisos/erros e formatação aprovados; 811 casos anteriores passaram separadamente. Arquivos anteriores de src/tests intactos por hashes. Nenhuma implementação nova nesta fatia.

Proposta: fim planejado opcional no formulário; início/fim como entradas HH:mm em vez de TimePicker; carga, gravação/limpeza, rascunhos e falhas protegidos; resumo indica término em outro dia. Diálogo mantém Agora e adiciona data/hora informadas com digitação direta; campos inválidos bloqueiam concluir, sem impedir Cancelar/Manter abertas. Compactação exercitada em janela 760x650 e diálogo 560x420, sem declarar aceite estético.

Mudança antiga proposta, ainda não aplicada: somente duas linhas de AgendaNavigationTests trocam TimePicker.SelectedTime por TextBox.Text, mantendo horários e todas as verificações. Aprovação deve abranger os casos e esse ajuste. Diffs separados e proposta textual disponíveis nos artefatos trama-agenda-date-controls-red.

Riscos: perder entrada parcial ou a data final no baseline; dessincronizar propriedades antigas de horário; limpar dados após falha; mostrar intervalo entre dias sem contexto; campos novos esconderem ações de saída em janela compacta. A implementação deve tratar esses riscos antes de revisão visual. Não há novo formato ou migração.

Revisão limitada dos testes não encontrou credencial literal nem logging novo; somente dados fictícios. Sem perfil pessoal, Git, dependências, publicação Windows ou manual/PDF alterados. Esforço moderado, sem novos subagentes: uma rodada focada e uma da base. Reutilizar os controles de texto e fixtures existentes reduz custo; tokens/custo exato não medidos. Aprovação de testes, implementação e aceite visual continuam separados.

[Estudo](../estudos/78-campos-de-datas-e-horarios-especificacao.md).
