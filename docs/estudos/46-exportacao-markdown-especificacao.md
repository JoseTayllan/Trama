# 46 · Exportação da página em Markdown: especificação

01/11/2026. Listas validadas no uso pelo responsável. Próxima fatia para fechar a primeira versão dos cadernos: exportar uma página em formato acessível, com imagens. Estado deste capítulo: **testes para revisão; exportação ainda não implementada**.

Atualização posterior: os 26 casos e os limites foram aprovados e implementados; consulte o [capítulo 47](47-exportacao-markdown-e-painel.md). Este documento preserva a especificação e a fase RED.

## Escopo proposto

Exportar a **página aberta**, incluindo o rascunho ainda não salvo, sem salvar no banco, alterar a página ou limpar seu histórico. Uma página nova também pode ser exportada; título vazio resulta no cabeçalho “Sem título”.

O destino é uma pasta absoluta nova, escolhida pelo usuário. A saída contém `pagina.md` em UTF-8 e, quando necessário, `imagens/` com os anexos. Nomes de títulos/legendas nunca se tornam caminhos. Não substituir uma pasta existente. Cancelar antes ou durante a operação não deve deixar uma exportação parcial publicada.

O botão de exportação pertence à página e abre um diálogo com destino, confirmação, cancelamento e feedback. O seletor nativo de pasta pode complementar o campo de caminho; não é necessário para executar os testes automatizados. Manter o fluxo utilizável em janela compacta.

## Portabilidade do conteúdo

| Conteúdo do Trama | Markdown proposto |
| --- | --- |
| Título | Cabeçalho de nível 1, sem permitir novas linhas virarem outros cabeçalhos |
| Texto | Ordem dos blocos preservada; caracteres literais de Markdown/HTML escapados |
| Negrito/itálico | `**negrito**`, `*itálico*` e `***ambos***` |
| Listas | Marcadores `-` e números derivados da sequência; reiniciar após parágrafo comum |
| Código | Cerca com linguagem segura; aumentar a cerca quando o código contém crases |
| Imagens PNG/JPEG | Bytes preservados, links relativos em `imagens/`, extensão correspondente |
| Referência repetida à mesma imagem | Dois blocos podem apontar para um único arquivo exportado |
| Cor, tamanho e marca-texto | Texto e ênfase preservados; atributos visuais específicos não entram como CSS/HTML |

Rótulo de linguagem inválido deve virar `text`, sem permitir que uma quebra de linha injete Markdown. Caracteres especiais nas legendas são escapados. Não expor caminhos absolutos do perfil ou `trama.db` no documento.

Esta primeira exportação não pretende ser um backup fiel para importar de volta: o backup restaurável existente continua sendo a ferramenta de recuperação. Exportação em lote de cadernos/espaços, tags/contexto da biblioteca, PDF, checklist e estilos avançados ficam fora desta fatia. A perda dos atributos visuais específicos é um limite a aprovar junto aos casos, não uma promessa de equivalência visual com o editor.

## Segurança e recuperação de falhas

Preparar a saída antes de publicá-la; imagem ausente, erro de leitura ou cancelamento devem limpar os arquivos temporários pertencentes à operação. Nunca remover arquivos preexistentes para resolver uma falha. O banco, os anexos gerenciados e o rascunho de origem precisam permanecer disponíveis.

Streams abertos para exportação pertencem à operação e devem ser descartados em sucesso/falha/cancelamento. O cancelamento precisa alcançar a leitura/cópia da imagem, sem travar a interface. Rejeitar caminho relativo em vez de escrever silenciosamente no diretório de execução.

## Os 26 casos

Arquivo: [MarkdownExportWorkflowTests.cs](../../tests/Trama.IntegrationTests/Knowledge/MarkdownExportWorkflowTests.cs).

| Grupo | Casos | Exemplos |
| --- | ---: | --- |
| Rascunho e fluxo | 3 | Cancelamento inicial, exportar alterações sem salvar, página nova |
| Conteúdo e formatação | 9 | Ordem dos blocos, listas, ênfase, atributos visuais, três escapes, cerca e linguagem |
| Imagens e portabilidade | 4 | PNG/JPEG, imagem repetida, ausência de anexos e caminhos internos |
| Falhas e recuperação | 5 | Imagem ausente, leitura falha, cancelamento em andamento, destino existente e caminho relativo |
| Caminhos, interface e recursos | 3 | Título hostil, janela compacta e descarte de streams no sucesso |
| Proteções atuais | 2 | Modelo com texto/metadados separados e imagem já preservada pelo armazenamento |
| Total | 26 | 24 em RED e duas proteções aprovadas |

Os casos usam o editor existente, SQLite real temporário e o fluxo visual. Não acrescentam tipo fictício ou implementação de exportação ao produto. A fixture usa a composição existente para resolver o editor, permitindo injetar futuras dependências; somente o leitor de imagens é controlado nos cenários de falha/cancelamento.

**Limite do RED:** os 24 fluxos param na ausência de `ExportMarkdownButton`. Isso comprova a entrada funcional ausente, não 24 defeitos independentes no processamento de Markdown. As asserções posteriores de arquivos, formatação, descarte e recuperação só serão exercitadas durante o GREEN. Os testes compilam e as duas proteções passam; não há erro de compilação sendo apresentado como falha comportamental.

Resultados e revisão: [relatório 054](../reviews/054-markdown-export-red.md). O [roadmap](../../rodmap.md) separa implementação, validação de uso e revisão explícita do diff. A seção 9 do [CODEX](../../CODEX.md) exige aprovação destes casos antes da implementação. Manual/PDF não foram regenerados.

## Para estudar

1. Por que exportar o rascunho é diferente de buscar a última versão no banco?
2. Por que um arquivo Markdown portátil não substitui o backup restaurável?
3. Onde deve ficar a formatação, a escrita de arquivos e a escolha visual do destino em uma arquitetura por camadas?
4. Como publicar uma pasta pronta sem sobrescrever um destino criado por outra operação?
