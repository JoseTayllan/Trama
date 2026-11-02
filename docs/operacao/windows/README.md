# Publicar e compartilhar o Trama para Windows

Atualizado em 29/10/2026. Este guia serve para quem desenvolve e envia o pacote. O colega deve receber o [manual em PDF](../../../output/pdf/Trama_Guia_de_Execucao_Windows.pdf), que não exige ferramentas de desenvolvimento.

## 1. Gerar a versão após atualizar o código

Feche o Trama que estiver usando a pasta de saída. No **PowerShell do Windows**, execute a partir do projeto principal:

```powershell
cd C:\Users\joset\www\Trama
dotnet publish src/Trama.Desktop/Trama.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -o artifacts/publish/windows
```

Espere o comando terminar com sucesso. Se falhar, não envie a saída como versão atualizada: podem existir arquivos de uma publicação anterior. Uma restauração para outro runtime pode atualizar os locks NuGet; revisar essas alterações antes de concluir a entrega. Esta é uma publicação em pasta com runtime incluído, não um instalador ou arquivo único.

## 2. Identificar a pasta correta

```text
C:\Users\joset\www\Trama\artifacts\publish\windows
```

Dentro dela deve existir `Trama.Desktop.exe`, acompanhado de DLLs, runtime e outros arquivos. Envie **todos os arquivos da pasta**, não apenas o executável. O colega não precisa instalar .NET, SDK, VS Code, Ubuntu ou WSL. O pacote é para Windows x64; não representa uma versão Linux ou ARM64.

O pacote do teste inicial ficou em uma pasta externa de evidências do Codex. Para as novas publicações, use a pasta de saída acima; consulte o [relatório histórico de abertura](../../reviews/028-windows-exe-smoke-test.md) se precisar localizar o pacote inicial.

## 3. Testar antes de enviar

Abra a versão publicada e confira o estado Pronto. Para uma conferência isolada, sem usar seu perfil Windows habitual, execute:

```powershell
& .\artifacts\publish\windows\Trama.Desktop.exe --data-directory "$env:TEMP\Trama-Publish-Test"
```

O caminho seleciona um perfil de teste persistente entre aberturas; ele não apaga um perfil já existente. Crie somente dados fictícios. Teste criar caderno/página, salvar, fechar e reabrir pelo mesmo comando.

## 4. Preparar o ZIP

No Explorador de Arquivos, vá até `artifacts\publish`. Clique com o botão direito na pasta `windows` e use a opção de compactar em ZIP. Um nome possível para o arquivo de entrega é `Trama-Windows-x64-2026-10-29.zip`; atualize a data e identifique a versão a cada envio.

Envie esse ZIP acompanhado de `Trama_Guia_de_Execucao_Windows.pdf`. Não inclua o projeto inteiro, bancos pessoais, diretórios de perfis ou a pasta Linux. Os dados pessoais ficam fora da publicação; não os copie para a pasta de saída. Não é necessário mandar o lançador interno `Testar-Trama.cmd` do teste inicial.

## 5. Orientar o colega

O colega extrai tudo, coloca a pasta em um local fixo e abre `Trama.Desktop.exe`. Pode criar um atalho na Área de Trabalho para o exe mantendo os demais arquivos juntos. A biblioteca inicia sem suas notas; os dados criados pelo colega ficam no perfil local dele, normalmente `%LOCALAPPDATA%\Trama`.

Nas novas publicações, o cabeçalho informa Salvamento automático: aguarde dois segundos sem digitar e confirme a mensagem de página salva antes de fechar/reabrir. O botão Salvar página continua disponível. Pacotes anteriores com o indicador Salvamento manual exigem clicar no botão. Teste também texto, código, imagens e busca por tags em letras maiúsculas/minúsculas. Solicite versão do pacote, versão do Windows, passos e mensagem/print em caso de falha, sem dados sensíveis. Veja o [estudo do autosave](../../estudos/23-salvamento-automatico-implementado.md).

## Segurança e limites

O pacote ainda não tem assinatura digital. O teste inicial abriu no computador do desenvolvedor, mas isso não garante que outras máquinas aceitarão o executável. Se houver bloqueio do Windows, pedir a mensagem completa para análise; não orientar a desativar Smart App Control, antivírus ou outras proteções.

Substituir os arquivos do aplicativo não equivale a restaurar o banco. Antes de futuras atualizações com migrações, fazer backup pelo Trama e fechar o programa. Assinatura, instalador, atualização automática e teste em outras máquinas continuam pendentes.

## Documentos relacionados

- [Guia de WSL e VS Code](../wsl/README.md).
- [Resultado do teste Windows](../../reviews/028-windows-exe-smoke-test.md).
- [PDF para o colega](../../../output/pdf/Trama_Guia_de_Execucao_Windows.pdf).
