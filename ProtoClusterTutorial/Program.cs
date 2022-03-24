using Proto;
using Proto.Cluster;
using ProtoClusterTutorial;
using Grpc.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddActorSystem();
builder.Services.AddHostedService<ActorSystemClusterHostedService>();
builder.Services.AddHostedService<Bidder>();
var app = builder.Build();

app.MapGet("/", () => "Hello, Proto.Cluster!");
app.MapGet("/auctions/create/{identity}/{numberOfLots}", async (ActorSystem actorSystem, string identity, string numberOfLots) =>
{
    var req = new CreateAuctionRequest();
    req.NumberOfLots = Int32.Parse(numberOfLots);
    return await actorSystem.Cluster().GetAuctionGrain(identity).CreateAuction(req, CancellationToken.None);
});
app.MapGet("/auctions/{identity}", async (ActorSystem actorSystem, string identity) =>
{
    try
    {
        var res = await actorSystem.Cluster().GetAuctionGrain(identity).GetAuction(new GetAuctionRequest(), CancellationToken.None);
        return res?.ToString();
    }
    catch (RpcException e)
    {
        // Doesnt work??
        return e.Status.ToString();
    }
});

app.Run();
