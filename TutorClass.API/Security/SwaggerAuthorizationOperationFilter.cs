using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TutorClass.API.Security;

public sealed class SwaggerAuthorizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();

        if (allowsAnonymous || !requiresAuthorization)
        {
            // An empty operation requirement overrides the document's Bearer default.
            operation.Security = [];
        }
    }
}
