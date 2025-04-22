using System;

namespace Pipelink.Outbox.Models
{
    /// <summary>
    /// Represents a message entity used in the Outbox pattern for ensuring reliable message processing.
    /// </summary>
    /// <remarks>
    /// The <c>OutboxMessage</c> class encapsulates details about a message that is
    /// stored in the outbox for eventual processing or delivery. It provides properties
    /// to track the message payload, type, processing status, timestamps, retry attempts,
    /// and error details.
    /// This class is typically used in conjunction with an outbox database context to
    /// ensure messages are reliably persisted and processed in the context of distributed systems.
    /// </remarks>
    public class OutboxMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for the outbox message.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Represents the type of the message being handled by the outbox system.
        /// This property is required and is used to identify or categorize the message.
        /// </summary>
        public required string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the content of the outbox message to be processed or delivered.
        /// This property typically holds the serialized data that represents the message.
        /// </summary>
        public required string Payload { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was created.
        /// </summary>
        /// <remarks>
        /// The CreatedAt property records the timestamp at which the message is added to the outbox.
        /// It is typically set to the current UTC date and time during the message creation process.
        /// This property is required and is used for tracking and auditing purposes.
        /// </remarks>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp indicating when the message was processed.
        /// A null value indicates that the message has not yet been processed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Represents the current status of the outbox message.
        /// </summary>
        /// <remarks>
        /// This property is used to track the state of a message within the outbox mechanism, such as "Pending", "Processed", or "Failed".
        /// The status is required and must be updated appropriately within the message lifecycle.
        /// </remarks>
        public required string Status { get; set; } = "Pending";

        /// <summary>
        /// Represents the number of times the message processing has been retried.
        /// This property is used to track and control the retry attempts for processing
        /// the outbox message in case of failure.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the error message associated with the processing status of the Outbox message.
        /// </summary>
        /// <remarks>
        /// This property holds any error information encountered during the processing of the message.
        /// It is optional and may be null if no errors occurred.
        /// </remarks>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the last error message associated with the processing status of the Outbox message.
        /// </summary>
        /// <remarks>
        /// This property holds any error information encountered during the processing of the message.
        /// It is optional and may be null if no errors occurred.
        /// </remarks>
        public string? LastError { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the last error occurred.
        /// </summary>
        /// <remarks>
        /// This property records the timestamp when the last error occurred.
        /// It is optional and may be null if no errors occurred.
        /// </remarks>
        public DateTime? LastErrorAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message has been processed.
        /// </summary>
        public bool IsProcessed { get; set; }
    }
} 