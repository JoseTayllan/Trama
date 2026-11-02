# Publicação e abertura do executável Windows — 29/10/2026

## Resultado

Publicação Release win-x64 self-contained concluída. O processo nativo abriu a janela Trama e criou trama.db no perfil isolado; o usuário confirmou: **Abriu e mostra Pronto**. O pacote executou sem WSL e nenhuma política de segurança foi alterada. O bloqueio observado anteriormente não se reproduziu nesta inicialização; isso não demonstra que todos os builds futuros serão aceitos.

## Como foi produzido

Fontes e configuração foram copiados para uma pasta externa isolada, sem bin/obj. A primeira tentativa de desativar o lock na restauração falhou com NU1005 porque já existiam locks; a publicação final manteve locks na cópia e preservou os originais. Não houve alteração no código do produto, migrações ou testes.

Comando equivalente para repetir uma publicação a partir da raiz do projeto (a restauração para outro runtime pode atualizar os locks; nesta conferência usamos a cópia isolada):

```powershell
dotnet publish src/Trama.Desktop/Trama.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -o artifacts/publish/windows
```

O pacote contém Trama.Desktop.exe, DLLs e runtime .NET. Copiar a pasta publish inteira; não transportar apenas o exe. Não foi gerado instalador nem executável de arquivo único. A publicação não exige .NET instalado no computador de destino; compatibilidade e segurança em outras máquinas ainda precisam de testes próprios.

## Repetir o teste deste pacote

Pasta de evidências: `C:\Users\joset\.codex\visualizations\2026\10\27\01a17ef1-6260-7453-a765-f5b9dc1c2eca\trama-windows-exe`.

Abra `Testar-Trama.cmd` nessa pasta. Ele executa `publish\Trama.Desktop.exe` com `--data-directory` apontando para `test-profile`. O perfil preserva os dados sintéticos entre aberturas. Abrir somente o exe sem esse argumento seleciona o perfil padrão Windows; para repetir esta conferência, use o lançador.

Sugestão para conferência manual: criar espaço, caderno e página; digitar texto; salvar; fechar; reabrir pelo mesmo lançador; verificar persistência. Testar negrito/itálico e imagens em seguida. Somente a abertura e o estado Pronto foram confirmados nesta etapa; o restante desse roteiro ainda não foi confirmado pelo usuário.

## Limites, segurança e revisão

O executável está sem assinatura Authenticode. Não garantir que será aceito em todos os computadores ou após nova compilação. Assinatura, reputação, instalador e distribuição continuam pendentes. O banco pessoal anterior não foi aberto nem transferido por este teste.

A suíte de 298 testes foi validada anteriormente no WSL; esta etapa fez publicação e smoke test nativo, sem reexecutar a suíte nem reivindicar nova aprovação dela. Não houve alteração de código a revisar. O diff desta etapa contém este relatório e links de documentação; o lançador externo apenas seleciona o perfil de teste, sem remover dados ou alterar segurança. Nenhuma credencial foi adicionada.

Esforço: um agente, sem subagentes; duas tentativas de publicação e uma abertura. Sem medição monetária ou de tokens. Economia: reutilizar o pacote para a conferência manual e recompilar somente quando o código mudar.
