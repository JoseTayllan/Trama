# 48 · Início e integração entre Cadernos e Agenda

01/11/2026. Proposta de navegação para iniciar o módulo de Agenda. Este documento descreve a direção da interface e a divisão das entregas; não registra implementação ou testes novos aprovados.

## Navegação proposta

Uma tela **Início**, nativa em Avalonia, será a entrada do aplicativo. Ela oferece acesso aos módulos e, conforme as funcionalidades forem entregues, um resumo do dia. O painel HTML continua sendo o acompanhamento do desenvolvimento do projeto.

Uma barra superior compacta mantém os acessos **Início**, **Cadernos** e **Agenda**. O módulo ativo recebe destaque e identificação por texto. Isso mantém a largura disponível para a lateral do acervo e o editor dos cadernos.

Cada módulo usa a área central e conserva sua navegação específica. A hierarquia Espaço → Caderno → Página permanece nos Cadernos. A Agenda terá seleção de semana/dia e filtros próprios. Exibir a marca uma vez na composição principal e evitar repetir cabeçalhos, busca e ações globais dentro de cada módulo.

Na primeira fatia, Início e Cadernos já podem funcionar. A Agenda passa a ser um destino funcional quando o primeiro fluxo de tarefas estiver integrado. Contadores e resumos só aparecem com consultas reais e estados vazios definidos.

## Tela Início

- Cartão Cadernos: acesso ao acervo e, posteriormente, atalho para retomar a última página.
- Cartão Agenda: acesso à semana e às tarefas do dia quando esse fluxo estiver disponível.
- Resumo do dia: data local, tarefas previstas e concluídas, derivadas do módulo de agenda.
- Páginas recentes: consulta independente do editor, sem criar novas instâncias ou gravar conteúdo.

A primeira entrega não precisa incluir todos os resumos. Priorizar a navegação funcional e a continuidade da edição antes das métricas.

## Preservar o trabalho ao alternar módulos

Alternar o módulo visível não deve fechar a página, recriar seu editor ou limpar a pilha de desfazer. Guardar apenas o ViewModel não basta: o editor nativo mantém seleção, rolagem e histórico na instância visual. O host precisa preservar também os controles já abertos, sem descartá-los a cada clique.

O autosave dos Cadernos mantém seu comportamento enquanto o usuário consulta outro módulo. Não exigir Salvar/Descartar/Cancelar apenas para esconder um editor que continuará preservado. Ao abrir outra página ou encerrar o aplicativo, manter a proteção existente para alterações pendentes.

Quando a Agenda tiver um formulário em edição, a confirmação de fechamento precisa considerar os rascunhos dos dois módulos, inclusive o que estiver oculto. Bloquear ações incompatíveis durante salvamento, restauração e diálogos que já protegem operações sensíveis.

## Integração funcional

Uma tarefa poderá ter um vínculo opcional com uma página. Exemplo: “Estudar interfaces — 19h” pode oferecer **Abrir anotações** e navegar até a página correspondente no curso de C#.

O vínculo usa o ID da página, não uma cópia de seu título ou caminho. Renomear ou mover a página não quebra a referência. Se estiver na lixeira ou indisponível, informar o estado e preservar a tarefa. Não restaurar ou excluir conteúdo automaticamente.

Abrir o vínculo passa pelo fluxo existente de navegação dos Cadernos, respeitando alterações na página que estiver aberta. Ao retornar à Agenda, manter a semana selecionada e os filtros. Agenda e Cadernos continuam utilizáveis de forma independente.

## Base técnica observada

`MainWindowViewModel.Workspace` hoje escolhe `Library` ou `Notebook`. `MainWindow` hospeda esse conteúdo num `ContentControl` e o fechamento consulta a biblioteca. Não existe navegação entre módulos implementada.

`LibraryViewModel` já concentra navegação de páginas, tratamento de alterações pendentes e recriação controlada de editores. A composição passa por `DesktopComposition`. A nova navegação deve coordenar esses componentes, preservando os fluxos existentes.

Divisão proposta:

| Parte | Responsabilidade |
| --- | --- |
| Janela principal | Barra global, módulo ativo e hospedagem das áreas preservadas |
| ViewModel da navegação | Comandos de seleção de módulo e coordenação do encerramento |
| Início | Consultas de resumo e atalhos de navegação |
| Cadernos | Acervo, páginas e edição existentes |
| Agenda | Tarefas, programação e registros de atividade |
| Application | Casos de uso e contratos próprios de cada módulo |
| Infrastructure | Persistência e consultas no mesmo perfil local |

Compartilhar perfil de dados, recursos de tema e serviços globais. A Agenda recebe suas próprias tabelas e migrações; não acrescentar campos de tarefa aos registros dos cadernos nem reescrever migrações existentes. Backup/restauração deverá incluir e validar os novos dados antes de declarar o módulo completo.

Nenhum servidor HTTP ou runtime Node/Python é necessário para essa navegação. O aplicativo continua local, em C# e Avalonia.

## Ordem de implementação

1. Especificar a navegação global: entrada Início, seleção de módulo, destaque ativo e largura compacta.
2. Escrever os testes comportamentais da primeira fatia e apresentar o RED para revisão, conforme o CODEX.
3. Após aprovação, implementar Início e integração dos Cadernos com o host, preservando a instância do editor.
4. Entregar o primeiro fluxo funcional da Agenda: criar, listar, editar e reagendar tarefas com persistência.
5. Acrescentar semana visual, categorias, registros do realizado e indicadores, por fatias testadas.
6. Integrar vínculo de página, resumos do Início e backup/restauração.
7. Conferir ciclo completo, Windows/Linux, regressões dos Cadernos e diff final antes do aceite do módulo.

Os testes da navegação precisam verificar continuidade de texto/estilos/listas, seleção/histórico e autosave; retorno ao módulo; estados vazios; fechamento com edição oculta e janela compacta. Essa lista define riscos a especificar, não uma suíte nova executada.

Commits, branches, push, Pull Requests e merges são executados pelo responsável. O agente fornece os comandos quando solicitados. Este planejamento não altera o estado do Git.
