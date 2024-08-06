using EventSourcedSandbox.Domain.Maps;
using Marten;

namespace EventSourcedSandbox.Features.Maps;

internal sealed record ViewMapByNameRequest(string MapName);

internal sealed class ViewMapByNameQuery(IQuerySession session)
    : Endpoint<ViewMapByNameRequest, Results<Ok<MapAggregate>, NotFound>>
{
    public override void Configure()
    {
        Get($"/maps");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        ViewMapByNameRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await session
            .Query<MapAggregate>()
            .FirstOrDefaultAsync(x => x.MapName == request.MapName, cancellationToken);

        if (response is null)
        {
            await SendResultAsync(TypedResults.NotFound());
            return;
        }

        await SendResultAsync(TypedResults.Ok(response));
    }
}
