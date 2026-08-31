namespace SocarDispatch.Infrastructure.Settings;

public class MinioSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "miniopassword";
    public string BucketName { get; set; } = "socar-dispatch-media";
    public string FeedbackBucketName { get; set; } = "socar-dispatch-feedbacks";
    public bool UseSSL { get; set; } = false;
    public string PublicEndpoint { get; set; } = "http://localhost:9000";
}
