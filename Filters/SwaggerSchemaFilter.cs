using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Filters
{
    public class SwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(DateTime) || context.Type == typeof(DateTime?))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiString("2025-06-22T10:57:52Z");
            }

            if (context.Type.Name.Contains("LoginDto"))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("admin@example.com"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("Admin123!@#"),
                    ["recaptchaToken"] = new Microsoft.OpenApi.Any.OpenApiString("optional-recaptcha-token")
                };
            }

            if (context.Type.Name.Contains("RegisterDto"))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("user@example.com"),
                    ["password"] = new Microsoft.OpenApi.Any.OpenApiString("User123!@#"),
                    ["firstName"] = new Microsoft.OpenApi.Any.OpenApiString("John"),
                    ["lastName"] = new Microsoft.OpenApi.Any.OpenApiString("Doe"),
                    ["recaptchaToken"] = new Microsoft.OpenApi.Any.OpenApiString("recaptcha-token")
                };
            }
        }
    }
}
