using System.Threading.Tasks;

namespace LogiHub.Services.Shared;

public interface IBloccoMagazzinoService
{
    Task<bool> IsBloccatoAsync();
}