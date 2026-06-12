using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Common.Validation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors.Where(f => f != null));
        }

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}