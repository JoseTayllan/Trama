# 47 · Exportação Markdown e painel do projeto

01/11/2026. Os 26 casos e os limites do formato foram aprovados antes da implementação. Estado: exportação implementada para revisão do diff e validação de uso. O painel HTML foi solicitado na mesma etapa. Manual/PDF permanecem intactos.

Atualização de aceite: o responsável validou o uso da exportação e do painel HTML em 01/11/2026. A revisão explícita do diff e o aceite final do módulo permanecem separados. As verificações abaixo são as da implementação; não houve nova execução da suíte nesta atualização documental.

Confirmação posterior: o uso também foi validado pelo responsável nos dois ambientes, Windows e Linux/WSL. A única pendência para o encerramento formal passou a ser a revisão explícita do diff; as referências posteriores a validação Windows pendente descrevem o momento da entrega inicial.

## Fluxo da exportação

O botão **Exportar página**, nas ações da página aberta, cria um snapshot do rascunho. Isso inclui alterações ainda não salvas, mas não executa Salvar nem limpa o histórico. Uma página nova também pode ser exportada. O diálogo orienta a escolher uma pasta nova e informa os limites do Markdown.

O `NotebookEditorViewModel` recebe `IMarkdownPageExporter` por injeção. Seus construtores antigos continuam disponíveis; a composição real também passa o exportador para os editores criados pela biblioteca, inclusive depois de navegar ou mover páginas. Os construtores antigos, sem exportador, não anunciam um recurso não configurado.

As camadas ficam separadas:

| Componente | Responsabilidade |
| --- | --- |
| `MarkdownPageFormatter` — Application | Transformar snapshot, estilos, listas, código e referências de imagem em Markdown |
| `IMarkdownPageExporter` — Application | Contrato assíncrono da operação com cancelamento e abertura de imagens |
| `LocalMarkdownPageExporter` — Infrastructure | Ler streams, escrever arquivos, preparar a saída e publicar a pasta |
| `MarkdownExportViewModel` — Desktop | Destino, feedback, estado ocupado e tradução de falhas esperadas |
| Diálogo e code-behind — Desktop | Escolha nativa de pasta, confirmação, cancelamento e fechamento |

O diálogo pausa o autosave enquanto está aberto e retoma o comportamento anterior ao fechar. A exportação não força uma gravação; um autosave já devido pode continuar normalmente depois do diálogo. O snapshot exportado não muda durante a operação.

## Conteúdo portátil

O resultado tem `pagina.md` em UTF-8 sem BOM e uma pasta `imagens/` somente quando houver anexos. Links são relativos e nomes vêm da identidade das imagens, não de títulos/legendas. PNG/JPEG mantêm os bytes originais; referências repetidas reutilizam o arquivo.

O texto preserva a ordem dos blocos. Negrito, itálico e combinação viram delimitadores Markdown; atributos de cor/tamanho não quebram uma sequência com a mesma ênfase. Listas são derivadas dos metadados dos parágrafos. Código usa cerca maior que qualquer sequência de crases em seu conteúdo; rótulos inseguros viram `text`. Texto, título e legenda literais recebem escape, sem transformar HTML digitado em marcação ativa.

**Limites aprovados:** a saída é uma página por operação; cores, tamanho e marca-texto específicos não são exportados como CSS/HTML. Tags/contexto da biblioteca, exportação em lote e PDF continuam futuros. A exportação é portátil, mas não é um round-trip para importar de volta. Para recuperação fiel, usar o backup restaurável existente.

## Arquivos e cancelamento

O destino precisa ser absoluto, novo e ter um diretório pai existente. Um staging com nome próprio é criado ao lado do destino. Imagens e Markdown são escritos ali; somente depois a pasta é movida para o destino final. Uma pasta/arquivo que apareça no destino durante a operação não é sobrescrito.

Falha/cancelamento descarta os streams e remove o staging pertencente à operação. O fechamento do diálogo aguarda esse encerramento em vez de abandonar uma cópia no meio. A limpeza ainda depende das permissões e da disponibilidade do sistema de arquivos; não é uma transação capaz de sobreviver a qualquer interrupção do processo ou do computador.

Não houve migração, dependência nova nem alteração das versões de conteúdo/backup nesta entrega. A compatibilidade anterior permanece: páginas 1/2/3/4; pacote de backup 1.

## Painel HTML do roadmap

Abra [roadmap.html](../../roadmap.html) diretamente no navegador. O painel funciona offline e apresenta marcos, critérios de fechamento, recursos existentes, planos futuros e pendências técnicas. Filtros de status, busca sem distinguir acentos, tema claro/escuro e anotações pessoais facilitam o acompanhamento.

A fonte oficial continua [rodmap.md](../../rodmap.md). O gerador padrão da biblioteca Python produz os dados incorporados ao HTML, sem fetch, servidor ou pacotes externos:

```powershell
python tools/build_roadmap.py
```

`tools/roadmap.template.html` concentra layout/interação e `tools/build_roadmap.py` lê as tabelas e checklists do Markdown. Não editar o HTML gerado manualmente: a próxima geração substituirá essas mudanças. Regenerar o painel sempre que atualizar o roadmap.

Anotações e preferência de tema ficam apenas no armazenamento deste navegador. Não alteram status oficiais, Markdown ou cadernos e não são sincronizadas entre computadores. Se o navegador bloquear esse armazenamento, o painel informa que as anotações não foram persistidas. Os números do painel são uma fotografia da última geração, não monitoramento automático. O progresso conta critérios desta versão, sem declarar porcentagem de todo o projeto.

## Evidências e próximos passos

**512 testes aprovados:** 77 unitários e 435 integrações, incluindo os 26 novos intactos. Build Release e verificação de formatação passaram. O roteiro SQLite/Skia conferiu rascunho/histórico, exportação, salvar, busca por tag em caixa diferente, movimentação, lixeira/restauração, backup/restauração e exportação do perfil recuperado, com capturas ampla/compacta.

O painel foi verificado em navegador headless, offline, em 1440/900/390 pixels, com filtros, busca, estado vazio, tema e persistência de anotações fictícias. Essa conferência não equivale a auditoria completa de acessibilidade ou a aprovação estética do responsável.

Detalhes e riscos: [relatório 055](../reviews/055-markdown-export-green.md). Para encerrar a primeira versão ainda faltam revisão explícita do diff, validação do uso da exportação no Windows e aceite integrado. O roadmap mantém esses itens abertos.

## Para estudar

1. Por que gerar o painel de uma fonte única evita divergência de status?
2. Por que um snapshot do rascunho é mais seguro do que ler a página novamente no meio da exportação?
3. Como o staging evita apresentar um resultado incompleto e quais falhas ele não resolve?
4. Por que o exportador recebe um leitor de imagens em vez de depender do ViewModel ou de um contexto SQLite?
