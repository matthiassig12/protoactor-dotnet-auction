using Proto;
using Proto.Cluster;
using Grpc.Core;

namespace ProtoClusterTutorial;

public class AuctionGrain : AuctionGrainBase
{
    private readonly ClusterIdentity _clusterIdentity;
    private Auction? _state;

    public AuctionGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
    {
        _clusterIdentity = clusterIdentity;

        Console.WriteLine($"Auction grain {_clusterIdentity.Identity}: started");
    }

    public override Task<CreateAuctionResponse> CreateAuction(CreateAuctionRequest request)
    {
        _state = new Auction();
        _state.Id = _clusterIdentity.Identity;
        _state.State = Auction.Types.State.Idle;
        for (var i = 1; i <= request.NumberOfLots; i++)
        {
            var lot = new Lot();
            lot.Id = Guid.NewGuid().ToString();
            lot.Number = i;
            _state.Lots.Add(lot);
        }
        return Task.FromResult(new CreateAuctionResponse
        {
            Auction = _state
        });
    }

    public override Task<GetAuctionResponse> GetAuction(GetAuctionRequest request)
    {
        if (_state == null)
        {
            return Task.FromException<GetAuctionResponse>(new RpcException(new Status(StatusCode.NotFound, "Auction not found")));
        }
        return Task.FromResult(new GetAuctionResponse
        {
            Auction = _state
        });
    }

    public override Task<BidOnLotResponse> BidOnLot(BidOnLotRequest request)
    {
        if (_state == null)
        {
            return Task.FromException<BidOnLotResponse>(new RpcException(new Status(StatusCode.NotFound, "Auction not found")));
        }

        foreach (Lot lot in _state.Lots)
        {
            if (lot.Number == request.Bid.LotNumber)
            {
                if (lot.CurrentBid > request.Bid.Amount)
                {
                    return Task.FromResult(new BidOnLotResponse
                    {
                        BidWithYou = lot.Leader == request.Bid.User
                    });
                }
                lot.CurrentBid = request.Bid.Amount;
                lot.Leader = request.Bid.User;
                return Task.FromResult(new BidOnLotResponse
                {
                    BidWithYou = true
                });
            }
        }

        return Task.FromResult(new BidOnLotResponse
        {
            BidWithYou = false
        });
    }
}