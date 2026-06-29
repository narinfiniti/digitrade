using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class DeletePushDeviceCommand(DeletePushDeviceCommand.Model? input)
    : IUseCase<DeletePushDeviceCommand.Model, Unit>
{
    public Model? Input => input;

    public sealed record Model(string DeviceId);

    public sealed class Handler : IRequestHandler<DeletePushDeviceCommand, Unit>
    {
        public Task<Unit> Handle(DeletePushDeviceCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || string.IsNullOrWhiteSpace(input.DeviceId))
            {
                return Task.FromResult(Unit.Value);
            }

            var key = NotificationContractState.Registrations
                .FirstOrDefault(pair => string.Equals(pair.Value.DeviceId, input.DeviceId, StringComparison.Ordinal))
                .Key;

            if (key != Guid.Empty)
            {
                NotificationContractState.Registrations.TryRemove(key, out _);
            }

            return Task.FromResult(Unit.Value);
        }
    }
}