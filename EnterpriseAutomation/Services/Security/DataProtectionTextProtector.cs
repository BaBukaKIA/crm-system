using Microsoft.AspNetCore.DataProtection;

namespace EnterpriseAutomation.Services.Security;

public sealed class DataProtectionTextProtector : ITextProtector
{
    private const string Prefix = "enc:";
    private readonly IDataProtector _protector;

    public DataProtectionTextProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("EnterpriseAutomation.SensitiveSecrets");
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        return Prefix + _protector.Protect(plainText);
    }

    public string? TryUnprotect(string? protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return protectedText;
        }

        if (!protectedText.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return protectedText;
        }

        try
        {
            return _protector.Unprotect(protectedText[Prefix.Length..]);
        }
        catch
        {
            return protectedText;
        }
    }
}
