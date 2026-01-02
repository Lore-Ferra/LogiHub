using System;
using System.Threading.Tasks;

namespace LogiHub.Services.Inventario.Sessioni;

public interface ISessioniService
{
    Task<Guid> CreaSessioneRapidaAsync(Guid userId);
    Task<bool> EliminaSessioneAsync(Guid id, Guid userId);
    Task<bool> ChiudiSessioneAsync(Guid id, Guid userId);
}