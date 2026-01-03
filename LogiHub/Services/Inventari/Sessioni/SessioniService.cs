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

    public async Task<DettaglioSessioneDTO> GetDashboardAsync(Guid sessioneId)
    {
        var sessione = await _context.SessioniInventario
            .Where(s => s.Id == sessioneId)
            .Select(s => new DettaglioSessioneDTO
            {
                SessioneId = s.Id,
                NomeSessione = s.NomeSessione,
                Chiuso = s.Chiuso,
                Ubicazioni = s.StatiUbicazioni
                .Where(u => _context.SemiLavorati
                .Any(sl => sl.UbicazioneId == u.UbicazioneId && !sl.Eliminato && !sl.Uscito))
                    .Select(su => new DettaglioSessioneDTO.UbicazioneConStato
                    {
                        UbicazioneId = su.UbicazioneId,
                        Posizione = su.Ubicazione.Posizione,
                        Completata = su.Completata,
                        InLavorazione = su.OperatoreCorrenteId != null,
                        OperatoreCorrente = su.OperatoreCorrente != null 
                            ? su.OperatoreCorrente.FirstName + " " + su.OperatoreCorrente.LastName 
                            : null
                    })
                    .OrderBy(u => u.Posizione)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (sessione == null)
            throw new InvalidOperationException("Sessione non trovata.");

        return sessione;
    }
}