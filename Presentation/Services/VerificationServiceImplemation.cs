using Grpc.Core;
using VerificationServiceGrpcServer;

namespace Presentation.Services;

public class VerificationServiceImplemation : VerificationServiceProto.VerificationServiceProtoBase
{
    public override Task<ValidateEmailResponse> ValidateEmail(ValidateEmailRequest request, ServerCallContext context)
    {
        return Task.FromResult(new ValidateEmailResponse
        {
            Success = true,
            Message = "Email validated successfully",
            Email = request.Email
        });
    }

    public override Task<ConfirmUserEmailResponse> ConfirmUserEmail(ConfirmUserEmailRequest request, ServerCallContext context)
    {
        return Task.FromResult(new ConfirmUserEmailResponse
        {
            Success = true,
            Message = "Email confirmed successfully"
        });
    }
}

