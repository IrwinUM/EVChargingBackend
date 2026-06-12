using EVChargingBackend.Application.Abstractions;
using EVChargingBackend.Application.Commands.ChargingSessions;
using EVChargingBackend.Application.Common;
using EVChargingBackend.Application.Queries.Wallets;
using EVChargingBackend.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<CompleteChargingSessionCommandHandler>();
builder.Services.AddScoped<GetWalletBalanceQueryHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CompleteChargingSessionCommandValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

await EnsureDatabaseCreatedAsync(app);

app.MapPost("/sessions/{id:guid}/complete", async (
    Guid id,
    IValidator<CompleteChargingSessionCommand> validator,
    CompleteChargingSessionCommandHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new CompleteChargingSessionCommand(id);
    var validationResult = await validator.ValidateAsync(command, cancellationToken);

    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(ToValidationErrors(validationResult));
    }

    try
    {
        await handler.Handle(command, cancellationToken);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return MapException(ex);
    }
});

app.MapGet("/wallets/{userId:guid}/balance", async (
    Guid userId,
    IValidator<GetWalletBalanceQuery> validator,
    GetWalletBalanceQueryHandler handler,
    CancellationToken cancellationToken) =>
{
    var query = new GetWalletBalanceQuery(userId);
    var validationResult = await validator.ValidateAsync(query, cancellationToken);

    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(ToValidationErrors(validationResult));
    }

    try
    {
        var result = await handler.Handle(query, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapException(ex);
    }
});

app.Run();

static async Task EnsureDatabaseCreatedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

static Dictionary<string, string[]> ToValidationErrors(ValidationResult validationResult)
{
    return validationResult.Errors
        .GroupBy(x => x.PropertyName)
        .ToDictionary(
            group => group.Key,
            group => group.Select(x => x.ErrorMessage).ToArray());
}

static IResult MapException(Exception exception)
{
    return exception.Message switch
    {
        ErrorMessages.SessionNotFound =>
            Results.NotFound(new ErrorResponse(ErrorMessages.SessionNotFound)),
        ErrorMessages.WalletNotFound =>
            Results.NotFound(new ErrorResponse(ErrorMessages.WalletNotFound)),
        ErrorMessages.SessionNotInProgress =>
            Results.Conflict(new ErrorResponse(ErrorMessages.SessionNotInProgress)),
        ErrorMessages.AlreadyCompleted =>
            Results.Conflict(new ErrorResponse(ErrorMessages.AlreadyCompleted)),
        ErrorMessages.InsufficientFunds =>
            Results.UnprocessableEntity(new ErrorResponse(ErrorMessages.InsufficientFunds)),
        _ =>
            Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}

public record ErrorResponse(string Message);

public partial class Program
{
}