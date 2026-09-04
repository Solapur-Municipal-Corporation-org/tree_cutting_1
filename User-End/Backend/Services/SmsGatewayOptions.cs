namespace TreeCutting.Api.Services;

public sealed class SmsGatewayOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://push3.aclgateway.com/servlet/com.aclwireless.pushconnectivity.listeners.TextListener";
    public string AppId { get; set; } = "MahaITsomc";
    public string UserId { get; set; } = "MahaITsomc";
    public string Password { get; set; } = string.Empty;
    public string Sender { get; set; } = "MAHGOV";
    public string CountryCode { get; set; } = "91";
    public string DltTemplateId { get; set; } = "DLT";
    public string MessageTemplate { get; set; } = "Thank You for the Application. Your Application Number is {ApplicationNumber}. Thank you, SMC, Solapur.";
    public string OtpMessageTemplate { get; set; } = "Your OTP Number is {OTP}. Please Enter the OTP to proceed. Thank you, SMC, Solapur.";
    public int OtpExpiryMinutes { get; set; } = 10;
    public int OtpMaxAttempts { get; set; } = 5;
}