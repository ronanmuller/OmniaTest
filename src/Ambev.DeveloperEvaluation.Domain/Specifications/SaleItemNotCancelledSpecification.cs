using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Specifications;

public class SaleItemNotCancelledSpecification : ISpecification<SaleItem>
{
    public bool IsSatisfiedBy(SaleItem item) => !item.IsCancelled;
}
