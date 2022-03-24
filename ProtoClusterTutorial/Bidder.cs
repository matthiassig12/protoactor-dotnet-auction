using Proto;
using Proto.Cluster;

namespace ProtoClusterTutorial;

public class Bidder : BackgroundService
{
    private readonly ActorSystem _actorSystem;

    public Bidder(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Place a random bid every 5 seconds
        var random = new Random();

        var auctionId = "auction1";
        var bidders = new[] { "user1", "user2", "user3" };
        var biddingAmounts = new[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };
        var lotNumbers = new[] { 1, 2, 3 };

        while (!stoppingToken.IsCancellationRequested)
        {
            var lotNumber = lotNumbers[random.Next(lotNumbers.Length)];
            var amount = biddingAmounts[random.Next(biddingAmounts.Length)];
            var bidder = bidders[random.Next(bidders.Length)];

            var auctionGrainClient = _actorSystem.Cluster().GetAuctionGrain(auctionId);
            var bid = new Bid();
            bid.Amount = amount;
            bid.User = bidder;
            bid.LotNumber = lotNumber;
            var req = new BidOnLotRequest();
            req.Bid = bid;
            Console.WriteLine($"Bidder {bidder} placing {amount} bid on lot {lotNumber}");
            try
            {
                var res = await auctionGrainClient.BidOnLot(req, stoppingToken);
                Console.WriteLine($"BidWithYou {res?.BidWithYou}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error {e}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(5000), stoppingToken);
        }
    }
}