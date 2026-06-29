using MediatR;

namespace DigiTrade.SharedKernel.Abstractions;


/// <summary>
/// Abstraction for creating a command/query use-case.
/// </summary>
public interface IUseCase<out TInputModel, out TOutputModel> : IRequest<TOutputModel>
{
    TInputModel? Input { get; }
}

/// <summary>
/// Abstraction for creating a command.
/// </summary>
public interface IUseCase<out TInputModel> : IRequest
{
    TInputModel? Input { get; }
}