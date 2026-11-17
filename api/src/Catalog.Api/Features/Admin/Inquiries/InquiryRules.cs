using System.Net.Mail;
using Catalog.Api.Data;

namespace Catalog.Api.Features.Admin.Inquiries;

public sealed record InquiryProduct(Guid Id, string Name, string Slug);

public sealed record InquiryResponse(
    Guid Id, string Name, string Email, string Message, InquiryStatus Status, DateTimeOffset CreatedAt, InquiryProduct Product);

public static class InquiryRules
{
    public static bool IsValidEmail(string email) =>
        email.Length <= Inquiry.EmailMaxLength
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email
        && address.Host.Contains('.', StringComparison.Ordinal);
}
