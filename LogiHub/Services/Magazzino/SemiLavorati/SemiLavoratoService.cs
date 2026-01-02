using System;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Shared.SemiLavorati;
using Microsoft.EntityFrameworkCore;


namespace LogiHub.Services.Magazzino.SemiLavorati;
public class SemiLavoratoService : ISemiLavoratoService
{
    private readonly TemplateDbContext _context;

    public SemiLavoratoService(TemplateDbContext context)
    {
        _context = context;
    }

    //METODI CRUD
    public async Task<SemiLavorato> AggiungiSemiLavoratoAsync(AggiungiSemiLavoratoDTO dto)
    {
        var semi = new SemiLavorato
        {
            Id = Guid.NewGuid(),
            Barcode = dto.Barcode,
            Descrizione = dto.Descrizione,
            UbicazioneId = dto.UbicazioneId,
            AziendaEsternaId = dto.AziendaEsternaId,
            Eliminato = false,
            DataCreazione = DateTime.Now,
            UltimaModifica = DateTime.Now
        };

        _context.SemiLavorati.Add(semi);
        GeneraAzione(semi.Id, dto.UserId, TipoOperazione.Creazione, "Creazione iniziale");
        await _context.SaveChangesAsync();
        return semi;
    }

    public async Task<bool> ModificaSemiLavorato(ModificaSemiLavoratoDTO dto)
    {
        var sl = await _context.SemiLavorati.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (sl == null || sl.Eliminato) return false;
        var ubicazioneOld = await _context.Ubicazioni
            .Where(x => x.UbicazioneId == sl.UbicazioneId)
            .Select(u => u.Posizione)
            .FirstOrDefaultAsync();
        
        var aziendaOld = sl.AziendaEsternaId;
        
        sl.Barcode = dto.Barcode;
        sl.Descrizione = dto.Descrizione;
        sl.AziendaEsternaId = dto.AziendaEsternaId;
        sl.UltimaModifica = DateTime.Now;
        
        var ubicazioneCambiata = sl.UbicazioneId != dto.UbicazioneId && dto.UbicazioneId != null;
        var rientro = aziendaOld.HasValue && dto.AziendaEsternaId == null;
        
        sl.UbicazioneId = dto.UbicazioneId;
        
        if (rientro)
        {
            var nomeAzienda = await _context.AziendeEsterne
                .Where(x => x.Id == aziendaOld)
                .Select(x => x.Nome)
                .FirstOrDefaultAsync();
            
            GeneraAzione(dto.Id, dto.UserId, TipoOperazione.Entrata, 
                $"Rientro da {nomeAzienda}");
        
            if (dto.UbicazioneId != null)
            {
                sl.UbicazioneId = dto.UbicazioneId;
            
                var ubicazioneNew = await _context.Ubicazioni
                    .Where(u => u.UbicazioneId == dto.UbicazioneId)
                    .Select(u => u.Posizione)
                    .FirstOrDefaultAsync();
            
                GeneraAzione(dto.Id, dto.UserId, TipoOperazione.CambioUbicazione,
                    $"Posizionato in {ubicazioneNew}");
            }
        }
        else if (dto.AziendaEsternaId != null)
        {
            var inCaricoA = await _context.AziendeEsterne
                .Where(x => x.Id == dto.AziendaEsternaId)
                .Select( x => x.Nome)
                .FirstOrDefaultAsync();
            GeneraAzione(dto.Id, dto.UserId, TipoOperazione.Uscita, $"In carico a {inCaricoA}");
        }
        else if(ubicazioneCambiata && dto.UbicazioneId != null)
        {
            var ubicazioneNew = await _context.Ubicazioni
                .Where(u => u.UbicazioneId == sl.UbicazioneId)
                .Select(u => u.Posizione)
                .FirstOrDefaultAsync();
            
            GeneraAzione(dto.Id, dto.UserId, TipoOperazione.CambioUbicazione, 
                $"Spostamento da {ubicazioneOld} a {ubicazioneNew}");
        }
            
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<bool> EliminaSemiLavoratoAsync(EliminaSemiLavoratoDTO dto)
    {
        var sl = await _context.SemiLavorati.FindAsync(dto.SemiLavoratoId);
        if (sl == null || sl.Eliminato) return false;
        
        sl.Eliminato = true;
        sl.UltimaModifica = DateTime.Now;

        GeneraAzione(dto.SemiLavoratoId, dto.UserId, TipoOperazione.Eliminazione, 
            dto.Dettagli ?? "Eliminazione semilavorato");
        return await _context.SaveChangesAsync() > 0;
    }
    
    private void GeneraAzione(
        Guid semiLavoratoId, 
        Guid userId, 
        TipoOperazione tipoOperazione, 
        string dettagli)
    {
        if (semiLavoratoId == Guid.Empty || userId == Guid.Empty) return;
    
        var azione = new Azione
        {
            SemiLavoratoId = semiLavoratoId,
            TipoOperazione = tipoOperazione,
            UserId = userId,
            DataOperazione = DateTime.Now,
            Dettagli = dettagli
        };
        
        _context.Azioni.Add(azione);
    }
}