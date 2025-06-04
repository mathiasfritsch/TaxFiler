using Refit;

namespace LlamaParse;

public interface ILlamaExtractClient
{
    [Multipart]
    [Post("/api/v1/files")]
    Task<HttpResponseMessage> UploadFileAsync(
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);
}