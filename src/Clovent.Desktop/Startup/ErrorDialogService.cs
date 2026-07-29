namespace Clovent.Desktop.Startup;

/// <summary><see cref="IErrorDialogService"/> implementation showing <see cref="ErrorDialogForm"/> modally.</summary>
public sealed class ErrorDialogService : IErrorDialogService
{
    /// <inheritdoc/>
    public void ShowError(Exception exception, string? context = null)
    {
        using var form = new ErrorDialogForm(exception, context);
        form.ShowDialog();
    }
}
