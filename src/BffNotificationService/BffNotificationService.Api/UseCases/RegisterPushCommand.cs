using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class RegisterPushCommand(RegisterPushCommand.Model? input)
    : IUseCase<RegisterPushCommand.Model, PushRegistrationDto>
{
    public Model? Input => input;

    public sealed record Model(PushRegistrationInput Request);

    public sealed class Handler(TimeProvider timeProvider) : IRequestHandler<RegisterPushCommand, PushRegistrationDto>
    {
        public Task<PushRegistrationDto> Handle(RegisterPushCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input?.Request ?? throw new ArgumentNullException(nameof(request));
            var registration = new PushRegistrationDto(
                Guid.NewGuid(),
                input.UserId,
                input.DeviceId,
                input.Platform,
                true,
                input.Categories.Distinct(StringComparer.Ordinal).ToArray(),
                timeProvider.GetUtcNow());

            NotificationContractState.Registrations[registration.RegistrationId] = registration;
            return Task.FromResult(registration);
        }
    }
}