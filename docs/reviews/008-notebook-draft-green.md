# Caderno em blocos — estrutura GREEN

Data: 27 de outubro de 2026. Implementação após aprovação dos 15 casos do [RED](007-notebook-draft-red.md), disponível para revisão humana do diff.

## O que mudou

[NotebookDraft](../../src/Trama.Application/Knowledge/NotebookDraft.cs) agora gerencia uma lista ordenada de blocos de texto, código e referência de imagem em memória. Permite inserir, atualizar, mover e remover por identidade. Expõe uma visão somente de leitura da lista e calcula a presença de conteúdo conforme os blocos atuais.

Conteúdo extenso, Unicode, quebras de linha, espaços e indentação são preservados. A atualização mantém posição e identidade. A inserção rejeita identidade duplicada, posição inválida e imagem sem identificador de anexo. Validações são realizadas antes de modificar a lista. Nenhum teste aprovado foi editado.

## Verificação executada

```powershell
dotnet build Trama.slnx --configuration Release --artifacts-path artifacts/notebook-build --no-restore --verbosity quiet
dotnet test Trama.slnx --configuration Release --artifacts-path artifacts/notebook-build --no-build --logger "trx;LogFilePrefix=notebook-green" --results-directory artifacts/notebook-green/results --verbosity quiet
dotnet format Trama.slnx --verify-no-changes --no-restore
```

Build com zero avisos e zero erros. **72 casos aprovados: 29 unitários e 43 de integração.** Os 15 casos aprovados para esta fatia passaram sem alterações; nenhum caso ignorado. Formatação aprovada. Evidências TRX em `artifacts/notebook-green/results`. A CI remota não foi executada.

## Revisão e limites

A revisão conferiu ordem ao mover nos dois sentidos, preservação de conteúdo, validação anterior à mutação e proteção da coleção exposta. A leitura é uma visão do estado atual, não um snapshot independente. O rascunho é destinado a uso sequencial; não contém sincronização para acesso concorrente.

Os testes aprovados não cobrem todos os argumentos inválidos, identidade ausente, troca do tipo de um bloco nem mutação da coleção por conversão de tipo. A implementação inclui validações básicas de nulo, identidade vazia e identidade não encontrada, mas não atribuir a elas cobertura automatizada específica. Antes de conectar a interface, especificar os cenários adicionais necessários.

Não houve alteração de tela, dependência, esquema ou acesso a dados pessoais. Código armazenado não é executado. Imagens ainda são apenas referências em memória; não há importação, validação de arquivo, exibição ou remoção física. Não ligar esse rascunho ao CreateNote atual sem especificar persistência e compatibilidade das notas existentes.

## Próxima etapa e esforço

O próximo recorte é especificar salvamento e leitura do caderno, preservação das notas antigas e armazenamento gerenciado de imagens, antes de conectar a página contínua. Documentação de estudo e roadmap foram atualizados.

Um agente, sem delegação ou dependências adicionais; um build, uma execução da suíte completa e uma verificação de formatação. Não há medição de tokens por tarefa disponível. Manter o escopo por fatia evita reexplorar o projeto inteiro e reduz consumo nas próximas entregas.
