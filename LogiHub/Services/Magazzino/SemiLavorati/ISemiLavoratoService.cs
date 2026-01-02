using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Shared.SemiLavorati;

public interface ISemiLavoratoService
{
    Task<SemiLavorato> AggiungiSemiLavoratoAsync(AggiungiSemiLavoratoDTO dto);
    Task<bool> EliminaSemiLavoratoAsync(EliminaSemiLavoratoDTO dto);
    Task<bool> ModificaSemiLavorato(ModificaSemiLavoratoDTO dto);
}