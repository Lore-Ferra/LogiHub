using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Services.Inventari.Sessioni.Query;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Services.Inventari.Sessioni;

public class SessioniService : ISessioniService
{
    private readonly TemplateDbContext _context;

    public SessioniService(TemplateDbContext context)
    {
        _context = context;
    }

    // Gestione Sessione
    public async Task<SessioneInventario> AggiungiSessioneAsync(AggiungiSessioneInventarioDTO dto)
    {
        // 1. Controllo univocità sessione attiva
        if (await _context.SessioniInventario.AnyAsync(s => !s.Chiuso))
            throw new InvalidOperationException("Esiste già una sessione aperta.");

        // Calcolo un numero progressivo basato sul conteggio totale
        var numeroProgressivo = await _context.SessioniInventario.CountAsync() + 1;

        // 2. Creazione Testata
        var sessione = new SessioneInventario
        {
            Id = Guid.NewGuid(),
            NomeSessione = $"Inventario #{numeroProgressivo}",
            CreatoDaUserId = dto.UserId,
            DataCreazione = DateTime.Now
        };

        // 3. Snapshot SemiLavorati (Righe)
        var semiLavorati = await _context.SemiLavorati
            .Where(s => !s.Eliminato && !s.Uscito)
            .AsNoTracking()
            .ToListAsync();

        var nuoveRighe = semiLavorati.Select(sl => new RigaInventario
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessione.Id,
            SemiLavoratoId = sl.Id,
            UbicazioneSnapshotId = sl.UbicazioneId,
            Stato = StatoRigaInventario.InAttesa
        }).ToList();

        // 4. Snapshot Ubicazioni (Stati)
        var tutteUbicazioni = await _context.Ubicazioni.AsNoTracking().ToListAsync();
        var statiUbi = tutteUbicazioni.Select(u => new SessioneUbicazione
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessione.Id,
            UbicazioneId = u.UbicazioneId
        }).ToList();

        // 5. Salvataggio UNICO
        _context.SessioniInventario.Add(sessione);
        _context.RigheInventario.AddRange(nuoveRighe); // Nota: AddRange è sufficiente, non serve Async qui
        _context.SessioniUbicazioni.AddRange(statiUbi);

        await _context.SaveChangesAsync();

        return sessione;
    }

    public async Task<DettaglioSessioneDTO> OttieniDettaglioSessioneAsync(Guid sessioneId)
    {
        var sessione = await _context.SessioniInventario
            .Where(s => s.Id == sessioneId)
            .Select(s => new DettaglioSessioneDTO
            {
                SessioneId = s.Id,
                NomeSessione = s.NomeSessione,
                Chiuso = s.Chiuso,
                Ubicazioni = s.StatiUbicazioni
                    // Filtriamo le ubicazioni guardando le RIGHE della sessione, non il magazzino reale
                    .Where(su => _context.RigheInventario.Any(r =>
                        r.SessioneInventarioId == sessioneId &&
                        r.UbicazioneSnapshotId == su.UbicazioneId))
                    .Select(su => new DettaglioSessioneDTO.UbicazioneConStato
                    {
                        UbicazioneId = su.UbicazioneId,
                        Posizione = su.Ubicazione.Posizione,
                        Completata = su.Completata,
                        InLavorazione = su.OperatoreCorrenteId != null,
                        OperatoreCorrenteId = su.OperatoreCorrenteId,
                        OperatoreCorrente = su.OperatoreCorrente != null
                            ? su.OperatoreCorrente.FirstName + " " + su.OperatoreCorrente.LastName
                            : null
                    })
                    .OrderBy(u => u.Completata ? 2 : (u.InLavorazione ? 1 : 0))
                    .ThenBy(u => u.Posizione)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (sessione == null)
            throw new InvalidOperationException("Sessione non trovata.");

        return sessione;
    }

    public async Task ChiudiSessioneAsync(Guid sessioneId, Guid userId)
    {
        // Verifichiamo se ci sono ubicazioni NON completate che però contengono righe di inventario.
        // Usiamo lo stesso filtro della visualizzazione dettaglio per coerenza.
        var ciSonoUbicazioniDaCompletare = await _context.SessioniUbicazioni
            .AnyAsync(su => su.SessioneInventarioId == sessioneId 
                            && !su.Completata
                            && _context.RigheInventario.Any(r => r.SessioneInventarioId == sessioneId && r.UbicazioneSnapshotId == su.UbicazioneId));

        if (ciSonoUbicazioniDaCompletare)
        {
            throw new InvalidOperationException("Impossibile chiudere l'inventario: ci sono ancora ubicazioni da completare.");
        }

        var sessione = await _context.SessioniInventario.FindAsync(sessioneId);
        if (sessione != null)
        {
            sessione.Chiuso = true;
            await _context.SaveChangesAsync();
        }
    }
    
    // Operatività - Gestione Accesso
    public async Task BloccaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId, Guid userId)
    {
        var statoUbi = await _context.SessioniUbicazioni
            .FirstOrDefaultAsync(u => u.SessioneInventarioId == sessioneId && u.UbicazioneId == ubicazioneId);

        if (statoUbi == null)
            throw new InvalidOperationException("Ubicazione non configurata per questa sessione.");

        if (statoUbi.Completata)
            throw new InvalidOperationException("Questa ubicazione è già stata completata.");

        // Se è già occupata da un altro
        if (statoUbi.OperatoreCorrenteId != null && statoUbi.OperatoreCorrenteId != userId)
            throw new InvalidOperationException("Ubicazione occupata da un altro operatore.");

        statoUbi.OperatoreCorrenteId = userId;
        await _context.SaveChangesAsync();
    }
    
    public async Task RilasciaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId)
    {
        var statoUbi = await _context.SessioniUbicazioni
            .FirstOrDefaultAsync(u => u.SessioneInventarioId == sessioneId && u.UbicazioneId == ubicazioneId);

        if (statoUbi != null)
        {
            statoUbi.OperatoreCorrenteId = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CompletaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId)
    {
        var righeResidue = await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == sessioneId && 
                        r.UbicazioneSnapshotId == ubicazioneId && 
                        r.Stato == StatoRigaInventario.InAttesa)
            .ToListAsync();
        
        foreach (var riga in righeResidue)
        {
            riga.Stato = StatoRigaInventario.Mancante;
            riga.DataRilevamento = DateTime.Now;
        }
        
        var statoUbi = await _context.SessioniUbicazioni
            .FirstOrDefaultAsync(u => u.SessioneInventarioId == sessioneId && u.UbicazioneId == ubicazioneId);

        if (statoUbi != null)
        {
            statoUbi.Completata = true;
            statoUbi.OperatoreCorrenteId = null;
            statoUbi.DataCompletamento = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }
    
    // Azioni sui Pezzi
    public async Task<List<PezzoInventarioDTO>> OttieniPezziUbicazioneAsync(PezziUbicazioneQuery query)
    {
        return await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == query.SessioneId &&
                        r.UbicazioneSnapshotId == query.UbicazioneId)
            .Select(r => new PezzoInventarioDTO
            {
                RigaId = r.Id,
                Barcode = r.SemiLavorato.Barcode,
                Descrizione = r.SemiLavorato.Descrizione,
                Stato = r.Stato
            })
            .OrderBy(r => r.Barcode)
            .ToListAsync();
    }
    
    public async Task SegnaPresenteAsync(Guid rigaId, Guid userId)
    {
        var riga = await _context.RigheInventario.FindAsync(rigaId);
        if (riga == null) throw new Exception("Riga non trovata");

        riga.Stato = StatoRigaInventario.Trovato;
        riga.UbicazioneRilevataId = riga.UbicazioneSnapshotId;
        riga.RilevatoDaUserId = userId;
        riga.DataRilevamento = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task SegnaMancanteAsync(Guid rigaId, Guid userId)
    {
        var riga = await _context.RigheInventario.FindAsync(rigaId);
        if (riga == null) throw new Exception("Riga non trovata");

        riga.Stato = StatoRigaInventario.Mancante;
        riga.RilevatoDaUserId = userId;
        riga.DataRilevamento = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task AggiungiExtraAsync(Guid sessioneId, Guid ubicazioneId, string barcode, Guid userId)
    {
        var giaRilevato = await _context.RigheInventario.AnyAsync(r => 
            r.SessioneInventarioId == sessioneId && 
            r.SemiLavorato.Barcode == barcode &&
            (r.Stato == StatoRigaInventario.Trovato || r.Stato == StatoRigaInventario.Extra));

        if (giaRilevato)
            throw new InvalidOperationException("Questo barcode è già stato rilevato in questa sessione.");

        var sl = await _context.SemiLavorati.FirstOrDefaultAsync(s => s.Barcode == barcode);

        var extra = new RigaInventario
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessioneId,
            SemiLavoratoId = sl?.Id ?? Guid.Empty, 
            UbicazioneSnapshotId = null,
            UbicazioneRilevataId = ubicazioneId,
            Stato = StatoRigaInventario.Extra,
            RilevatoDaUserId = userId,
            DataRilevamento = DateTime.Now
        };

        _context.RigheInventario.Add(extra);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DiscrepanzaDTO>> OttieniDiscrepanzeAsync(Guid sessioneId)
    {
        var righe = await _context.RigheInventario
            .Include(r => r.SemiLavorato)
            .Include(r => r.UbicazioneSnapshot)
            .Include(r => r.UbicazioneRilevata)
            .Include(r => r.RilevatoDaUser)
            .Where(r => r.SessioneInventarioId == sessioneId &&
                        (r.Stato == StatoRigaInventario.Mancante || r.Stato == StatoRigaInventario.Extra))
            .AsNoTracking()
            .ToListAsync();

        return righe.Select(r => new DiscrepanzaDTO
        {
            RigaId = r.Id,
            Barcode = r.SemiLavorato.Barcode,
            Descrizione = r.SemiLavorato.Descrizione,
            // Se Mancante mostriamo dove doveva essere, se Extra dove è stato trovato
            Ubicazione = r.Stato == StatoRigaInventario.Mancante 
                ? r.UbicazioneSnapshot?.Posizione ?? "N/D" 
                : r.UbicazioneRilevata?.Posizione ?? "N/D",
            TipoDiscrepanza = r.Stato,
            RilevatoDa = r.RilevatoDaUser != null ? $"{r.RilevatoDaUser.FirstName} {r.RilevatoDaUser.LastName}" : "Sistema",
            DataRilevamento = r.DataRilevamento
        }).OrderByDescending(x => x.DataRilevamento).ToList();
    }
}