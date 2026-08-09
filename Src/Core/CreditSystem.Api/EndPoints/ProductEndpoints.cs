using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Credit Products")
            .WithOpenApi();

        group.MapGet("/", GetAllProducts)
            .WithName("GetAllProducts")
            .WithSummary("List all active credit products")
            .Produces<IEnumerable<ProductResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Get a credit product by ID")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new credit product")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/status", UpdateProductStatus)
            .WithName("UpdateProductStatus")
            .WithSummary("Update the status of a credit product")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllProducts(
        [FromServices] ICreditProductRepository repository,
        CancellationToken cancellationToken)
    {
        var products = await repository.GetAllActiveAsync(cancellationToken);
        return Results.Ok(products.Select(ToResponse));
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        [FromServices] ICreditProductRepository repository,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        return product == null ? Results.NotFound() : Results.Ok(ToResponse(product));
    }

    private static async Task<IResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        [FromServices] ICreditProductRepository repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Product name is required");
        if (request.MinAmount >= request.MaxAmount)
            return Results.BadRequest("MinAmount must be less than MaxAmount");
        if (request.MinTermMonths >= request.MaxTermMonths)
            return Results.BadRequest("MinTermMonths must be less than MaxTermMonths");

        var limits = new ProductLimits(request.MinAmount, request.MaxAmount, request.MinTermMonths, request.MaxTermMonths);
        var rates = new ProductRates(request.BaseInterestRate, request.MaxLtv);

        var product = new CreditProduct(
            Guid.NewGuid(),
            request.Name,
            limits,
            rates,
            request.DefaultAmortizationMethod,
            request.RequiresCollateral);

        await repository.InsertAsync(product, cancellationToken);

        return Results.Created($"/api/products/{product.Id}", product.Id);
    }

    private static async Task<IResult> UpdateProductStatus(
        Guid id,
        [FromBody] UpdateProductStatusRequest request,
        [FromServices] ICreditProductRepository repository,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product == null) return Results.NotFound();

        await repository.UpdateStatusAsync(id, request.Status, cancellationToken);
        return Results.NoContent();
    }

    private static ProductResponse ToResponse(CreditProduct p) => new(
        p.Id,
        p.Name,
        p.Limits.MinAmount,
        p.Limits.MaxAmount,
        p.Limits.MinTermMonths,
        p.Limits.MaxTermMonths,
        p.Rates.BaseInterestRate,
        p.Rates.MaxLtv,
        p.DefaultAmortizationMethod.ToString(),
        p.RequiresCollateral,
        p.Status.ToString());
}

public record CreateProductRequest(
    string Name,
    decimal MinAmount,
    decimal MaxAmount,
    int MinTermMonths,
    int MaxTermMonths,
    decimal? BaseInterestRate,
    decimal? MaxLtv,
    AmortizationMethod DefaultAmortizationMethod,
    bool RequiresCollateral);

public record UpdateProductStatusRequest(ProductStatus Status);

public record ProductResponse(
    Guid Id,
    string Name,
    decimal MinAmount,
    decimal MaxAmount,
    int MinTermMonths,
    int MaxTermMonths,
    decimal? BaseInterestRate,
    decimal? MaxLtv,
    string DefaultAmortizationMethod,
    bool RequiresCollateral,
    string Status);
