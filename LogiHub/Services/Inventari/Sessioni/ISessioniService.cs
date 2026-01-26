using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Services.Inventari.Sessioni.Query;

namespace LogiHub.Services.Inventari.Sessioni;

public interface ISessioniService
{
    Task<SessioneInventario> AggiungiSessioneAsync(AggiungiSessioneInventarioDTO dto);
    Task<DettaglioSessioneDTO> OttieniDettaglioSessioneAsync(Guid sessioneId);
    Task ChiudiSessioneAsync(Guid sessioneId, Guid userId);
    
    Task BloccaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId, Guid userId);
    Task RilasciaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId);
    Task<UbicazioneStatusDTO> CompletaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId);
    
    
    Task<List<PezzoInventarioDTO>> OttieniPezziUbicazioneAsync(PezziUbicazioneQuery query);   
    Task SegnaPresenteAsync(Guid rigaId, Guid userId);
    Task SegnaMancanteAsync(Guid rigaId, Guid userId);
    Task<EsitoAggiuntaExtra> AggiungiExtraAsync(Guid sessioneId, Guid ubicazioneId, string barcode, string descrizione, Guid userId);
    Task<Guid?> OttieniConflittoExtraAsync(Guid rigaId);
    Task RimuoviExtraAsync(Guid extraId);
    Task<List<DiscrepanzaDTO>> OttieniDiscrepanzeAsync(Guid sessioneId);
    Task<UbicazioneStatusDTO> OttieniStatusUbicazioneAsync(Guid sessioneId, Guid ubicazioneId);
    Task AnnullaDiscrepanzaAsync(Guid sessioneId, string barcode, Guid userId);
    Task RisolviDiscrepanzaAsync(Guid sessioneId, DiscrepanzaDTO d, TipoRisoluzione tipo, Guid userId);
    Task RisolviTuttoAsync(Guid sessioneId, Guid userId);
}