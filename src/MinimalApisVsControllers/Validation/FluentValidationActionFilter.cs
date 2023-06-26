using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MinimalApisVsControllers;

public class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public FluentValidationActionFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory)
    {
        _serviceProvider = serviceProvider;
        _problemDetailsFactory = problemDetailsFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var parameters = context.ActionDescriptor.Parameters;
        if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var methodParameters = controllerActionDescriptor.MethodInfo.GetParameters();
            foreach (var parameter in parameters)
            {
                if (!context.ActionArguments.ContainsKey(parameter.Name))
                {
                    continue;
                }
                
                var argument = context.ActionArguments[parameter.Name];
                if (argument != null)
                {
                    var paramInfo = methodParameters.Single(p => p.Name == parameter.Name);
                    var attribute = paramInfo.GetCustomAttribute<ValidateAttribute>();

                    if (attribute != null)
                    {
                        var validatorType = typeof(IValidator<>).MakeGenericType(parameter.ParameterType);
                        if (_serviceProvider.GetService(validatorType) is IValidator validator)
                        {
                            var validationResult =
                                await validator.ValidateAsync(new ValidationContext<object>(argument));
                            if (!validationResult.IsValid)
                            {
                                validationResult.AddToModelState(context.ModelState);
                                var problemDetails =
                                    _problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext,
                                        context.ModelState);
                                context.Result = new BadRequestObjectResult(problemDetails);

                                return;
                            }
                        }
                    }
                }
            }
        }
        await next();
    }
}
