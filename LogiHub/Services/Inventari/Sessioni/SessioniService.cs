using System;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Inventari.Sessioni.DTO;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Services.Inventari.Sessioni;

public class SessioniService : ISessioniService
{
    private readonly TemplateDbContext _context;

    public SessioniService(TemplateDbContext context)
    {
        _context = context;
    }

    public async Task<SessioneInventario> AggiungiSessioneAsync(AggiungiSessioneInventarioDTO dto)
    {
        // 1. Controllo duplicati
        if (await _context.SessioniInventario.AnyAsync(s => !s.Chiuso))
            throw new InvalidOperationException("Esiste già una sessione aperta.");

        // 2. Testata
        var sessione = new SessioneInventario
        {
            Id = Guid.NewGuid(),
            NomeSessione = $"Inventario del {DateTime.Now:dd/MM/yyyy HH:mm}",
            CreatoDaUserId = dto.UserId
        };

        // 3. Snapshot Prodotti
        var semiLavorati =
            await _context.SemiLavorati.Where(s => !s.Eliminato && !s.Uscito).AsNoTracking().ToListAsync();
        var nuoveRighe = semiLavorati.Select(sl => new RigaInventario
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessione.Id,
            SemiLavoratoId = sl.Id,
            UbicazionePrevistaId = sl.UbicazioneId
        }).ToList();

        // 4. Snapshot Ubicazioni 
        var tutteUbicazioni = await _context.Ubicazioni.AsNoTracking().ToListAsync();
        var statiUbi = tutteUbicazioni.Select(u => new SessioneUbicazione
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessione.Id,
            UbicazioneId = u.UbicazioneId
        }).ToList();

        // 5. Salvataggio
        _context.SessioniInventario.Add(sessione);
        await _context.RigheInventario.AddRangeAsync(nuoveRighe);
        await _context.SessioniUbicazioni.AddRangeAsync(statiUbi);

        await _context.SaveChangesAsync();

        return sessione;
    }
}