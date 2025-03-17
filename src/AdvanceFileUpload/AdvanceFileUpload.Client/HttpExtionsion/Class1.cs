using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client.HttpExtensions
{
    public sealed class ProgressMessageHandler : DelegatingHandler
    {
        // Key to store buffer size in request options
        private static readonly HttpRequestOptionsKey<int> BufferSizeOptionKey =
            new HttpRequestOptionsKey<int>("ProgressBufferSize");

        public event Action<long, long, int> ProgressChanged; // (sent, total, bufferSize)

        public ProgressMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is null)
                return await base.SendAsync(request, cancellationToken);

            // Get buffer size from request options (or use default)
            request.Options.TryGetValue(BufferSizeOptionKey, out int bufferSize);
            bufferSize = bufferSize > 0 ? bufferSize : 4096;

            var progressContent = new ProgressableStreamContent(
                request.Content,
                bufferSize,
                (sent, total) => ProgressChanged?.Invoke(sent, total, bufferSize)
            );

            request.Content = progressContent;
            return await base.SendAsync(request, cancellationToken);
        }
    }

    public sealed class ProgressableStreamContent : HttpContent
    {
        private readonly HttpContent _content;
        private readonly int _bufferSize;
        private readonly Action<long, long> _progress;

        public ProgressableStreamContent(
            HttpContent content,
            int bufferSize,
            Action<long, long> progress)
        {
            _content = content;
            _bufferSize = bufferSize > 0 ? bufferSize : 4096;
            _progress = progress;

            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            long totalUploaded = 0;
            var totalLength = Headers.ContentLength ?? -1;

            try
            {
                await using var contentStream = await _content.ReadAsStreamAsync();
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalUploaded += bytesRead;
                    _progress?.Invoke(totalUploaded, totalLength);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Headers.ContentLength ?? -1;
            return _content.Headers.ContentLength.HasValue;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _content.Dispose();
            base.Dispose(disposing);
        }
    }
}
