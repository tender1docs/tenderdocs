using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace TenderDocs.Api.Controllers;

/// <summary>
/// Streams content produced by a callback directly to the response body, so large ZIPs
/// are never buffered fully in memory. Used by the project-bundle download endpoint.
/// </summary>
public class FileCallbackResult : IActionResult
{
    private readonly string _contentType;
    private readonly string _fileName;
    private readonly Func<Stream, CancellationToken, Task> _callback;

    public FileCallbackResult(string contentType, string fileName,
        Func<Stream, CancellationToken, Task> callback)
        => (_contentType, _fileName, _callback) = (contentType, fileName, callback);

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = _contentType;

        if (!response.Headers.ContainsKey(HeaderNames.ContentDisposition))
        {
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = _fileName,
                Inline = false
            };
            response.Headers.Append(HeaderNames.ContentDisposition, cd.ToString());
        }

        await _callback(response.Body, context.HttpContext.RequestAborted);
    }
}
