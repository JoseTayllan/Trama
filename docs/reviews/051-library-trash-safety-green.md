# Lixeira integrada: GREEN para revisão final

31/10/2026. Os dez casos complementares foram aprovados explicitamente antes das três correções. Permanecem intactos, junto dos 18 fluxos e dos 426 anteriores. Entrega integrada disponível para revisão humana do diff, da interface e do pacote isolado.

## Correções e resultado

O laço de busca de notas legadas agora aplica a filtragem da lixeira. A leitura pública mantém itens excluídos ocultos; a leitura interna de backup valida páginas armazenadas mesmo quando excluídas. O validador de backup confere origens e consistência básica dos lotes, sem mudar o formato 1 do pacote nem as versões de página.

Resultado final: **454 aprovados, zero falhas/ignorados**, sendo 77 unitários e 377 de integração. Build Release sem avisos/erros e formatação aprovados. Dez casos focados passaram antes da suíte completa. Os testes aprovados foram conferidos por hash e não foram adaptados à implementação.

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

## Conferência integrada

Harness com SQLite/Skia e perfil fictício confirmou envio por página/espaço, confirmação do impacto, exclusão independente anterior, bloqueio do filho, restauração da origem e depois da página, lixeira vazia e navegação com IDs/estilos preservados. Backup/restauração do perfil completo e recuperação dos itens excluídos no perfil restaurado também passaram. Capturas da interface inspecionadas.

Pacote Windows win-x64 self-contained gerado em cópia separada dos fontes. Smoke nativo: título Trama, Responding=true e fechamento normal. O perfil de demonstração foi copiado por backup SQLite e contém um espaço excluído para restauração. Não houve atualização do perfil pessoal, alteração de segurança ou substituição da publicação habitual. Fonte publicada comparada aos arquivos atuais.

Artefatos finais em `trama-library-trash-safety-green`: `unit.trx`, `integration.trx`, `qa/qa-success.txt`, capturas, `contrast.json`, `native-smoke-final.json`, `Testar-Lixeira.cmd`, executável em `windows/`, `correcoes.diff` e `implementacao-integrada.diff`. A primeira implementação mantém sua evidência em `trama-library-trash-green` e o RED aprovado em `trama-library-trash-safety-red`.

## Revisão e limites

Revisão focada em transações, lotes, restauração de pais/filhos, compatibilidade de construtores, migrações antigas, snapshots tardios, caminhos legado/estruturado e validação de conteúdo excluído. A sexta migração acrescenta a tabela de marcações; as cinco anteriores e as dependências permanecem intactas. Sem SQL de remoção de conteúdo/anexos ou implementação de esvaziamento permanente.

A entrega técnica está em GREEN; revisão humana do diff e estética ainda são necessárias. Outros DPI/computadores e uso prolongado não foram exauridos. A suíte executou no WSL; o smoke Windows verifica inicialização/fechamento, sem alegar execução de toda a suíte no Windows. O aplicativo Linux que já estava aberto não foi fechado ou reiniciado automaticamente.

Esforço moderado, um agente e nenhum subagente. Foram reutilizados fixtures, mapa de consultas e harness; casos focados guiaram correções e a suíte completa validou a integração. Não há medição de tokens/custo monetário. Economia seguinte: reunir o feedback neste pacote e evitar reabrir o escopo já coberto pelos 28 casos aprovados.
