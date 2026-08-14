using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AerolineaW.Api.Validation;

/// <summary>
/// Runs every action argument through its registered FluentValidation <see cref="IValidator{T}"/>,
/// if one exists, before the action runs. FluentValidation.AspNetCore (the package that used to
/// wire this up automatically) has been unmaintained since FluentValidation 11 dropped it, so
/// this is the small amount of glue that replaces it — validators are still just
/// <c>AbstractValidator&lt;T&gt;</c>, registered once via <c>AddValidatorsFromAssembly</c>.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            return;
        }

        await next();
    }
}
