using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISemiLavoratoService
{

    Task<SemiLavorato> CreaSemiLavoratoAsync(CreaSemiLavoratoDTO dto);
    Task<SemiLavorato?> GetByIdAsync(string id);
    
    Task<bool> AggiornaAsync(
        string id,
        string? nuovaDescrizione,
        Guid? nuovaUbicazioneId,
        Guid? nuovaAziendaEsternaId,
        Guid userId,
        string dettagli
    );

    Task<bool> CambiaUbicazioneAsync(
        string semiLavoratoId,
        Guid nuovaUbicazioneId,
        Guid userId,
        string dettagli
    );

    Task<bool> InviaAdAziendaEsternaAsync(
        string semiLavoratoId,
        Guid aziendaEsternaId,
        Guid userId,
        string dettagli
    );

    Task<bool> RegistraRientroAsync(
        string semiLavoratoId,
        Guid userId,
        string dettagli
    );

    Task<bool> EliminaAsync(
        string semiLavoratoId,
        Guid userId,
        string dettagli
    );
}
