using BffOrchestratorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffOrchestratorService.Application.UseCases;

public sealed class GetBusinessProcessCatalogQuery(GetBusinessProcessCatalogQuery.Model? input)
    : IUseCase<GetBusinessProcessCatalogQuery.Model, IReadOnlyCollection<BusinessProcessDefinitionDto>>
{
    public Model? Input => input;

    public sealed record Model;

    public sealed class Handler : IRequestHandler<GetBusinessProcessCatalogQuery, IReadOnlyCollection<BusinessProcessDefinitionDto>>
    {
        public Task<IReadOnlyCollection<BusinessProcessDefinitionDto>> Handle(GetBusinessProcessCatalogQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<BusinessProcessDefinitionDto> catalog =
            [
                new(
                    "sync-trade-order-risk",
                    "sync",
                    "sync-trade-order-risk",
                    ["Identity", "Account", "Instrument", "Trade", "Order", "Risk"],
                    "Synchronously validates identity, account eligibility, instrument availability, order acceptance, and risk readiness for trade execution."),
                new(
                    "sync-settlement-ledger",
                    "sync",
                    "sync-settlement-ledger",
                    ["Identity", "Account", "Trade", "Order", "Risk", "Settlement", "Ledger"],
                    "Synchronously coordinates settlement lifecycle with ledger posting and account impact checks."),
                new(
                    "sync-portfolio-pricing",
                    "sync",
                    "sync-portfolio-pricing",
                    ["Identity", "Account", "Position", "Portfolio", "Pricing", "Risk"],
                    "Synchronously revalues portfolio positions using pricing and risk dependencies."),
                new(
                    "async-trade-lifecycle",
                    "async",
                    "async-trade-lifecycle",
                    ["Identity", "Account", "Instrument", "Trade", "Order", "Risk", "Settlement", "Ledger"],
                    "Asynchronously progresses trade lifecycle from order intent through settlement and ledger completion."),
                new(
                    "async-risk-rebalance",
                    "async",
                    "async-risk-rebalance",
                    ["Identity", "Account", "Risk", "Position", "Portfolio", "Pricing", "Reporting", "Audit"],
                    "Asynchronously recalculates risk, updates portfolio views, and emits reporting and audit checks."),
                new(
                    "async-post-trade-reporting",
                    "async",
                    "async-post-trade-reporting",
                    ["Identity", "Trade", "Order", "Settlement", "Ledger", "Reporting", "Audit"],
                    "Asynchronously composes post-trade reporting, reconciles settlement/ledger state, and records audit artifacts."),
            ];

            return Task.FromResult(catalog);
        }
    }
}