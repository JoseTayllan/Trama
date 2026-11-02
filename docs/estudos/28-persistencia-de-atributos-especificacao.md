# Persistência de atributos: especificação aprovada

30/10/2026. O responsável aprovou os 21 casos e as regras de versões antes da implementação. Esta especificação preserva o contrato; o [GREEN posterior](29-persistencia-de-atributos-implementada.md) passou 374 testes. A interface mantém a hierarquia planejada Espaço → Caderno → Páginas; os controles de formatação continuam na próxima fatia.

## O que precisa ser preservado

Tamanho, cor da letra e marca-texto devem retornar iguais ao salvar, fechar a conexão e abrir novamente uma página. Preservar identidade, título, conteúdo exato, quebras de linha, offsets e ordem dos blocos. Código, imagens e estilos anteriores continuam válidos.

O modelo já preserva atributos em memória, mas o repositório atual escreve páginas formatadas como versão 2 e recusa versão 3. O backup também só aceita páginas 1/2. Isso permite armazenar atributos novos com um rótulo que aplicativos anteriores poderiam interpretar sem entendê-los. A nova versão de conteúdo é a proteção proposta; não basta acrescentar propriedades ao JSON.

## Regras de versão

| Conteúdo | Versão da página |
| --- | --- |
| Sem estilos de texto | 1 |
| Somente negrito/itálico | 2 |
| Com tamanho, cor ou destaque explícitos | 3 |

Escrever a versão conforme os atributos efetivamente presentes. Ao remover todos os atributos novos de uma página versão 3, voltar para a versão correspondente ao conteúdo restante, preservando seu ID. Se nenhum estilo restar, remover também seus metadados antigos. Atualizar uma página enriquecida precisa substituir os intervalos anteriores sem duplicação.

Ler versões 1/2 válidas sem promover automaticamente seu formato. Rejeitar conteúdo enriquecido marcado como 1/2, sem modificar o banco. Uma versão desconhecida/futura não pode ser lida nem sobrescrita. A versão do pacote de backup permanece 1 nesta proposta: o contêiner continua o mesmo; a mudança é na interpretação das páginas do SQLite.

Valores inválidos devem falhar antes de substituir uma página já gravada. Na restauração, validar o conteúdo antes de publicar a pasta de destino; rejeições não podem deixar saída parcial nem staging abandonado. Na criação de backup, não publicar arquivo que declare conteúdo inconsistente.

## Testes preparados

[RichTextVersioningTests.cs](../../tests/Trama.IntegrationTests/Knowledge/RichTextVersioningTests.cs) contém 21 casos:

1. Salvar página enriquecida como versão 3, preservando atributos/texto/código.
2. Reabrir versão 3 por contexto/conexão novos.
3. Atualizar versão 3 sem trocar identidade nem acumular estilos antigos.
4–5. Remover atributos novos e gravar versão 1 ou 2 conforme o conteúdo restante.
6–7. Ler/salvar versões anteriores válidas sem promoção desnecessária.
8–9. Rejeitar atributos enriquecidos indevidamente rotulados como versões 1 e 2 na leitura.
10–12. Tamanho, primeiro plano ou destaque inválidos não sobrescrevem a página original.
13. Versão futura não é lida nem sobrescrita.
14. Backup/restauração de versão 3 preserva atributos, código e imagem sem alterar a origem; pacote continua versão 1.
15. Restaurar um pacote de versão 3 montado independentemente do escritor de backup.
16–17. Restaurar páginas antigas válidas, mantendo suas versões e conteúdo.
18–19. Rejeitar criação de backup de conteúdo enriquecido rotulado como 1/2, sem publicar arquivo ou abandonar staging.
20–21. Rejeitar restauração desse conteúdo inconsistente, preservando o pacote de entrada e limpando staging.

Os casos usam SQLite real em diretórios temporários exclusivos. A preparação de páginas com rótulos específicos usa SQL parametrizado. O pacote independente usa o snapshot nativo do SQLite e manifesto com hash correto, para não confundir incompatibilidade de página com corrupção de ZIP ou checksum. A comparação de estados abrange versão, título, texto/código e JSON dos estilos.

Não existe novo método vazio, contrato fictício ou acesso ao perfil pessoal. Os testes aprovados do modelo permanecem congelados. A compatibilidade de backups com migrações antigas já é exercitada pela suíte anterior; estes casos acrescentam a distinção entre versões de conteúdo.

O [RED histórico](../reviews/038-rich-text-storage-red.md) teve 13 falhas comportamentais esperadas e oito proteções passando, preservando os 353 anteriores. Após aprovação explícita dos 21 casos e das versões, a implementação passou 374 testes, build e formatação. Ver [relatório GREEN](../reviews/039-rich-text-storage-green.md). Os testes aprovados permaneceram intactos.

## Plano após aprovação dos casos

1. Centralizar a interpretação das versões no repositório, distinguindo flags antigas de atributos novos.
2. Ler, validar e salvar páginas versão 3 com metadados completos; manter formatos anteriores e rejeitar inconsistências.
3. Coordenar a validação de backup com o leitor, preservando snapshot, limites, staging e restauração em pasta nova.
4. Fazer os testes aprovados passarem sem alterações e repetir as verificações aplicáveis.
5. Só depois preparar os testes dos controles, seleção, renderização e histórico completo.

Não editar migrações históricas ou alterar o manifesto só para aceitar o novo conteúdo. A necessidade de migração será decidida pela implementação; a tabela de estilos existente já guarda JSON e a versão da página já é um inteiro.

Para estudar: por que uma versão de página difere da versão do ZIP? Como um teste de restauração independente detecta um erro que o par escritor/leitor poderia esconder? Por que validar antes de sobrescrever preserva o conteúdo quando uma entrada é inválida?
