using AdvanceFileUpload.Application.Request;
using System;
namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// A builder class for configuring and creating instances of <see cref="FileUploadService"/>.
    /// </summary>
    public sealed class FileUploadBuilder
    {
        private  Uri _baseUrl;
        private string _apiKey= string.Empty;
        private int _maxConcurrentUploads =4;
        private int _maxRetriesCount =3;
        private string _tempDirectory = Path.GetTempPath();
        private CompressionOption? _compressionOption;
        private List<string> _excludedCompressionExtensions = new();
        private FileUploadBuilder(Uri baseUri)
        {
            _baseUrl = baseUri;
        }
        /// <summary>
        /// Creates a new instance of <see cref="FileUploadBuilder"/> with the specified base URL.
        /// </summary>
        /// <param name="baseUri">The base URL for the file upload service.</param>
        /// <returns>A new instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="baseUri"/> is null or empty.</exception>
        public static FileUploadBuilder New(string baseUri)
        {
            if (string.IsNullOrWhiteSpace(baseUri))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUri));
            }
            Uri uri = new Uri(baseUri);
            return new FileUploadBuilder(uri);
           
        }

        /// <summary>
        /// Creates a new instance of <see cref="FileUploadBuilder"/> with the specified base URL.
        /// </summary>
        /// <param name="baseUrl">The base URL for the file upload service.</param>
        /// <returns>A new instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="baseUrl"/> is null.</exception>
        public static FileUploadBuilder New(Uri baseUrl)
        {
            if (baseUrl == null)
            {
                throw new ArgumentException("Base URL cannot be null.", nameof(baseUrl));
            }
            return new FileUploadBuilder(baseUrl);

        }

        /// <summary>
        /// Creates a new instance of <see cref="FileUploadBuilder"/> with the specified base URL and API key.
        /// </summary>
        /// <param name="baseUrl">The base URL for the file upload service.</param>
        /// <param name="apiKey">The API key to use for authentication.</param>
        /// <returns>A new instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="baseUrl"/> is null or <paramref name="apiKey"/> is null or empty.</exception>
        public static FileUploadBuilder New(Uri baseUrl, string apiKey)
        {
            if (baseUrl == null)
            {
                throw new ArgumentException("Base URL cannot be null.", nameof(baseUrl));
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            }
            return new FileUploadBuilder(baseUrl)
            {
                _apiKey = apiKey,
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="FileUploadBuilder"/> with the specified base URL and API key.
        /// </summary>
        /// <param name="baseUrl">The base URL for the file upload service as a string.</param>
        /// <param name="apiKey">The API key to use for authentication.</param>
        /// <returns>A new instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="baseUrl"/> is null or empty, or if <paramref name="apiKey"/> is null or empty.</exception>
        public static FileUploadBuilder New(string baseUrl, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            }
            Uri uri = new Uri(baseUrl);
            return new FileUploadBuilder(uri)
            {
                _apiKey = apiKey,
            };
        }
        /// <summary>
        /// Sets the API key for the file upload service.
        /// </summary>
        /// <param name="apiKey">The API key to use for authentication.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="apiKey"/> is null or empty.</exception>
        public FileUploadBuilder WithAPIKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            }
            _apiKey = apiKey;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent uploads.
        /// </summary>
        /// <param name="maxConcurrentUploads">The maximum number of concurrent uploads.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="maxConcurrentUploads"/> is less than or equal to zero.</exception>
        public FileUploadBuilder WithMaxConcurrentUploads(int maxConcurrentUploads)
        {
            if (maxConcurrentUploads <= 0)
            {
                throw new ArgumentException("Max concurrent uploads must be greater than zero.", nameof(maxConcurrentUploads));
            }
            _maxConcurrentUploads = maxConcurrentUploads;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of retry attempts for failed uploads.
        /// </summary>
        /// <param name="maxRetriesCount">The maximum number of retry attempts.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="maxRetriesCount"/> is negative.</exception>
        public FileUploadBuilder WithMaxRetriesCount(int maxRetriesCount)
        {
            if (maxRetriesCount < 0)
            {
                throw new ArgumentException("Max retries count cannot be negative.", nameof(maxRetriesCount));
            }
            _maxRetriesCount = maxRetriesCount;
            return this;
        }

        /// <summary>
        /// Sets the temporary directory for file uploads.
        /// </summary>
        /// <param name="tempDirectory">The path to the temporary directory.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="tempDirectory"/> is null or empty.</exception>
        public FileUploadBuilder WithTempDirectory(string tempDirectory)
        {
            if (string.IsNullOrWhiteSpace(tempDirectory))
            {
                throw new ArgumentException("Temporary directory cannot be null or empty.", nameof(tempDirectory));
            }
            _tempDirectory = tempDirectory;
            return this;
        }

        /// <summary>
        /// Sets the compression options for the file upload service.
        /// </summary>
        /// <param name="compressionOption">The compression options to use.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        public FileUploadBuilder WithCompressionOption(Func<CompressionOption> compressionOption)
        {
            _compressionOption =compressionOption();
            return this;
        }
        /// <summary>
        /// Sets the compression options for the file upload service.
        /// </summary>
        /// <param name="compressionOption">The compression options to use.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        public FileUploadBuilder WithCompressionOption(CompressionOption compressionOption)
        {
            _compressionOption = compressionOption;
            return this;
        }

        /// <summary>
        /// Excludes a specific file extension from compression.
        /// </summary>
        /// <param name="extension">The file extension to exclude.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="extension"/> is null or empty.</exception>
        public FileUploadBuilder ExcludeCompressionExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("Extension cannot be null or empty.", nameof(extension));
            }
            _excludedCompressionExtensions.Add(extension);
            return this;
        }

        /// <summary>
        /// Excludes multiple file extensions from compression.
        /// </summary>
        /// <param name="extensions">The collection of file extensions to exclude.</param>
        /// <returns>The current instance of <see cref="FileUploadBuilder"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="extensions"/> is null or empty.</exception>
        public FileUploadBuilder ExcludeCompressionExtensions(IEnumerable<string> extensions)
        {
            if (extensions == null || !extensions.Any())
            {
                throw new ArgumentException("Extensions cannot be null or empty.", nameof(extensions));
            }
            _excludedCompressionExtensions.AddRange(extensions);
            return this;
        }

        /// <summary>
        /// Builds and returns a new instance of <see cref="FileUploadService"/> based on the configured options.
        /// </summary>
        /// <returns>A new instance of <see cref="FileUploadService"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the API key is not set.</exception>
        public FileUploadService Build()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("API key is required.");
            }
            UploadOptions uploadOptions = new UploadOptions()
            {
                APIKey = _apiKey,
                CompressionOption = _compressionOption,
                ExcludedCompressionExtensions = _excludedCompressionExtensions,
                MaxConcurrentUploads = _maxConcurrentUploads,
                MaxRetriesCount = _maxRetriesCount,
                TempDirectory = _tempDirectory
            };
            return new FileUploadService(_baseUrl, uploadOptions);
        }
    }
}
