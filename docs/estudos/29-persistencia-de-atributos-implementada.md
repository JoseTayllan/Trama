# Persistência dos novos atributos de texto

30/10/2026. Implementação após aprovação dos 21 casos e das versões. Suíte completa: **374 aprovados**, sem falhas/ignorados; build Release sem avisos/erros e formatação aprovada no WSL. Manual/PDF intactos. A interface ainda não oferece os controles de tamanho/cor/destaque.

## Versão é um contrato de interpretação

[SqliteNotebookRepository](../../src/Trama.Infrastructure/Persistence/SqliteNotebookRepository.cs) escolhe a versão pelo conteúdo: 1 sem estilos; 2 somente negrito/itálico; 3 quando algum intervalo tem tamanho, cor da letra ou destaque explícito. Os atributos continuam no JSON da tabela existente `NotebookTextStyles`; não houve nova migração nem mudança de esquema.

Salvar valida os blocos antes de começar a substituir dados e conserva a transação existente. Atualizar uma página versão 3 preserva o ID e substitui blocos/estilos anteriores. Ao remover os atributos, a versão passa a 1 ou 2 conforme o conteúdo restante. Isso descreve o conteúdo atual, não o histórico das funcionalidades usadas na página.

Ler valida a versão declarada, a localização dos metadados e os intervalos. A versão calculada dos blocos lidos precisa corresponder à declaração. Uma página enriquecida marcada como 1/2 é recusada; uma versão desconhecida continua recusada. Não ignorar metadados de uma página simples só porque seu rótulo diz 1.

## Esquemas anteriores

Páginas simples já existiam antes da tabela de estilos. Para versão 1, o leitor consulta o catálogo de tabelas SQLite antes de ler metadados. Se a tabela não existir, a leitura simples continua funcionando; se existir, não pode haver estilos escondidos nessa página. A consulta é fixa e fica na infraestrutura, sem SQL em ViewModels.

Uma checagem externa usou um banco temporário com as quatro primeiras migrações, gravou/reabriu/editou texto e código sem tabela de estilos e confirmou que a leitura não atualizou o esquema. Depois aplicou as migrações atuais e confirmou a preservação da página. Essa checagem complementa os testes, sem alterar a suíte aprovada ou acessar dados pessoais.

## Backup compartilha a validação

[LocalBackupService.Validation](../../src/Trama.Infrastructure/Persistence/LocalBackupService.Validation.cs) continua verificando integridade SQLite, migrações, referências e limites. A interpretação das páginas agora é delegada ao mesmo leitor do repositório, removendo a lista duplicada de versões 1/2 que impedia backups de páginas 3.

O snapshot é validado antes de publicar o pacote; na restauração, o staging é validado antes de publicar a pasta. O tratamento existente converte conteúdo inválido em `InvalidDataException` e limpa os temporários. O pacote continua formato 1, com manifesto/hashes e imagens gerenciadas. Formato do pacote e versão do conteúdo são contratos diferentes.

Os [21 testes aprovados](../../tests/Trama.IntegrationTests/Knowledge/RichTextVersioningTests.cs) exercitam versões, gravação/reabertura/atualização, remoção de atributos, falhas sem sobrescrita, conteúdo inconsistente, backup com imagem e restauração independente do escritor. Todos os 353 anteriores continuam passando. Ver [relatório](../reviews/039-rich-text-storage-green.md).

## Próxima etapa

Preparar testes das ações por seleção, controles de tamanho/cor/destaque, limpar formatação, estados mistos, foco e histórico completo. Depois conectar a apresentação contínua com Espaço → Caderno → Páginas claramente identificados. Ainda não foi gerado um novo executável nem validado contraste/renderização dos atributos na interface.

Para estudar: por que recalcular a versão ao ler detecta um rótulo incorreto? Por que consultar a existência de uma tabela evita quebrar esquemas anteriores? Como reutilizar o leitor impede que backup e aplicativo discordem sobre o formato aceito?
