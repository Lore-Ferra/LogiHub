using System.Threading.Tasks;

namespace LogiHub.Web1.SignalR
{
    public interface IPublishDomainEvents
    {
        Task Publish(object evnt);
    }
}
