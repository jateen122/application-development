namespace VehiclePartsAPI.Models
{
    public class ExternalServicesOptions
    {
        // Payment API (optional)
        public string? PaymentApiUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? MerchantId { get; set; }
        public string? MerchantName { get; set; }

        // Timeout configuration
        public int TimeoutSeconds { get; set; } = 30;

        // Email Service (optional - useful for invoices feature)
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string? EmailFrom { get; set; }
        public string? EmailPassword { get; set; }

        // SMS or Notification Service (optional)
        public string? SmsApiKey { get; set; }
        public string? SmsSenderId { get; set; }
    }
}