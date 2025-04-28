namespace AdvanceFileUpload.API
{

    /// <summary>
    /// Represents the configurable settings for the Kestrel web server.<br></br>
    /// Includes endpoint definitions, connection limits, HTTPS defaults, and HTTP/2 and HTTP/3 specific configurations.
    /// </summary>
    public class KestrelConfiguration
    {
        /// <summary>
        /// The configuration section name for Kestrel server options.
        /// </summary>
        public const string SectionName = "KestrelConfiguration";
        /// <summary>
        /// Gets or sets the dictionary of endpoints for the server.<br></br>
        /// Key represents the endpoint identifier, and value holds the endpoint settings.
        /// </summary>
        public Dictionary<string, EndpointSettings> Endpoints { get; set; } = new();

        /// <summary>
        /// Gets or sets the server-wide connection and request limits.
        /// </summary>
        public KestrelLimits Limits { get; set; } = new();

        /// <summary>
        /// Gets or sets the global HTTPS configuration defaults.
        /// </summary>
        public KestrelHttpsSettings Https { get; set; } = new();

        /// <summary>
        /// Gets or sets the HTTP/2 specific configuration settings.
        /// </summary>
        public KestrelHttp2Settings Http2 { get; set; } = new();

        /// <summary>
        /// Gets or sets the HTTP/3 specific configuration settings.
        /// </summary>
        public KestrelHttp3Settings Http3 { get; set; } = new();
    }

    /// <summary>
    /// Defines the configuration for an individual endpoint, including network binding, security, and protocol options.
    /// </summary>
    public class EndpointSettings
    {
        /// <summary>
        /// Gets or sets the IP address to bind.<br></br>
        /// Use "0.0.0.0" to listen on all network interfaces.
        /// </summary>
        /// <value>Default Value: 0.0.0.0</value>
        public string Ip { get; set; } = "0.0.0.0";

        /// <summary>
        /// Gets or sets the port number on which the server will listen.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether HTTPS is enabled for this endpoint.
        /// </summary>
        public bool Https { get; set; }

        /// <summary>
        /// Gets or sets the allowed HTTP protocols for this endpoint.
        /// Supported values are "Http1", "Http2", and "Http3".
        /// </summary>
        public List<string> Protocols { get; set; } = new() { "Http1", "Http2" };

        /// <summary>
        /// Gets or sets the certificate settings for HTTPS on this endpoint.
        /// </summary>
        public CertificateSettings Certificate { get; set; } = new();

        /// <summary>
        /// Gets or sets the client certificate mode.
        /// Options include: "NoCertificate", "AllowCertificate", "RequireCertificate".
        /// </summary>
        public string ClientCertificateMode { get; set; } = "NoCertificate";

        /// <summary>
        /// Gets or sets the allowed SSL/TLS protocol versions.
        /// Typically includes "Tls12" and "Tls13".
        /// </summary>
        public List<string> SslProtocols { get; set; } = new() { "Tls12", "Tls13" };
    }

    /// <summary>
    /// Represents the certificate configuration options for HTTPS endpoints.
    /// Provides information for locating and authenticating the certificate.
    /// </summary>
    public class CertificateSettings
    {
        /// <summary>
        /// Gets or sets the file path to the certificate (PFX or P12 format).
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the certificate password.
        /// It is recommended to use environment variables or secure stores for production.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the name of the Windows certificate store (e.g., "My", "Root").
        /// </summary>
        public string Store { get; set; }

        /// <summary>
        /// Gets or sets the certificate store location, such as "LocalMachine" or "CurrentUser".
        /// </summary>
        public string Location { get; set; } = "LocalMachine";

        /// <summary>
        /// Gets or sets the subject name for certificate lookup in the store.
        /// </summary>
        public string Subject { get; set; }
    }

    /// <summary>
    /// Describes the server-wide limits for connections and requests.
    /// Controls settings such as timeouts, maximum sizes, and data rates.
    /// </summary>
    public class KestrelLimits
    {
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.KeepAliveTimeout"/>
        /// </summary>
        public TimeSpan KeepAliveTimeout { get; set; }= TimeSpan.FromSeconds(130);

        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.RequestHeadersTimeout"/>
        /// </summary>
        public TimeSpan RequestHeadersTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxConcurrentConnections"/>
        /// </summary>
        public int? MaxConcurrentConnections { get; set; }

        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxConcurrentUpgradedConnections"/>
        /// </summary>
        public int? MaxConcurrentUpgradedConnections { get; set; }

        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxRequestBodySize"/>
        /// </summary>
        public long MaxRequestBodySize { get; set; } = 30_000_000;

        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MinRequestBodyDataRate"/>
        /// </summary>
        public double? MinRequestBodyDataRate { get; set; } = 240;

        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MinResponseDataRate"/>
        /// </summary>
        public double? MinResponseDataRate { get; set; } = 240;
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxResponseBufferSize"/>
        /// </summary>
        public long? MaxResponseBufferSize { get; set; } = 64 * 1024;
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxRequestBufferSize"/>
        /// </summary>
        public long? MaxRequestBufferSize { get; set; } = 1024 * 1024;
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxRequestHeadersTotalSize"/>
        /// </summary>
        public int MaxRequestHeadersTotalSize { get; set; }=32 * 1024;
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxRequestLineSize"/>
        /// </summary>
        public int MaxRequestLineSize { get; set; }=8 * 1024;
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxRequestHeaderCount"/>
        /// </summary>
        public int MaxRequestHeaderCount { get; set; } = 100;
        /// <summary>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions.AllowSynchronousIO"/>
        /// </summary>
        public bool AllowSynchronousIO { get; set; }
    }

    /// <summary>
    /// Represents the global HTTPS configuration defaults for Kestrel.
    /// This configuration is applied if not overridden by endpoints.
    /// </summary>
    public class KestrelHttpsSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the server should check for certificate revocation.
        /// Default is true.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = true;

        /// <summary>
        /// Gets or sets the default client certificate mode.
        /// Options include "NoCertificate", "AllowCertificate", and "RequireCertificate".
        /// </summary>
        public string ClientCertificateMode { get; set; } = "NoCertificate";

        /// <summary>
        /// Gets or sets a list of allowed SSL protocols.
        /// Options include  "None", "Ssl2", "Ssl3", "Tls","Default","Tls11","Tls12", "Tls13"
        /// Defaults typically include "Default", "Tls12" and "Tls13".
        /// </summary>
        public List<string> AllowedProtocols { get; set; } = new() { "Default", "Tls12", "Tls13" };
    }

    /// <summary>
    /// Contains configuration settings specific to HTTP/2.
    /// Manages stream limits, header compression, and frame sizes.
    /// </summary>
    public class KestrelHttp2Settings
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent streams allowed per connection.<br></br>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.Http2Limits.MaxStreamsPerConnection"/>
        /// </summary>
        public int MaxStreamsPerConnection { get; set; } = 100;

        /// <summary>
        /// Gets or sets the size of the HPACK header compression table.
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.Http2Limits.HeaderTableSize"/>
        /// </summary>
        public int HeaderTableSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the maximum allowed frame size in bytes.<br></br>
        /// <inheritdoc cref="Microsoft.AspNetCore.Server.Kestrel.Core.Http2Limits.MaxFrameSize"/>
        /// </summary>
        public int MaxFrameSize { get; set; } = 16_384;
    }

    /// <summary>
    /// Contains configuration settings specific to HTTP/3.
    /// Enables or disables HTTP/3 support on the server.
    /// </summary>
    public class KestrelHttp3Settings
    {
        /// <summary>
        /// Gets or sets a value indicating whether HTTP/3 is enabled.
        /// </summary>
        public bool Enable { get; set; }
    }
}
