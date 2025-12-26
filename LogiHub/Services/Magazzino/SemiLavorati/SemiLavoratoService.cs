using System;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services;
using LogiHub.Services.Shared.SemiLavorati;
using Microsoft.EntityFrameworkCore;

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
        sl.Barcode = dto.Barcode;
        sl.Descrizione = dto.Descrizione;
        sl.AziendaEsternaId = dto.AziendaEsternaId;
        var ubicazioneCambiata = sl.UbicazioneId != dto.UbicazioneId;
        
        sl.UltimaModifica = DateTime.Now;
        
        if (ubicazioneCambiata)
        {
            sl.UbicazioneId = dto.UbicazioneId;
            
            var ubicazioneNew = await _context.Ubicazioni
                .Where(u => u.UbicazioneId == sl.UbicazioneId)
                .Select(u => u.Posizione)
                .FirstOrDefaultAsync();
            
            GeneraAzione(dto.Id, dto.UserId, TipoOperazione.CambioUbicazione, 
                $"Spostamento da {ubicazioneOld} a {ubicazioneNew}");
        }
            
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<bool> EliminaAsync(EliminaSemiLavoratoDTO dto)
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
    
    // Task<bool> RegistraCambioUbicazione(CambioUbicazioneDTO dto);
    // Task<bool> RegistraUscita(RegistraUscitaDTO dto);
    // Task<bool> RegistraEntrata(sRegistraEntrataDTO dto);
    // Task<bool> RegistraEliminazione(EliminaSemiLavoratoDTO dto);
    
 
    
 
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    //
    //
    //
    //
    //
    //
    // public async Task<SemiLavorato?> GetByIdAsync(string id)
    // {
    //     return await _context.SemiLavorati
    //         .Include(s => s.Ubicazione)
    //         .Include(s => s.AziendaEsterna)
    //         .Include(s => s.Azioni)
    //         .FirstOrDefaultAsync(s => s.Id == id);
    // }
    //
    // public async Task<bool> AggiornaAsync(
    //     string id,
    //     string? nuovaDescrizione,
    //     Guid? nuovaUbicazioneId,
    //     Guid? nuovaAziendaEsternaId,
    //     Guid userId,
    //     string dettagli)
    // {
    //     var semi = await _context.SemiLavorati.FindAsync(id);
    //     if (semi == null || semi.Eliminato) return false;
    //
    //     semi.Descrizione = nuovaDescrizione ?? semi.Descrizione;
    //     semi.UbicazioneId = nuovaUbicazioneId ?? semi.UbicazioneId;
    //     semi.AziendaEsternaId = nuovaAziendaEsternaId ?? semi.AziendaEsternaId;
    //     semi.UltimaModifica = DateTime.Now;
    //
    //     await RegistraCreazioneAsync(id, "Aggiornamento", userId, dettagli);
    //     await _context.SaveChangesAsync();
    //
    //     return true;
    // }
    //
    // public async Task<bool> CambiaUbicazioneAsync(
    //     string semiLavoratoId,
    //     Guid nuovaUbicazioneId,
    //     Guid userId,
    //     string dettagli)
    // {
    //     var semi = await _context.SemiLavorati.FindAsync(semiLavoratoId);
    //     if (semi == null || semi.Eliminato) return false;
    //
    //     semi.UbicazioneId = nuovaUbicazioneId;
    //     semi.UltimaModifica = DateTime.Now;
    //
    //     await RegistraCreazioneAsync(semiLavoratoId, "Cambio Ubicazione", userId, dettagli);
    //     await _context.SaveChangesAsync();
    //
    //     return true;
    // }
    //
    // public async Task<bool> InviaAdAziendaEsternaAsync(
    //     string semiLavoratoId,
    //     Guid aziendaEsternaId,
    //     Guid userId,
    //     string dettagli)
    // {
    //     var semi = await _context.SemiLavorati.FindAsync(semiLavoratoId);
    //     if (semi == null || semi.Eliminato) return false;
    //
    //     semi.AziendaEsternaId = aziendaEsternaId;
    //     semi.UltimaModifica = DateTime.Now;
    //
    //     await RegistraCreazioneAsync(semiLavoratoId, "Invio a terzista", userId, dettagli);
    //     await _context.SaveChangesAsync();
    //
    //     return true;
    // }
    //
    // public async Task<bool> RegistraRientroAsync(
    //     string semiLavoratoId,
    //     Guid userId,
    //     string dettagli)
    // {
    //     var semi = await _context.SemiLavorati.FindAsync(semiLavoratoId);
    //     if (semi == null || semi.Eliminato) return false;
    //
    //     semi.AziendaEsternaId = null;
    //     semi.UltimaModifica = DateTime.Now;
    //
    //     await RegistraCreazioneAsync(semiLavoratoId, "Rientro da terzista", userId, dettagli);
    //     await _context.SaveChangesAsync();
    //
    //     return true;
    // }
    //
    //
    //
    // public async Task<IEnumerable<SemiLavorato>> GetAllAsync()
    // {
    //     return await _context.SemiLavorati
    //         .Include(s => s.Ubicazione)
    //         .Include(s => s.AziendaEsterna)
    //         .ToListAsync();
    // }
    //
    // private async Task RegistraCreazioneAsync(
    //     string semiLavoratoId,
    //     string tipo,
    //     Guid userId,
    //     string dettagli)
    // {
    //     var azione = new Azione
    //     {
    //         Id = Guid.NewGuid(),
    //         SemiLavoratoId = semiLavoratoId,
    //         TipoOperazione = tipo,
    //         Dettagli = dettagli,
    //         UserId = userId,
    //         DataOperazione = DateTime.Now
    //     };
    //
    //     _context.Azioni.Add(azione);
    //     await Task.CompletedTask;
    // }
}