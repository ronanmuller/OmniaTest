using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ResyncReadModel;

public record ResyncReadModelCommand : IRequest<ResyncReadModelResult>;

public record ResyncReadModelResult(int Synced, int Failed);
