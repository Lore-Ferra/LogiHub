using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Services.Inventari.Sessioni.Query;

namespace LogiHub.Services.Inventari.Sessioni;

public interface ISessioniService
{
    // Gestione Sessione
    Task<SessioneInventario> AggiungiSessioneAsync(AggiungiSessioneInventarioDTO dto);
    Task<DettaglioSessioneDTO> OttieniDettaglioSessioneAsync(Guid sessioneId);
    // Task<bool> EliminaSessioneAsync(Guid id, Guid userId);
    Task ChiudiSessioneAsync(Guid sessioneId, Guid userId);
    
    
    // Operatività - Gestione Accesso
    Task BloccaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId, Guid userId);
    Task RilasciaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId);
    Task CompletaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId);
    
    
    // Azioni sui Pezzi
    Task<List<PezzoInventarioDTO>> OttieniPezziUbicazioneAsync(PezziUbicazioneQuery query);   
    Task SegnaPresenteAsync(Guid rigaId, Guid userId);
    Task SegnaMancanteAsync(Guid rigaId, Guid userId);
    Task AggiungiExtraAsync(Guid sessioneId, Guid ubicazioneId, string barcode, Guid userId);
}