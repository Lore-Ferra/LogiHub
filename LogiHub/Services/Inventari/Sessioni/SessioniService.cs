using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Services.Inventari.Sessioni.Query;
using LogiHub.Services.Magazzino.SemiLavorati.DTO;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Services.Inventari.Sessioni;

public class SessioniService : ISessioniService
{
    private readonly TemplateDbContext _context;
    private readonly ISemiLavoratoService _slService;

    public SessioniService(TemplateDbContext context, ISemiLavoratoService slService)
    {
        _context = context;
        _slService = slService;
    }

    #region Gestione Sessione

    public async Task<SessioneInventario> AggiungiSessioneAsync(AggiungiSessioneInventarioDTO dto)
    {
        if (await _context.SessioniInventario.AnyAsync(s => !s.Chiuso))
            throw new InvalidOperationException("Esiste già una sessione aperta.");

        var numeroProgressivo = await _context.SessioniInventario.CountAsync() + 1;

        var sessione = new SessioneInventario
        {
            Id = Guid.NewGuid(),
            NomeSessione = $"Inventario #{numeroProgressivo}",
            CreatoDaUserId = dto.UserId,
            DataCreazione = DateTime.Now
        };

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

        var tutteUbicazioni = await _context.Ubicazioni.AsNoTracking().ToListAsync();
        var statiUbi = tutteUbicazioni.Select(u => new SessioneUbicazione
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessione.Id,
            UbicazioneId = u.UbicazioneId
        }).ToList();

        _context.SessioniInventario.Add(sessione);
        _context.RigheInventario.AddRange(nuoveRighe);
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
                    .Where(su => _context.RigheInventario.Any(r =>
                        r.SessioneInventarioId == sessioneId &&
                        r.UbicazioneSnapshotId == su.UbicazioneId))
                    .Select(su => new DettaglioSessioneDTO.UbicazioneConStato
                    {
                        UbicazioneId = su.UbicazioneId,
                        Posizione = su.Ubicazione.Posizione,
                        Completata = su.Completata,
                        InLavorazione = !su.Completata && su.OperatoreCorrenteId != null,
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

        if (sessione == null) throw new InvalidOperationException("Sessione non trovata.");
        return sessione;
    }

    public async Task ChiudiSessioneAsync(Guid sessioneId, Guid userId)
    {
        var ciSonoUbicazioniDaCompletare = await _context.SessioniUbicazioni
            .AnyAsync(su => su.SessioneInventarioId == sessioneId
                            && !su.Completata
                            && _context.RigheInventario.Any(r =>
                                r.SessioneInventarioId == sessioneId && r.UbicazioneSnapshotId == su.UbicazioneId));

        if (ciSonoUbicazioniDaCompletare)
            throw new InvalidOperationException("Impossibile chiudere l'inventario: ci sono ancora ubicazioni da completare.");

        var sessione = await _context.SessioniInventario.FindAsync(sessioneId);
        if (sessione != null)
        {
            sessione.Chiuso = true;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Operatività - Gestione Accesso

    public async Task BloccaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId, Guid userId)
    {
        var statoUbi = await _context.SessioniUbicazioni
            .FirstOrDefaultAsync(u => u.SessioneInventarioId == sessioneId && u.UbicazioneId == ubicazioneId);

        if (statoUbi == null) throw new InvalidOperationException("Ubicazione non configurata.");
        if (statoUbi.Completata) throw new InvalidOperationException("Ubicazione già completata.");
        if (statoUbi.OperatoreCorrenteId != null && statoUbi.OperatoreCorrenteId != userId)
            throw new InvalidOperationException("Ubicazione occupata.");

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
            statoUbi.DataCompletamento = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Azioni sui Pezzi

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

        if (giaRilevato) throw new InvalidOperationException("Barcode già rilevato.");

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

    #endregion

    #region Analisi e Risoluzione Discrepanze

    public async Task<List<DiscrepanzaDTO>> OttieniDiscrepanzeAsync(Guid sessioneId)
    {
        var righeGrezze = await _context.RigheInventario
            .Include(r => r.SemiLavorato)
            .Include(r => r.UbicazioneSnapshot)
            .Include(r => r.UbicazioneRilevata)
            .Include(r => r.RilevatoDaUser)
            .Where(r => r.SessioneInventarioId == sessioneId &&
                        (r.Stato == StatoRigaInventario.Mancante || r.Stato == StatoRigaInventario.Extra))
            .AsNoTracking()
            .ToListAsync();

        var gruppiPerBarcode = righeGrezze.GroupBy(r => r.SemiLavorato.Barcode);
        var risultato = new List<DiscrepanzaDTO>();

        foreach (var gruppo in gruppiPerBarcode)
        {
            var righe = gruppo.ToList();
            var haMancante = righe.Any(r => r.Stato == StatoRigaInventario.Mancante);
            var haExtra = righe.Any(r => r.Stato == StatoRigaInventario.Extra);
            var rif = haExtra ? righe.First(r => r.Stato == StatoRigaInventario.Extra) : righe.First();

            var dto = new DiscrepanzaDTO
            {
                SemiLavoratoId = haExtra ? rif.SemiLavoratoId : righe.First(r => r.Stato == StatoRigaInventario.Mancante).SemiLavoratoId,
                UbicazioneRilevataId = haExtra ? rif.UbicazioneRilevataId : null, // IMPORTANTE: Mappiamo l'ID per la risoluzione
                Barcode = gruppo.Key,
                Descrizione = rif.SemiLavorato.Descrizione,
                RilevatoDa = rif.RilevatoDaUser != null ? $"{rif.RilevatoDaUser.FirstName} {rif.RilevatoDaUser.LastName}" : "Sistema",
                DataRilevamento = righe.Max(r => r.DataRilevamento)
            };

            if (haMancante && haExtra)
            {
                dto.Tipo = TipoDiscrepanzaOperativa.Spostato;
                dto.UbicazioneSnapshot = righe.First(r => r.Stato == StatoRigaInventario.Mancante).UbicazioneSnapshot?.Posizione ?? "N/D";
                dto.UbicazioneRilevata = righe.First(r => r.Stato == StatoRigaInventario.Extra).UbicazioneRilevata?.Posizione ?? "N/D";
            }
            else if (haExtra)
            {
                dto.Tipo = TipoDiscrepanzaOperativa.Extra;
                dto.UbicazioneRilevata = rif.UbicazioneRilevata?.Posizione ?? "N/D";
            }
            else
            {
                dto.Tipo = TipoDiscrepanzaOperativa.Mancante;
                dto.UbicazioneSnapshot = rif.UbicazioneSnapshot?.Posizione ?? "N/D";
            }

            risultato.Add(dto);
        }

        return risultato.OrderByDescending(x => x.DataRilevamento).ToList();
    }

    public async Task RisolviDiscrepanzaAsync(Guid sessioneId, DiscrepanzaDTO d, TipoRisoluzione tipo)
    {
        switch (tipo)
        {
            case TipoRisoluzione.Sposta:
                if (!d.SemiLavoratoId.HasValue || !d.UbicazioneRilevataId.HasValue)
                    throw new InvalidOperationException("Dati insufficienti per lo spostamento.");

                await _slService.ModificaSemiLavorato(new ModificaSemiLavoratoDTO
                {
                    Id = d.SemiLavoratoId.Value,
                    UbicazioneId = d.UbicazioneRilevataId.Value,
                    Descrizione = d.Descrizione,
                    IsRettificaInventario = true
                });
                break;

            case TipoRisoluzione.Aggiungi:
                if (!d.UbicazioneRilevataId.HasValue)
                    throw new InvalidOperationException("Ubicazione mancante per l'aggiunta.");

                await _slService.AggiungiSemiLavoratoAsync(new AggiungiSemiLavoratoDTO
                {
                    Barcode = d.Barcode,
                    Descrizione = d.Descrizione ?? "Extra rilevato in inventario",
                    UbicazioneId = d.UbicazioneRilevataId.Value,
                    IsRettificaInventario = true
                });
                break;

            case TipoRisoluzione.Rimuovi:
                if (!d.SemiLavoratoId.HasValue)
                    throw new InvalidOperationException("ID mancante per la rimozione.");

                await _slService.EliminaSemiLavoratoAsync(new EliminaSemiLavoratoDTO
                {
                    SemiLavoratoId = d.SemiLavoratoId.Value,
                    IsRettificaInventario = true
                });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(tipo));
        }
    }

    public async Task RisolviTuttoAsync(Guid sessioneId)
    {
        var discrepanze = await OttieniDiscrepanzeAsync(sessioneId);

        foreach (var d in discrepanze)
        {
            var azione = d.Tipo switch
            {
                TipoDiscrepanzaOperativa.Spostato => TipoRisoluzione.Sposta,
                TipoDiscrepanzaOperativa.Extra => TipoRisoluzione.Aggiungi,
                _ => TipoRisoluzione.Rimuovi
            };

            await RisolviDiscrepanzaAsync(sessioneId, d, azione);
        }
    }

    #endregion
}