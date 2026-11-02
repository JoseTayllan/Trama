using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public sealed class NoteEditorViewModel : ObservableObject
{
    private readonly ICreateNote _createNote;
    private string _title = string.Empty;
    private string _body = string.Empty;
    private string _feedback = string.Empty;
    private bool _isSaving;
    private CreateNoteStatus? _lastStatus;
    private NoteSnapshot? _lastSavedNote;

    public NoteEditorViewModel(ICreateNote createNote)
    {
        ArgumentNullException.ThrowIfNull(createNote);
        _createNote = createNote;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Body { get => _body; set => SetProperty(ref _body, value); }
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public CreateNoteStatus? LastStatus { get => _lastStatus; private set => SetProperty(ref _lastStatus, value); }
    public NoteSnapshot? LastSavedNote { get => _lastSavedNote; private set => SetProperty(ref _lastSavedNote, value); }
    public IAsyncRelayCommand SaveCommand { get; }

    private async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        LastStatus = null;
        LastSavedNote = null;
        Feedback = "Salvando…";
        try
        {
            var result = await _createNote.ExecuteAsync(new CreateNoteRequest(Title, Body));
            LastStatus = result.Status;
            Feedback = result.Status switch
            {
                CreateNoteStatus.Created => "Nota salva neste computador.",
                CreateNoteStatus.ValidationFailed => "Preencha o título ou o conteúdo da nota.",
                _ => "Não foi possível salvar. Seu rascunho foi mantido."
            };
            if (result.Status == CreateNoteStatus.Created)
            {
                LastSavedNote = result.Note;
                Title = string.Empty;
                Body = string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
            Feedback = "Salvamento cancelado. Seu rascunho foi mantido.";
        }
        catch (Exception)
        {
            Feedback = "Erro inesperado ao salvar. Seu rascunho foi mantido.";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
