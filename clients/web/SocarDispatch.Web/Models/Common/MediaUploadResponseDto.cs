namespace SocarDispatch.Web.Models.Common;

public class MediaUploadResponseDto
{
    public string ObjectKey { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
