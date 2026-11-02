# Atributos de texto: RED e hierarquia do caderno

30/10/2026. O responsável aceitou o modelo conceitual e pediu que Espaço → Caderno → Páginas fique explícito. A prévia foi revisada: Espaço Estudos, Caderno Curso C#, Página Interfaces e contratos, identificados na lateral e no topo. A hierarquia é uma relação de organização; texto/código/imagens pertencem à página.

## Escopo e evidências

Novo arquivo: [RichTextAttributesTests.cs](../../tests/Trama.UnitTests/Knowledge/RichTextAttributesTests.cs). São 28 casos executáveis, sem alteração de código/contratos de produção ou testes anteriores. A entrada é o JSON de intervalos, já utilizado pelo modelo de estilos; não foram criados métodos vazios nem usados erros de compilação para simular RED.

Casos:

- Seis tamanhos previstos: 12, 14, 16, 18, 24 e 32 unidades lógicas.
- Cor da letra, marca-texto e combinação com tamanho/negrito/itálico.
- Intervalos adjacentes com cores ou tamanhos diferentes permanecem separados; iguais são unidos sem perda.
- Aplicar itálico parcialmente e remover a última flag mantém os demais atributos.
- Inserir dentro/antes, substituir, excluir e lidar com offsets UTF-16 preserva metadados e texto.
- Cor sem negrito/itálico é formatação válida; copiar intervalos protege contra mutação da lista do chamador.
- Quatro tamanhos não suportados e duas cores desconhecidas são rejeitados, em vez de descartados silenciosamente.
- Compatibilidade com JSON antigo de negrito/itálico permanece.

Comando executado no PowerShell:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

O helper sincronizou fontes no espelho autorizado, restaurou com lock, compilou Release e verificou formatação antes de executar toda a suíte. Build: zero avisos/erros. Formatação: aprovada.

| Grupo | Passaram | Falharam | Total |
| --- | --- | --- | --- |
| Unitários anteriores | 49 | 0 | 49 |
| Integração anterior | 276 | 0 | 276 |
| Novos atributos | 1 | 27 | 28 |
| Total | 326 | 27 | 353 |

Motivos confirmados nos TRXs: `FontSize`, `Foreground` e `Highlight` são ignorados pelo contrato atual; intervalos diferentes são unidos indevidamente; remover negrito apaga o intervalo mesmo havendo outros atributos propostos; cor sem flags é rejeitada; tamanhos/cores desconhecidos são descartados sem validação. O caso novo de JSON antigo passa. Não houve falha de compilação, infraestrutura ou integração anterior.

## Limites e próxima ação

Esta fatia especifica representação e edição dos atributos no modelo em memória. Não implementa seleção visual, limpar formatação, salvamento SQLite, backup, undo/redo enriquecido ou nova navegação. Essas integrações terão seus próprios testes e revisão. A prévia é estática; a interface atual permanece inalterada. Não foi acessado banco pessoal nem alterada política de segurança.

Pausa solicitada pelo responsável no fluxo TDD e registrada na seção 9 do CODEX: apresentar diff e aguardar aprovação explícita dos 28 casos antes de implementar. Após aprovação, ampliar atributos de forma aditiva, preservar os 325 anteriores e fazer os casos aprovados passarem sem alterá-los. Proteções de versões e restauração continuam no plano seguinte.

Riscos tratados por esta especificação: perda silenciosa e fusão incorreta dos atributos. Permanecem para as fatias seguintes persistência/backup, contraste e foco, altura/rolagem com tamanhos mistos e histórico completo.

Artefatos externos de revisão nesta entrega: `trama-rich-formatting-red/testes-e-plano.diff`, `unit.trx`, `integration.trx`, `caderno-trama-hierarquia.png` e `audit.json`, na pasta de artefatos da sessão. A execução completa terminou com código 1 por causa dos 27 casos RED esperados; não declarar GREEN.

Esforço moderado, um agente, sem subagentes. Uma execução completa de build/format/testes e uma inspeção visual da prévia; sem medição de tokens/custo. Economia na próxima etapa: executar os 28 casos durante o ajuste do modelo e repetir a suíte completa apenas na validação final ou se surgir regressão.
