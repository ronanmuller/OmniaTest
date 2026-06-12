using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Specifications;

public class SaleNotCancelledSpecification : ISpecification<Sale>
{
    public bool IsSatisfiedBy(Sale sale) => !sale.IsCancelled;
}
