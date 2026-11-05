using System.ComponentModel.DataAnnotations;

namespace SupportTicketTriage.Application.Contracts;

public sealed class CreateTicketRequest : IValidatableObject
{
    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var name = CustomerName?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 200)
        {
            yield return new ValidationResult(
                "CustomerName is required and must be 1-200 characters after trimming.",
                [nameof(CustomerName)]);
        }

        var email = Email?.Trim() ?? string.Empty;
        if (email.Length is < 3 or > 254)
        {
            yield return new ValidationResult(
                "Email is required and must be a syntactically valid address of at most 254 characters.",
                [nameof(Email)]);
        }

        var description = Description?.Trim() ?? string.Empty;
        if (description.Length is < 10 or > 5000)
        {
            yield return new ValidationResult(
                "Description is required and must be 10-5000 characters after trimming.",
                [nameof(Description)]);
        }
    }
}
