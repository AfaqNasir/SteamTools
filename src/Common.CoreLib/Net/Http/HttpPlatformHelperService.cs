using System.Globalization;
using System.IO.FileFormats;

namespace System.Net.Http;

/// <inheritdoc cref="IHttpPlatformHelperService"/>
public abstract class HttpPlatformHelperService : IHttpPlatformHelperService
{
    protected const string DefaultUserAgent = "Mozilla/5.0 (Windows Phone 10.0; Android 4.2.1; Microsoft; Lumia 950) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Mobile Safari/537.36 Edge/14.14263";

    public abstract string UserAgent { get; }

    public virtual string AcceptImages => "image/png, image/*; q=0.8";

    public virtual string AcceptLanguage => CultureInfo.CurrentUICulture.GetAcceptLanguage();

    static readonly Lazy<ImageFormat[]> mSupportedImageFormats = new(() => new ImageFormat[]
    {
        ImageFormat.JPEG,
        ImageFormat.PNG,
        ImageFormat.GIF,
        ImageFormat.WebP,
        ImageFormat.BMP,
        ImageFormat.ICO,
    });

    public virtual ImageFormat[] SupportedImageFormats => mSupportedImageFormats.Value;

    public virtual (string filePath, string mime)? TryHandleUploadFile(Stream fileStream) => null;

    protected virtual bool IsConnected => true;

    public virtual Task<bool> IsConnectedAsync() => Task.FromResult(IsConnected);
}
