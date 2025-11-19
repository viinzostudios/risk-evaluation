using AutoMapper;
using Transational.Api.Application.Commands.CreatePayment;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Entities;

namespace Transational.Api.Application.Mappings;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        // CreatePaymentCommand → Payment entity
        CreateMap<CreatePaymentCommand, Payment>()
            .ForMember(dest => dest.CustomerId,
                opt => opt.MapFrom(src => src.CustomerId.ToByteArray()))
            .ForMember(dest => dest.ServiceProviderId,
                opt => opt.MapFrom(src => src.ServiceProviderId.ToByteArray()))
            .ForMember(dest => dest.PaymentMethodId,
                opt => opt.MapFrom(src => src.PaymentMethodId))
            .ForMember(dest => dest.Amount,
                opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalOperationId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentStatusId, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore());

        // Payment entity → PaymentResponse DTO
        CreateMap<Payment, PaymentResponse>()
            .ForMember(dest => dest.ExternalOperationId,
                opt => opt.MapFrom(src => new Guid(src.ExternalOperationId)))
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.PaymentStatus != null ? src.PaymentStatus.Name : "evaluating"));
    }
}
