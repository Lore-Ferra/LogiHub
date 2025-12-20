using System;
using System.Collections.Generic;
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
    public async Task<SemiLavorato> CreaSemiLavoratoAsync(CreaSemiLavoratoDTO dto)
    {
        // var exists = await _context.SemiLavorati.AnyAsync(x => x.Barcode == dto.Barcode); //causa crash in caso aggiunta nuovo semi stesso barcode

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
        GeneraAzioneCreazione(semi.Id, dto.UserId);
        await _context.SaveChangesAsync();
        return semi;
    }
    public async Task<bool> EliminaAsync(EliminaSemiLavoratoDTO dto)
    {
        var sl = await _context.SemiLavorati.FindAsync(dto.SemiLavoratoId);
        if (sl == null || sl.Eliminato) return false;
        
        sl.Eliminato = true;
        sl.UltimaModifica = DateTime.Now;

        GeneraAzioneEliminazione(dto);
        return await _context.SaveChangesAsync() > 0;
    }
    
    //AZIONI
    private void GeneraAzioneCreazione(Guid semiLavoratoId, Guid userId)
    {
        if (semiLavoratoId == Guid.Empty || userId == Guid.Empty) return;
 
        var azione = new Azione
        {
            SemiLavoratoId = semiLavoratoId,
            TipoOperazione = TipoOperazione.Creazione,
            UserId = userId,
            DataOperazione = DateTime.Now,
            Dettagli = "Creazione iniziale"
        };
        _context.Azioni.Add(azione);
    }
    
    private void GeneraAzioneEliminazione(EliminaSemiLavoratoDTO dto)
    {
        if (dto.SemiLavoratoId == Guid.Empty || dto.UserId == Guid.Empty) return;

        var azione = new Azione
        {
            SemiLavoratoId = dto.SemiLavoratoId,
            TipoOperazione = TipoOperazione.Eliminazione,
            UserId = dto.UserId,
            Dettagli = dto.Dettagli ?? "Eliminazione semilavorato",
            DataOperazione = DateTime.Now
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