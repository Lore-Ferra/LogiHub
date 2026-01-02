using System.Threading.Tasks;
using LogiHub.Services.Shared.SemiLavorati;

public interface ISemiLavoratoService
{
    Task<SemiLavorato> AggiungiSemiLavoratoAsync(AggiungiSemiLavoratoDTO dto);
    Task<bool> EliminaAsync(EliminaSemiLavoratoDTO dto);
    Task<bool> ModificaSemiLavorato(ModificaSemiLavoratoDTO dto);
}