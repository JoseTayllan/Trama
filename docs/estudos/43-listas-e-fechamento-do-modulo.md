# Listas e fechamento da primeira versão dos cadernos

31/10/2026. O responsável validou o uso da lixeira e pediu opções de lista na formatação. A implementação da lixeira tem 454 testes aprovados; a revisão do diff não é presumida apenas pela validação de uso. Listas ainda não foram implementadas nesta entrega.

## Próxima fatia: marcadores e numeração

Proposta inicial para os testes:

- Listas com marcadores e numeradas, acessíveis por um único controle Listas na barra de formatação recolhível.
- Aplicação ao parágrafo do cursor ou aos parágrafos abrangidos por uma seleção, sem alterar código/imagens de outros blocos.
- Converter entre lista com marcadores, numerada e texto comum, preservando o conteúdo e os estilos existentes.
- Enter continua a lista; Enter num item vazio encerra a lista. A numeração deve se ajustar ao inserir/remover itens.
- Desfazer/refazer inclui a mudança de lista; salvar, autosave, reabertura, lixeira e backup preservam a estrutura.
- Texto longo deve quebrar com alinhamento apropriado do conteúdo, sem invadir a margem dos blocos nem criar rolagem horizontal na prosa.

Checklist com caixas, sublistas/recuos e opções avançadas de numeração são evoluções sugeridas para uma fatia posterior, não requisitos já implementados. Não ampliar automaticamente esta primeira entrega com todos os comportamentos de um processador de texto.

## Modelo e compatibilidade a definir antes de implementar

`NotebookTextBlock` guarda texto e intervalos de estilo; `InlineTextDocument` acompanha as edições e `NotebookProseEditor` usa AvaloniaEdit com histórico e transformação visual. Não existe atualmente um modelo de formatação de listas/parágrafos nesses componentes.

A lista deve ter uma representação que permita editar, renumerar e desfazer com consistência, em vez de apenas pintar um símbolo fora do conteúdo e perder essa informação ao salvar. A escolha de metadados, identidade dos itens, atualização de offsets e serialização precisa vir acompanhada de testes. Não fixar nova versão de página, tabela ou migração antes dessa análise; as versões atuais 1/2/3 devem continuar legíveis.

A formatação por caracteres precisa continuar funcionando dentro dos itens. A aplicação de lista não deve apagar negrito, cores ou marca-texto, nem recriar o editor ao salvar. A expansão da barra e os limites da janela compacta continuam parte da conferência visual.

## Critério proposto de fechamento

1. Concluir a fatia inicial de listas com testes revisados e implementação validada.
2. Acrescentar exportação básica em formato acessível, sugerindo Markdown com imagens como primeiro alvo. PDF pode ser uma etapa posterior; esta é uma proposta, não uma exportação já autorizada ou implementada.
3. Revisar o ciclo integrado em Windows/Linux: criar, editar, formatar, salvar, reabrir, buscar, mover, excluir/restaurar, recuperar backup e exportar o conteúdo incluído nessa versão.

Modelos de estudo, relações entre páginas, OCR, busca semântica e assistente continuam expansões. O fechamento da primeira versão dos cadernos não equivale a concluir todo o escopo futuro da organização básica.

## Sequência de trabalho

Definir o comportamento/modelo por fatias; escrever e executar testes comportamentais; revisar os casos conforme a seção 9 do CODEX; implementar somente após a aprovação correspondente; conferir persistência/histórico/backup e renderização; apresentar diff para revisão final. Não declarar suporte a listas apenas com um menu sem edição e persistência completas.
