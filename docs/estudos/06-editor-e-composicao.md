# Editor e composição do desktop

Estado: **GREEN após aprovação, com 57 testes passando**. O primeiro editor foi conectado ao SQLite e ativado no desktop. Os testes aprovados foram preservados.

## Especificação para revisão

1. Salvar envia título e corpo exatamente como digitados; no sucesso, registra o snapshot, informa o resultado e limpa o rascunho.
2. Validação ou falha de gravação informa o erro e preserva o rascunho (dois casos).
3. Durante o salvamento, IsSaving é verdadeiro e o comando impede novo acionamento pela interface; termina liberando o comando.
4. A View conecta os campos, o botão e a mensagem ao ViewModel, com bindings em ambos os sentidos.
5. A composição inicializa um diretório explícito, conecta os serviços e permite recuperar por outro contexto a nota salva pelo editor, mesmo após recriar o provedor.
6. Falha ao preparar o diretório propaga sem fallback silencioso.

Total: seis métodos, sete casos em [NoteEditorTests](../../tests/Trama.IntegrationTests/Knowledge/NoteEditorTests.cs). No RED, seis casos paravam nos métodos sem implementação e a View não tinha controles. No GREEN, as asserções de comando, bindings e armazenamento são alcançadas sem alterar os testes.

## Componentes implementados

- [ICreateNote](../../src/Trama.Application/Knowledge/ICreateNote.cs): entrada de criação para o editor, sem dependência de EF.
- [SqliteNoteService](../../src/Trama.Infrastructure/Persistence/SqliteNoteService.cs): cria e descarta um contexto por operação, delegando ao CreateNote e repositório existentes.
- [NoteEditorViewModel](../../src/Trama.Desktop/ViewModels/NoteEditorViewModel.cs): rascunho, comando assíncrono, estado e feedback.
- [NoteEditorView](../../src/Trama.Desktop/Views/NoteEditorView.axaml): campos e botão, sem regras de banco no code-behind.
- [DesktopComposition](../../src/Trama.Desktop/DesktopComposition.cs): preparação do banco e registro de dependências, fora do ViewModel.

AsyncRelayCommand organiza a execução assíncrona e seu CanExecute. ObservableObject notifica mudanças usando SetProperty. O ViewModel captura o rascunho, marca IsSaving e atualiza o feedback conforme o resultado. No sucesso, guarda LastSavedNote e limpa os campos; falhas mantêm o texto. O finally libera o estado ocupado. Os campos ficam desabilitados enquanto salva.

SqliteNoteService guarda somente opções de conexão e cria um contexto independente por chamada. Assim, não compartilha DbContext singleton nem reaproveita um contexto que falhou. DesktopComposition prepara o banco antes de montar o provedor; o ViewModel nunca consulta arquivos ou EF diretamente.

## Inicialização no aplicativo

[App](../../src/Trama.Desktop/App.axaml.cs) abre a janela com estado de preparação. No evento Opened chama DesktopComposition para o diretório padrão, recebe o editor e o atribui ao [MainWindowViewModel](../../src/Trama.Desktop/ViewModels/MainWindowViewModel.cs). Um DataTemplate na [MainWindow](../../src/Trama.Desktop/Views/MainWindow.axaml) apresenta NoteEditorView.

O banco padrão fica em `%LOCALAPPDATA%\Trama\trama.db` no Windows. A inicialização não depende do diretório do repositório nem de um servidor. Fechar durante a preparação cancela a operação; erros esperados de diretório/banco são apresentados sem fallback silencioso. Na saída, o provedor é descartado.

Salvar só anuncia sucesso após a conclusão da persistência. A interface também apresenta cancelamento ou erro inesperado sem despejar detalhes técnicos, preservando o rascunho. Esses dois caminhos defensivos não possuem casos específicos nos sete testes aprovados; não afirmar cobertura completa de exceções inesperadas ou encerramento durante uma gravação.

## Verificações

Build isolado sem avisos e formatação aprovada. Suíte completa: 43 testes de integração e 14 unitários aprovados, nenhum ignorado. Resultados atuais: `artifacts/test-results/editor-green/`; histórico RED: `artifacts/test-results/editor-red/`. Hashes confirmam que nenhum teste aprovado foi alterado.

Testes usam diretórios temporários e Avalonia headless. A abertura nativa também foi verificada: processo da versão nova respondendo, título Trama e arquivo do banco padrão existente, sem inspecionar notas pessoais. Essa conferência não equivale a uma auditoria visual detalhada ou teste completo de acessibilidade; falha real de startup e encerramento durante salvamento continuam sem testes de UI dedicados.

Ainda não existem lista de notas, edição de registros existentes, busca, anexos ou autosave. O banco preserva as notas, mas a consulta visual será a próxima entrega. Rascunhos não salvos não são recuperados após fechar; aguarde a confirmação de salvamento. Este armazenamento de notas não é o futuro cofre criptografado.

Para abrir a versão compilada nesta etapa:

```powershell
dotnet run --project src/Trama.Desktop/Trama.Desktop.csproj --configuration Release --artifacts-path artifacts/repository-build --no-build
```

Revisão e documentação feitas pelo coordenador, sem subagentes, conforme a preferência por economia. Próxima etapa: especificar a lista e abertura das notas existentes.

Perguntas: por que o ViewModel não deve receber DbContext? Por que uma falha não deve apagar o rascunho? Por que testar CanExecute durante uma tarefa pendente?

[Voltar ao índice](README.md)
