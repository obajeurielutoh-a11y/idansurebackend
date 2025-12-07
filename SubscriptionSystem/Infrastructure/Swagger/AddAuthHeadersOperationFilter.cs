using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SubscriptionSystem.Infrastructure.Swagger
{
    public class AddAuthHeadersOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.MethodInfo;
            var type = method.DeclaringType;

            var methodAuth = method.GetCustomAttributes(true).OfType<AuthorizeAttribute>();
            var typeAuth = (type?.GetCustomAttributes(true).OfType<AuthorizeAttribute>()) ?? Enumerable.Empty<AuthorizeAttribute>();
            var authorizeAttributes = methodAuth.Concat(typeAuth).ToList();

            if (!authorizeAttributes.Any())
            {
                return;
            }

            var policies = authorizeAttributes
                .Select(a => a.Policy)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (policies.Contains("UserWithIdHeader"))
            {
                operation.Parameters ??= new List<OpenApiParameter>();
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-User-Id",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "User ID header must match the token's NameIdentifier (sub)."
                });
            }

            if (policies.Contains("AdminWithIdHeader"))
            {
                operation.Parameters ??= new List<OpenApiParameter>();
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Admin-Id",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Admin ID header must match the token's NameIdentifier (sub)."
                });
            }
        }
    }
}
