public class SignalMessage
{
    public required string SenderId { get; set; }
    public required string ReceiverId { get; set; }
    public MessageType MessageType { get; set; }
    public required PayloadBase Payload { get; set; }
    public required long? SentAt { get; set; } // Added by server when sending back
}

public enum MessageType
{
    TransferRequest,
    TransferResponse,
    TransferComplete
}

public class PayloadBase
{
    public required string OperationId { get; set; }
}

public class TransferRequest : PayloadBase
{
    public required string ItemName { get; set; }
    public int ItemSizeBytes { get; set; }
}

public class TransferResponse : PayloadBase
{
    public bool IsApproved { get; set; }
    public required string RejectionReason { get; set; } // Optional field for rejection reason
}

public class TransferComplete : PayloadBase
{
    public bool IsSuccessful { get; set; }
    public int DurationSeconds { get; set; }
}
