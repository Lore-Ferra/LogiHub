using LogiHub.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogiHub.Models.Shared;

public class SemiLavoratoService : ISemiLavoratoService
{
    private readonly TemplateDbContext _context;

    public SemiLavoratoService(TemplateDbContext context)
    {
        _context = context;
    }

    public async Task<SemiLavorato> CreaSemiLavoratoAsync(
        string id,
        string descrizione,
        Guid? ubicazioneId,
        Guid? aziendaEsternaId,
        Guid userId,
        string dettagli)
    {
        var exists = await _context.SemiLavorati.AnyAsync(x => x.Id == id);
        if (exists)
            throw new Exception($"Esiste già un semilavorato con ID: {id}");

        var semi = new SemiLavorato
        {
            Id = id,
            Descrizione = descrizione,
            UbicazioneId = ubicazioneId,
            AziendaEsternaId = aziendaEsternaId,
            Eliminato = false,
            DataCreazione = DateTime.Now,
            UltimaModifica = DateTime.Now
        };

        _context.SemiLavorati.Add(semi);

        await RegistraAzioneInterna(id, "Creazione", userId, dettagli ?? "");
        await _context.SaveChangesAsync();

        return semi;
    }

    public async Task<SemiLavorato?> GetByIdAsync(string id)
    {
        return await _context.SemiLavorati
            .Include(s => s.Ubicazione)
            .Include(s => s.AziendaEsterna)
            .Include(s => s.Azioni)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<SemiLavorato>> GetAllAsync()
    {
        return await _context.SemiLavorati
            .Include(s => s.Ubicazione)
            .Include(s => s.AziendaEsterna)
            .ToListAsync();
    }

    public async Task<bool> AggiornaAsync(
        string id,
        string? nuovaDescrizione,
        Guid? nuovaUbicazioneId,
        Guid? nuovaAziendaEsternaId,
        Guid userId,
        string dettagli)
    {
        var semi = await _context.SemiLavorati.FindAsync(id);
        if (semi == null || semi.Eliminato) return false;

        semi.Descrizione = nuovaDescrizione ?? semi.Descrizione;
        semi.UbicazioneId = nuovaUbicazioneId ?? semi.UbicazioneId;
        semi.AziendaEsternaId = nuovaAziendaEsternaId ?? semi.AziendaEsternaId;
        semi.UltimaModifica = DateTime.Now;

        await RegistraAzioneInterna(id, "Aggiornamento", userId, dettagli);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CambiaUbicazioneAsync(
        string semiLavoratoId,
        Guid nuovaUbicazioneId,
        Guid userId,
        string dettagli)
    {
        var semi = await _context.SemiLavorati.FindAsync(semiLavoratoId);
        if (semi == null || semi.Eliminato) return false;

        semi.UbicazioneId = nuovaUbicazioneId;
        semi.UltimaModifica = DateTime.Now;

        await RegistraAzioneInterna(semiLavoratoId, "Cambio Ubicazione", userId, dettagli);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> InviaAdAziendaEsternaAsync(
        string semiLavoratoId,
        Guid aziendaEsternaId,
        Guid userId,
        string dettagli)
    {
        var semi = await _context.SemiLavorati.FindAsync(semiLavoratoId);
        if (semi == null || semi.Eliminato) return false;

        semi.AziendaEsternaId = aziendaEsternaId;
        semi.UltimaModifica = DateTime.Now;

        await RegistraAzioneInterna(semiLavoratoId, "Invio a terzista", userId, dettagli);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RegistraRientroAsync(
        string semiLavoratoId,
        Guid userId,
        string dettagli)
    {
        var semi = await _context.SemiLavorati.FindAsync(semiLavoratoId);
        if (semi == null || semi.Eliminato) return false;

        semi.AziendaEsternaId = null;
        semi.UltimaModifica = DateTime.Now;

        await RegistraAzioneInterna(semiLavoratoId, "Rientro da terzista", userId, dettagli);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminaAsync(
        string semiLavoratoId,
        Guid userId,
        string dettagli)
    {
        var semi = await _context.SemiLavorati.FirstOrDefaultAsync(s => s.Id == semiLavoratoId);
        if (semi == null || semi.Eliminato) return false;

        semi.Eliminato = true;
        semi.UltimaModifica = DateTime.Now;

        await RegistraAzioneInterna(semiLavoratoId, "Eliminazione (Soft Delete)", userId, dettagli);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task RegistraAzioneInterna(
        string semiLavoratoId,
        string tipo,
        Guid userId,
        string dettagli)
    {
        var azione = new Azione
        {
            Id = Guid.NewGuid(),
            SemiLavoratoId = semiLavoratoId,
            TipoOperazione = tipo,
            Dettagli = dettagli,
            UserId = userId,
            DataOperazione = DateTime.Now
        };

        _context.Azioni.Add(azione);
        await Task.CompletedTask;
    }
}
