using Proto;
using Proto.Cluster;
using Proto.Persistence;
using Grpc.Core;

namespace ProtoClusterTutorial;

public class AuctionGrain : AuctionGrainBase
{
    private readonly ClusterIdentity _clusterIdentity;
    private readonly Persistence _persistence;
    private Auction? _state;

    public AuctionGrain(IContext context, ClusterIdentity clusterIdentity, IEventStore eventStore) : base(context)
    {
        _clusterIdentity = clusterIdentity;
        _persistence = Persistence.WithEventSourcing(eventStore, clusterIdentity.Identity, ApplyEvent);

        Console.WriteLine($"Auction grain {_clusterIdentity.Identity}: started");
    }

    private void ApplyEvent(Event @event)
    {
        switch (@event.Data)
        {
            case AuctionCreated msg:
                _state = NewAuction(msg.NumberOfLots);
                break;
            case BidPlaced msg:
                foreach (Lot lot in _state.Lots)
                {
                    if (lot.Number == msg.Bid.LotNumber)
                    {
                        lot.CurrentBid = msg.Bid.Amount;
                        lot.Leader = msg.Bid.User;
                    }
                }
                break;
        }
    }

    public override Task OnStarted()
    {
        return _persistence.RecoverStateAsync();
    }

    public override async Task<CreateAuctionResponse> CreateAuction(CreateAuctionRequest request)
    {
        _state = NewAuction(request.NumberOfLots);
        await _persistence.PersistEventAsync(new AuctionCreated
        {
            NumberOfLots = request.NumberOfLots
        });
        return new CreateAuctionResponse
        {
            Auction = _state
        };
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

    public override async Task<BidOnLotResponse> BidOnLot(BidOnLotRequest request)
    {
        if (_state == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Auction not found"));
        }

        foreach (Lot lot in _state.Lots)
        {
            if (lot.Number == request.Bid.LotNumber)
            {
                if (lot.CurrentBid > request.Bid.Amount)
                {
                    return new BidOnLotResponse
                    {
                        BidWithYou = lot.Leader == request.Bid.User
                    };
                }
                lot.CurrentBid = request.Bid.Amount;
                lot.Leader = request.Bid.User;
                await _persistence.PersistEventAsync(new BidPlaced
                {
                    Bid = request.Bid
                });
                return new BidOnLotResponse
                {
                    BidWithYou = true
                };
            }
        }

        return new BidOnLotResponse
        {
            BidWithYou = false
        };
    }

    private Auction NewAuction(Int32 numberOfLots)
    {
        var auction = new Auction();
        auction.Id = _clusterIdentity.Identity;
        auction.State = Auction.Types.State.Idle;
        for (var i = 1; i <= numberOfLots; i++)
        {
            var lot = new Lot();
            lot.Id = Guid.NewGuid().ToString();
            lot.Number = i;
            auction.Lots.Add(lot);
        }
        return auction;
    }
}