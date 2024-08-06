using EventSourcedSandbox.Domain.Maps;
using Marten;

namespace EventSourcedSandbox.Features.Maps;

internal sealed record ViewMapRequest(Guid MapId);

internal sealed class ViewMapQuery(IQuerySession session)
    : Endpoint<ViewMapRequest, Results<Ok<MapAggregate>, NotFound>>
{
    public override void Configure()
    {
        Get($"/maps/{{{nameof(ViewMapRequest.MapId)}}}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        ViewMapRequest request,
        CancellationToken cancellationToken
    )
    {
        /*
        var response = await session.LoadAsync<MapAggregate>(request.MapId, cancellationToken);
        */

        var response = await session.Events.AggregateStreamAsync<MapAggregate>(
            request.MapId,
            token: cancellationToken
        );

        if (response is null)
        {
            await SendResultAsync(TypedResults.NotFound());
            return;
        }

        await SendResultAsync(TypedResults.Ok(response));
    }
}
