using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Common.Security;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddValidatorsFromAssemblyContaining<ApplicationLayer>();
    }
}