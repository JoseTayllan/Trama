# Desenvolvimento pelo Ubuntu no WSL

29/10/2026. O Smart App Control bloqueou a DLL recém-compilada do desktop no Windows. Para continuar, preparamos o Ubuntu-24.04 já existente no WSL2, usando o usuário barberdev e WSLg. Nenhuma política de segurança do Windows foi desativada. O bloqueio do executável Windows continua pendente.

**Atualização posterior:** um pacote Windows Release self-contained abriu nativamente e o usuário confirmou o estado Pronto. Este pacote funcionou sem WSL e sem mudar a proteção; a assinatura e aceitação de builds futuros continuam pendentes. Consulte o [teste do executável Windows](../../reviews/028-windows-exe-smoke-test.md) para repetir a abertura com perfil isolado.

## Passo a passo: entrar no Ubuntu e abrir o VS Code

### 1. Identificar o terminal

Se aparecer `joset@Tayllan MINGW64 ~/www/Trama`, você está no **Git Bash do Windows**. Ele aceita comandos bash, mas não é o Ubuntu/WSL. Executar o script ali pode mostrar `Install rsync in the Linux environment.` mesmo que o rsync já esteja instalado no Ubuntu.

Não é necessário instalar rsync no Git Bash para seguir este fluxo. Primeiro entre na distribuição Linux.

### 2. Entrar no Ubuntu

No Git Bash ou no PowerShell, execute:

```powershell
wsl.exe -d Ubuntu-24.04 -u barberdev
```

O terminal passa a executar comandos no Ubuntu como usuário `barberdev`. Para conferir:

```bash
whoami
uname -s
```

Os resultados esperados são `barberdev` e `Linux`. O indicador `MINGW64` não deve mais aparecer no prompt ativo.

### 3. Navegar até o projeto principal

Dentro do Ubuntu:

```bash
cd /mnt/c/Users/joset/www/Trama
pwd
```

O WSL acessa o disco `C:` pelo caminho `/mnt/c`. Essa pasta é o mesmo projeto de `C:\Users\joset\www\Trama`, não um novo repositório.

### 4. Abrir o VS Code conectado ao WSL

Ainda dentro do Ubuntu e na pasta do projeto:

```bash
code .
```

Se solicitado, instale a extensão **WSL**, da Microsoft, no VS Code do Windows. A primeira abertura pode preparar o servidor do VS Code dentro do Ubuntu. Confirme que o canto inferior esquerdo mostra **WSL: Ubuntu-24.04**.

Se `code` não for encontrado, abra o VS Code pelo Windows, instale a extensão WSL e use a paleta de comandos (`Ctrl+Shift+P`): **WSL: Connect to WSL using Distro...** → **Ubuntu-24.04**. Depois abra a pasta `/mnt/c/Users/joset/www/Trama`.

### 5. Executar o Trama

No terminal Ubuntu, ou no terminal integrado do VS Code conectado ao WSL, estando na pasta principal:

```bash
bash tools/run-wsl.sh run
```

Continue editando os arquivos da pasta principal. O script sincroniza os fontes para o espelho Linux e executa a aplicação ali. Não edite diretamente `/home/barberdev/trama-codex`: a próxima sincronização substitui seus fontes.

Feche a janela antes de recompilar. Para executar build, formatação e testes:

```bash
bash tools/run-wsl.sh test
```

Para sair do terminal Ubuntu e voltar ao terminal Windows que o iniciou:

```bash
exit
```

Referência: [VS Code com WSL](https://code.visualstudio.com/docs/remote/wsl).

## Alternativa: abrir diretamente pelo PowerShell

Execute no PowerShell:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh run
```

O comando sincroniza fontes, restaura dependências com lock, compila e abre a janela Linux. Feche a janela antes de recompilar; alterações de código aparecem na próxima execução.

Para build, formatação e testes:

```powershell
wsl -d Ubuntu-24.04 -u barberdev --exec bash /mnt/c/Users/joset/www/Trama/tools/run-wsl.sh test
```

## Onde ficam código, SDK e dados

- Fonte principal: `C:\Users\joset\www\Trama`. Continue editando aqui.
- Espelho descartável de fontes: `/home/barberdev/trama-codex`. Não editar nele: a próxima sincronização substitui os arquivos de src/tests/assets.
- SDK exato 10.0.401: `/home/barberdev/.dotnet-trama`, instalado pelo script oficial da Microsoft no perfil Linux.
- Perfil de desenvolvimento: `/home/barberdev/trama-codex/.local-profile`. Fica preservado entre execuções.
- Resultados: `artifacts/wsl-results` dentro do espelho.

Os dados pessoais do Windows não foram copiados. A biblioteca Linux começa separada; uma biblioteca vazia não indica perda das notas anteriores. Para transportar conteúdo depois, usar backup/restauração em uma pasta nova, com revisão própria.

O script [run-wsl.sh](../../../tools/run-wsl.sh) exclui bin/obj da sincronização para não reutilizar DLLs Windows. Ele verifica uma marca de origem antes de atualizar o espelho. Não copiar bancos ativos ou binários entre as plataformas. Os subdiretórios de fontes do espelho são sincronizados com exclusão dos arquivos removidos; nunca guardar documentos pessoais nesses diretórios.

## Preparação realizada neste computador

O helper também exige Python 3, já disponível neste Ubuntu. Depois de sincronizar, ele mantém somente os grafos genéricos dos locks NuGet no espelho Linux para evitar NU1004 após uma publicação win-x64. Versões/hashes genéricos continuam fixados e a restauração usa --locked-mode; os locks do projeto Windows não são alterados.

**Limite confirmado do clipboard neste ambiente:** o print do Windows chega ao Wayland como image/bmp, mas não foi anunciado no X11 usado pelo Trama, mesmo após reinício. A correção do aplicativo ainda não foi implementada. Para continuar, use o exe Windows validado ou salve o print em PNG/JPEG e importe por + Imagem. Não confundir o sucesso do harness headless com validação da ponte nativa. Ver [diagnóstico](../../reviews/030-code-highlighting-green.md).

O Ubuntu já tinha WSLg, rsync e outras bibliotecas X11. Foram instalados libice6 e libsm6 pelo apt para abrir a janela Avalonia. O helper exige esse ambiente pronto; ele não instala dependências nem altera políticas de segurança automaticamente. Em outro computador, preparar WSL2, distribuição, usuário, WSLg, SDK e bibliotecas antes de usar.

## Evidências e aprendizado

Build Release sem avisos/erros, formatação aprovada e 298 testes aprovados: 49 unitários e 249 de integração. A conferência Avalonia.Headless com Skia passou seleção em negrito/itálico, deslocamento de estilos ao editar, texto colado, duas colagens de bitmap, drop, reabertura, backup/restauração e crescimento do texto. A janela real foi iniciada pelo WSLg; clipboard/drag nativos ainda exigem uso interativo.

Dois testes existentes revelaram uma diferença entre sistemas ao criar uma pasta onde já existe um arquivo: normalizamos a falha para IOException em LocalDatabase, preservando o contrato e os testes aprovados. O harness também revelou que um bitmap do próprio processo permanece pertencendo ao clipboard: ele não pode ser descartado depois da primeira colagem. O adaptador preserva essa propriedade e descarta bitmaps externos após converter.

Para estudar: por que separar fontes de binários? Qual a diferença entre um recurso emprestado e um recurso que o método deve descartar? Por que testes headless não comprovam a integração com a área de transferência do Windows?

Ver o [relatório e limites](../../reviews/027-wsl-development-green.md). Referências de preparação: [comandos WSL](https://learn.microsoft.com/en-us/windows/wsl/basic-commands) e [instalador .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script).
