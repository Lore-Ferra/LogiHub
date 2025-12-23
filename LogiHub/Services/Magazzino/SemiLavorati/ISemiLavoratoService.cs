using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogiHub.Services.Shared.SemiLavorati;

public interface ISemiLavoratoService
{
    
    Task<SemiLavorato> AggiungiSemiLavoratoAsync(AggiungiSemiLavoratoDTO dto);
    Task<bool> EliminaAsync(EliminaSemiLavoratoDTO dto);
    Task<bool> ModificaSemiLavorato(ModificaSemiLavoratoDTO dto);
    //
    //
    //
    // Task<SemiLavorato?> GetByIdAsync(string id);
    //
    // Task<bool> AggiornaAsync(
    //     string id,
    //     string? nuovaDescrizione,
    //     Guid? nuovaUbicazioneId,
    //     Guid? nuovaAziendaEsternaId,
    //     Guid userId,
    //     string dettagli
    // );
    //
    // Task<bool> CambiaUbicazioneAsync(
    //     string semiLavoratoId,
    //     Guid nuovaUbicazioneId,
    //     Guid userId,
    //     string dettagli
    // );
    //
    // Task<bool> InviaAdAziendaEsternaAsync(
    //     string semiLavoratoId,
    //     Guid aziendaEsternaId,
    //     Guid userId,
    //     string dettagli
    // );
    //
    // Task<bool> RegistraRientroAsync(
    //     string semiLavoratoId,
    //     Guid userId,
    //     string dettagli
    // );
}
