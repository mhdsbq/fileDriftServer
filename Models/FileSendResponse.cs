class FileSendResponse
{
    public string FileSenderId { get; set; } = string.Empty;
    public string FileReceiverId { get; set; } = string.Empty;
    public FileInfo FileInfo { get; set; } = new();
    public ResponseStatus ResponseStatus { get; set; } = ResponseStatus.Unknown;
}