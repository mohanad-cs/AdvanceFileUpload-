using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Integration.Contracts
{
  
    /// <summary>
    /// Contains constants used for integration purposes.
    /// </summary>
    public sealed class IntegrationConstants
    {
        /// <summary>
        /// Constants related to the "Session Created" event.
        /// </summary>
        public sealed class SessionCreatedConstants
        {
            /// <summary>
            /// The name of the queue for the "Session Created" event.
            /// </summary>
            public const string Queue = "Session-Created";
            /// <summary>
            /// The routing key for the "Session Created" event.
            /// </summary>
            public const string RoutingKey = "Session-Created";
            /// <summary>
            /// The exchange name for the "Session Created" event.
            /// </summary>
            public const string Exchange = "Session-Created";
            /// <summary>
            /// The type of exchange for the "Session Created" event.
            /// </summary>
            public const string ExchangeType = "direct";
            /// <summary>
            /// Indicates whether the queue is durable for the "Session Created" event.
            /// </summary>
            public const bool Durable = true;
            /// <summary>
            /// Indicates whether the queue is auto-deleted for the "Session Created" event.
            /// </summary>
            public const bool AutoDelete = false;
            /// <summary>
            /// Indicates whether the queue is exclusive for the "Session Created" event.
            /// </summary>
            public const bool Exclusive = false;
        }

        /// <summary>
        /// Constants related to the "Session Canceled" event.
        /// </summary>
        public sealed class SessionCanceledConstants
        {
            /// <summary>
            /// The name of the queue for the "Session Canceled" event.
            /// </summary>
            public const string Queue = "Session-Canceled";
            /// <summary>
            /// The routing key for the "Session Canceled" event.
            /// </summary>
            public const string RoutingKey = "Session-Canceled";
            /// <summary>
            /// The exchange name for the "Session Canceled" event.
            /// </summary>
            public const string Exchange = "Session-Canceled";
            /// <summary>
            /// The type of exchange for the "Session Canceled" event.
            /// </summary>
            public const string ExchangeType = "direct";
            /// <summary>
            /// Indicates whether the queue is durable for the "Session Canceled" event.
            /// </summary>
            public const bool Durable = true;
            /// <summary>
            /// Indicates whether the queue is auto-deleted for the "Session Canceled" event.
            /// </summary>
            public const bool AutoDelete = false;
            /// <summary>
            /// Indicates whether the queue is exclusive for the "Session Canceled" event.
            /// </summary>
            public const bool Exclusive = false;
        }

        /// <summary>
        /// Constants related to the "Session Completed" event.
        /// </summary>
        public sealed class SessionCompletedConstants
        {
            /// <summary>
            /// The name of the queue for the "Session Completed" event.
            /// </summary>
            public const string Queue = "Session-Completed";
            /// <summary>
            /// The routing key for the "Session Completed" event.
            /// </summary>
            public const string RoutingKey = "Session-Completed";
            /// <summary>
            /// The exchange name for the "Session Completed" event.
            /// </summary>
            public const string Exchange = "Session-Completed";
            /// <summary>
            /// The type of exchange for the "Session Completed" event.
            /// </summary>
            public const string ExchangeType = "direct";
            /// <summary>
            /// Indicates whether the queue is durable for the "Session Completed" event.
            /// </summary>
            public const bool Durable = true;
            /// <summary>
            /// Indicates whether the queue is auto-deleted for the "Session Completed" event.
            /// </summary>
            public const bool AutoDelete = false;
            /// <summary>
            /// Indicates whether the queue is exclusive for the "Session Completed" event.
            /// </summary>
            public const bool Exclusive = false;
        }

        /// <summary>
        /// Constants related to the "Chunk Uploaded" event.
        /// </summary>
        public sealed class ChunkUploadedConstants
        {
            /// <summary>
            /// The name of the queue for the "Chunk Uploaded" event.
            /// </summary>
            public const string Queue = "Chunk-Uploaded";
            /// <summary>
            /// The routing key for the "Chunk Uploaded" event.
            /// </summary>
            public const string RoutingKey = "Chunk-Uploaded";
            /// <summary>
            /// The exchange name for the "Chunk Uploaded" event.
            /// </summary>
            public const string Exchange = "Chunk-Uploaded";
            /// <summary>
            /// The type of exchange for the "Chunk Uploaded" event.
            /// </summary>
            public const string ExchangeType = "direct";
            /// <summary>
            /// Indicates whether the queue is durable for the "Chunk Uploaded" event.
            /// </summary>
            public const bool Durable = true;
            /// <summary>
            /// Indicates whether the queue is auto-deleted for the "Chunk Uploaded" event.
            /// </summary>
            public const bool AutoDelete = false;
            /// <summary>
            /// Indicates whether the queue is exclusive for the "Chunk Uploaded" event.
            /// </summary>
            public const bool Exclusive = false;
        }

        /// <summary>
        /// Constants related to the "Session Paused" event.
        /// </summary>
        public sealed class SessionPausedConstants
        {
            /// <summary>
            /// The name of the queue for the "Session Paused" event.
            /// </summary>
            public const string Queue = "Session-Paused";
            /// <summary>
            /// The routing key for the "Session Paused" event.
            /// </summary>
            public const string RoutingKey = "Session-Paused";
            /// <summary>
            /// The exchange name for the "Session Paused" event.
            /// </summary>
            public const string Exchange = "Session-Paused";
            /// <summary>
            /// The type of exchange for the "Session Paused" event.
            /// </summary>
            public const string ExchangeType = "direct";
            /// <summary>
            /// Indicates whether the queue is durable for the "Session Paused" event.
            /// </summary>
            public const bool Durable = true;
            /// <summary>
            /// Indicates whether the queue is auto-deleted for the "Session Paused" event.
            /// </summary>
            public const bool AutoDelete = false;
            /// <summary>
            /// Indicates whether the queue is exclusive for the "Session Paused" event.
            /// </summary>
            public const bool Exclusive = false;
        }

        /// <summary>
        /// Constants related to the "Session Resumed" event.
        /// </summary>
        public sealed class SessionResumedConstants
        {
            /// <summary>
            /// The name of the queue for the "Session Resumed" event.
            /// </summary>
            public const string Queue = "Session-Resumed";
            /// <summary>
            /// The routing key for the "Session Resumed" event.
            /// </summary>
            public const string RoutingKey = "Session-Resumed";
            /// <summary>
            /// The exchange name for the "Session Resumed" event.
            /// </summary>
            public const string Exchange = "Session-Resumed";
            /// <summary>
            /// The type of exchange for the "Session Resumed" event.
            /// </summary>
            public const string ExchangeType = "direct";
            /// <summary>
            /// Indicates whether the queue is durable for the "Session Resumed" event.
            /// </summary>
            public const bool Durable = true;
            /// <summary>
            /// Indicates whether the queue is auto-deleted for the "Session Resumed" event.
            /// </summary>
            public const bool AutoDelete = false;
            /// <summary>
            /// Indicates whether the queue is exclusive for the "Session Resumed" event.
            /// </summary>
            public const bool Exclusive = false;
        }
    }
}
