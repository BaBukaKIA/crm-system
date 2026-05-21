namespace EnterpriseAutomation.Services.Security;

public interface ITextProtector
{
    string Protect(string plainText);

    string? TryUnprotect(string? protectedText);
}
