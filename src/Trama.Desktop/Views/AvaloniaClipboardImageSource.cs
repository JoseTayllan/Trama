using Avalonia.Input;
using Avalonia.Input.Platform;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed class AvaloniaClipboardImageSource(IClipboard? clipboard) : IClipboardImageSource
{
    public async Task<Stream?> OpenImageAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (clipboard is null) return null;
        var inProcessData = await clipboard.TryGetInProcessDataAsync();
        var bitmap = inProcessData is null
            ? await clipboard.TryGetBitmapAsync()
            : await inProcessData.TryGetValueAsync(DataFormat.Bitmap);
        if (bitmap is null) return null;
        var stream = new BoundedImageStream();
        try
        {
            await Task.Run(() => bitmap.Save(stream), cancellationToken);
            stream.Position = 0;
            return stream;
        }
        catch { stream.Dispose(); throw; }
        finally
        {
            // In-process values are still owned by the clipboard and may be pasted again.
            if (inProcessData is null) bitmap.Dispose();
        }
    }

    private sealed class BoundedImageStream : MemoryStream
    {
        private void Check(int count)
        {
            if (count > 10 * 1024 * 1024 - Position) throw new InvalidDataException("The clipboard image exceeds 10 MiB.");
        }
        public override void Write(byte[] buffer, int offset, int count) { Check(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Check(buffer.Length); base.Write(buffer); }
    }
}
