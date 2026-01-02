using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Inventari.Sessioni.DTO;

namespace LogiHub.Services.Inventari.Sessioni;

public interface ISessioniService
{
    Task<SessioneInventario> AggiungiSessioneAsync(AggiungiSessioneInventarioDTO dto);
    // Task<bool> EliminaSessioneAsync(Guid id, Guid userId);
    // Task<bool> ChiudiSessioneAsync(Guid id, Guid userId);
}