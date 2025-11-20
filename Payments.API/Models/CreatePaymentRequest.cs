using System.ComponentModel.DataAnnotations;

namespace Transational.Api.Web.Models;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "CustomerId is required")]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "ServiceProviderId is required")]
    public Guid ServiceProviderId { get; set; }

    [Required(ErrorMessage = "PaymentMethodId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PaymentMethodId must be greater than 0")]
    public int PaymentMethodId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}
