using System.Threading.Tasks;

namespace LogiHub.Web.SignalR
{
    public interface IPublishDomainEvents
    {
        Task Publish(object evnt);
    }
}
