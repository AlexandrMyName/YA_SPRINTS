namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Endpoints;


public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/minimal/products")
            .WithTags("Products")
            .WithOpenApi();

        group.MapGet("/", () => new[] { "Product1", "Product2" });
        group.MapGet("/{id}", (int id) => $"Product {id}");
        group.MapPost("/", (string product) =>
            Results.Created($"/api/products/{product}", product));
        group.MapPut("/{id}", (int id, string product) => Results.Ok(product));
        group.MapDelete("/{id}", (int id) => Results.NoContent());

        return app;
    }
}


 