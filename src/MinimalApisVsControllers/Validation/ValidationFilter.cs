using System.Net;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MinimalApisVsControllers;


//adapted from: https://benfoster.io/blog/minimal-api-validation-endpoint-filters/
public static class ValidationFilter
{
    public static EndpointFilterDelegate ValidationFilterFactory(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        IEnumerable<ValidationDescriptor> validationDescriptors = GetValidators(context.MethodInfo, context.ApplicationServices);

        if (validationDescriptors.Any())
        {
            return invocationContext => Validate(validationDescriptors, invocationContext, next);
        }

        // pass-thru
        return invocationContext => next(invocationContext);
    }

    private static async ValueTask<object?> Validate(IEnumerable<ValidationDescriptor> validationDescriptors, EndpointFilterInvocationContext invocationContext, EndpointFilterDelegate next)
    {
        foreach (ValidationDescriptor descriptor in validationDescriptors)
        {
            var argument = invocationContext.Arguments[descriptor.ArgumentIndex];

            if (argument is not null)
            {
                var validationResult = await descriptor.Validator.ValidateAsync(
                    new ValidationContext<object>(argument)
                );

                if (!validationResult.IsValid)
                {
                    var problemDetailsFactory = invocationContext.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var modelState = new ModelStateDictionary();
                    
                    validationResult.AddToModelState(modelState);
                    var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(invocationContext.HttpContext, modelState);
                    return TypedResults.Problem(problemDetails);
                }
            }
        }

        return await next.Invoke(invocationContext);
    }

    static IEnumerable<ValidationDescriptor> GetValidators(MethodInfo methodInfo, IServiceProvider serviceProvider)
    {
        ParameterInfo[] parameters = methodInfo.GetParameters();

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            if (parameter.GetCustomAttribute<ValidateAttribute>() is not null)
            {
                Type validatorType = typeof(IValidator<>).MakeGenericType(parameter.ParameterType);

                // Note that FluentValidation validators needs to be registered as singleton
                IValidator? validator = serviceProvider.GetService(validatorType) as IValidator;

                if (validator is not null)
                {
                    yield return new ValidationDescriptor { ArgumentIndex = i, ArgumentType = parameter.ParameterType, Validator = validator };
                }
            }
        }
    }

    private class ValidationDescriptor
    {
        public required int ArgumentIndex { get; init; }
        public required Type ArgumentType { get; init; }
        public required IValidator Validator { get; init; }
    }
}
