# Integração semanal — implementação para revisão

Data: 06/11/2026. Implementada após aprovação explícita dos 34 casos, sem alterá-los.

Novo AgendaViewModel.Week.cs; integração em SaveCoreAsync, RefreshAsync e atualização após lixeira. DesktopComposition injeta leitor/projetor do mesmo perfil. AgendaPresentationConverter fornece símbolos de estado; nenhum XAML foi alterado.

Revisão: leitura e projeção precedem a publicação do resultado; filtros são aplicados sobre cópia dos snapshots consultados, preservando filtros e rascunho. Intervalo inválido não abre o leitor. IsBusy compartilhado bloqueia reentrada e fechamento. Mutações confirmadas continuam bem-sucedidas se a atualização semanal falhar; HasWeekError e feedback indicam o resultado antigo. Atualizar tenta releitura semanal mesmo quando a diária falha.

Construtores anteriores e consulta diária preservados. Não há acesso a SQL, disco ou serviços por localização no ViewModel. Nenhum segredo literal ou logging novo encontrado na inspeção. Perfis de testes fictícios/temporários.

Riscos e limites: navegar pelas semanas não muda o dia do formulário; criar por dia resolve alterações pendentes. Dados anteriores podem permanecer visíveis com erro até Atualizar. O cache semanal não é atualizado em segundo plano quando a semana está oculta; ShowWeekAsync consulta novamente ao voltar. Símbolos e rótulos estão preparados, mas cartões/renderização ainda não implementados. QA visual e validação de uso pertencem à próxima fatia.

Esforço baixo a moderado, sem subagentes: uma implementação, uma rodada focada e uma suíte completa. Reaproveitar editor, resolução de rascunhos e leitor reduz duplicação e uso. Tokens/custo exatos não medidos.

**Verificação final:** 34 casos aprovados intactos por hashes. Suíte completa com 982 aprovados (167 unitários e 815 integrações), zero falhas/ignorados; build Release sem avisos/erros e formatação aprovados no WSL. Todos os testes e demais arquivos anteriores de src intactos, exceto os quatro pontos de integração documentados.

Evidências e diff em trama-agenda-week-navigation-green. Revisão humana do diff pendente. Sem migração, dependências, Git, publicação ou manual/PDF.

[Estudo](../estudos/90-integracao-semanal-implementada.md) · [RED](096-agenda-week-navigation-red.md).
