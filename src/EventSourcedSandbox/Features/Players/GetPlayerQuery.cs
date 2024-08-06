using EventSourcedSandbox.Domain.Players;
using Marten;

namespace EventSourcedSandbox.Features.Players;

internal sealed record ViewPlayerRequest(Guid PlayerId);

internal sealed class ViewPlayerQuery(IQuerySession session)
    : Endpoint<ViewPlayerRequest, Results<Ok<PlayerAggregate>, NotFound>>
{
    public override void Configure()
    {
        Get($"/players/{nameof(ViewPlayerRequest.PlayerId)}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        ViewPlayerRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await session.LoadAsync<PlayerAggregate>(
            request.PlayerId,
            cancellationToken
        );

        if (response is null)
        {
            await SendResultAsync(TypedResults.NotFound());
            return;
        }

        await SendResultAsync(TypedResults.Ok(response));
    }
}
