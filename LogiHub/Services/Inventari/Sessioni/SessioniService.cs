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
        // 1. Controllo Ubicazioni (esistente)
        var ubicazioniConRighe = _context.RigheInventario
            .Where(r => r.SessioneInventarioId == sessioneId && r.UbicazioneSnapshotId != null)
            .Select(r => r.UbicazioneSnapshotId!.Value)
            .Distinct();

        var ciSonoUbicazioniDaCompletare = await _context.SessioniUbicazioni
            .AnyAsync(su =>
                su.SessioneInventarioId == sessioneId &&
                !su.Completata &&
                ubicazioniConRighe.Contains(su.UbicazioneId)
            );

        if (ciSonoUbicazioniDaCompletare)
            throw new InvalidOperationException("Ci sono ancora ubicazioni non completate.");


        // 2. Controllo Discrepanze Aperte (Fondamentale)
        var ciSonoDiscrepanzeAperte = await _context.RigheInventario
            .AnyAsync(r => r.SessioneInventarioId == sessioneId
                           && r.StatoDiscrepanza == StatoDiscrepanza.Aperta
                           && (r.Stato == StatoRigaInventario.Mancante || r.Stato == StatoRigaInventario.Extra));

        if (ciSonoDiscrepanzeAperte)
            throw new InvalidOperationException("Impossibile chiudere: ci sono discrepanze non gestite.");

        // 3. Chiusura Sessione
        var sessione = await _context.SessioniInventario.FindAsync(sessioneId);
        if (sessione != null)
        {
            sessione.Chiuso = true;
            sessione.DataChiusura = DateTime.Now;
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
        if (statoUbi.Completata)
        {
            return;
        }

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

    public async Task<UbicazioneStatusDTO> CompletaUbicazioneAsync(Guid sessioneId, Guid ubicazioneId)
    {
        var statoUbi = await _context.SessioniUbicazioni
            .FirstOrDefaultAsync(u => u.SessioneInventarioId == sessioneId && u.UbicazioneId == ubicazioneId);

        if (statoUbi == null) throw new InvalidOperationException("Ubicazione non trovata.");

        // Se è già completata, restituiamo lo stato attuale senza fare nulla
        if (statoUbi.Completata)
            return await OttieniStatusUbicazioneAsync(sessioneId, ubicazioneId);

        // 1. Troviamo le righe che sono ancora "InAttesa" e le segnamo come "Mancanti"
        var righeResidue = await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == sessioneId &&
                        r.UbicazioneSnapshotId == ubicazioneId &&
                        r.Stato == StatoRigaInventario.InAttesa)
            .ToListAsync();

        foreach (var riga in righeResidue)
        {
            riga.Stato = StatoRigaInventario.Mancante;
            riga.DataRilevamento = DateTime.Now;
            // Se vuoi tracciare chi ha chiuso l'ubicazione come responsabile dei mancanti:
            // riga.RilevatoDaUserId = userId; 
        }

        // 2. Aggiorniamo lo stato dell'ubicazione nella sessione
        statoUbi.Completata = true;
        statoUbi.DataCompletamento = DateTime.Now;
        statoUbi.OperatoreCorrenteId = null; // Fondamentale per rilasciare il lock

        await _context.SaveChangesAsync();

        // 3. Restituiamo il DTO aggiornato
        return await OttieniStatusUbicazioneAsync(sessioneId, ubicazioneId);
    }

    #endregion

    #region Azioni sui Pezzi

    public async Task<List<PezzoInventarioDTO>> OttieniPezziUbicazioneAsync(PezziUbicazioneQuery query)
    {
        return await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == query.SessioneId &&
                        r.UbicazioneSnapshotId == query.UbicazioneId)
            .Where(r => r.Stato != StatoRigaInventario.Extra)
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

    public async Task<UbicazioneStatusDTO> OttieniStatusUbicazioneAsync(Guid sessioneId, Guid ubicazioneId)
    {
        var righe = await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == sessioneId && r.UbicazioneSnapshotId == ubicazioneId)
            .ToListAsync();

        var statoUbi = await _context.SessioniUbicazioni
            .FirstOrDefaultAsync(u => u.SessioneInventarioId == sessioneId && u.UbicazioneId == ubicazioneId);

        return new UbicazioneStatusDTO
        {
            Totali = righe.Count,
            Rilevati = righe.Count(r => r.Stato != StatoRigaInventario.InAttesa),
            GiaCompletata = statoUbi?.Completata ?? false
        };
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

    public async Task AggiungiExtraAsync(Guid sessioneId, Guid ubicazioneId, string barcode, string descrizione,
        Guid userId)
    {
        // 1. Recuperiamo tutte le righe associate a questo barcode nella sessione
        var righeSessione = await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == sessioneId && r.SemiLavorato.Barcode == barcode)
            .ToListAsync();

        if (righeSessione.Any())
        {
            // Caso 3: Già segnato come Presente (nella sua posizione originale) -> BLOCCO
            if (righeSessione.Any(r => r.Stato == StatoRigaInventario.Trovato))
            {
                Console.WriteLine($"[DEBUG] Tentativo Extra fallito: Barcode {barcode} già presente come Trovato.");
                throw new InvalidOperationException(
                    "Questo barcode è già stato rilevato nella sua posizione corretta.");
            }

            // Caso 2: Già inserito come EXTRA -> SILENT RETURN (Idempotenza)
            if (righeSessione.Any(r => r.Stato == StatoRigaInventario.Extra))
            {
                Console.WriteLine(
                    $"[DEBUG] Barcode {barcode} già presente negli Extra. Ignoro l'inserimento duplicato.");
                return;
            }

            // Caso 1: Se esiste solo come 'InAttesa' o 'Mancante', procediamo con la creazione dell'Extra.
        }

        // 2. Recupero o Creazione del SemiLavorato
        var sl = await _context.SemiLavorati.FirstOrDefaultAsync(s => s.Barcode == barcode);

        if (sl == null)
        {
            sl = new SemiLavorato
            {
                Id = Guid.NewGuid(),
                Barcode = barcode,
                Descrizione = string.IsNullOrWhiteSpace(descrizione) ? "Nuovo Articolo" : descrizione,
                UbicazioneId = null,
                DataCreazione = DateTime.Now,
                UltimaModifica = DateTime.Now
            };
            _context.SemiLavorati.Add(sl);

            _context.Azioni.Add(new Azione
            {
                Id = Guid.NewGuid(),
                SemiLavoratoId = sl.Id,
                TipoOperazione = TipoOperazione.Creazione,
                UserId = userId,
                DataOperazione = DateTime.Now,
                Dettagli = "Creazione da Inventario (Extra)"
            });
        }

        // 3. Creazione riga Extra
        var extra = new RigaInventario
        {
            Id = Guid.NewGuid(),
            SessioneInventarioId = sessioneId,
            SemiLavoratoId = sl.Id,
            UbicazioneSnapshotId = sl.UbicazioneId,
            UbicazioneRilevataId = ubicazioneId,
            Stato = StatoRigaInventario.Extra,
            RilevatoDaUserId = userId,
            DataRilevamento = DateTime.Now,
            DescrizioneRilevata = string.IsNullOrWhiteSpace(descrizione) ? null : descrizione,
            StatoDiscrepanza = StatoDiscrepanza.Aperta
        };

        _context.RigheInventario.Add(extra);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[DEBUG] Nuovo Extra registrato con successo: {barcode}");
    }

    public async Task<Guid?> OttieniConflittoExtraAsync(Guid rigaId)
    {
        var riga = await _context.RigheInventario
            .Include(r => r.SemiLavorato)
            .FirstOrDefaultAsync(r => r.Id == rigaId);

        if (riga == null) return null;

        var extra = await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == riga.SessioneInventarioId &&
                        r.SemiLavorato.Barcode == riga.SemiLavorato.Barcode &&
                        r.Stato == StatoRigaInventario.Extra)
            .FirstOrDefaultAsync();

        return extra?.Id;
    }

    public async Task RimuoviExtraAsync(Guid extraId)
    {
        var extra = await _context.RigheInventario.FindAsync(extraId);
        if (extra != null)
        {
            _context.RigheInventario.Remove(extra);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Analisi e Risoluzione Discrepanze

   public async Task<List<DiscrepanzaDTO>> OttieniDiscrepanzeAsync(Guid sessioneId)
{
    // 1. Prendiamo tutte le righe che sono state segnalate come anomalie
    var righeGrezze = await _context.RigheInventario
        .IgnoreQueryFilters()
        .Include(r => r.SemiLavorato)
        .Include(r => r.UbicazioneSnapshot)
        .Include(r => r.UbicazioneRilevata)
        .Include(r => r.RilevatoDaUser)
        .Include(r => r.RisoltoDaUser)
        .Where(r => r.SessioneInventarioId == sessioneId &&
                    (r.Stato == StatoRigaInventario.Mancante || r.Stato == StatoRigaInventario.Extra))
        .AsNoTracking()
        .ToListAsync();

    var risultato = new List<DiscrepanzaDTO>();
    var gruppiPerBarcode = righeGrezze.GroupBy(r => r.SemiLavorato?.Barcode ?? "(Sconosciuto)");

    foreach (var gruppo in gruppiPerBarcode)
    {
        var righe = gruppo.ToList();

        // Determiniamo quali anomalie esistono per questo barcode
        var haMancante = righe.Any(r => r.Stato == StatoRigaInventario.Mancante);
        var haExtra = righe.Any(r => r.Stato == StatoRigaInventario.Extra);

        // La riga di riferimento deve essere quella "Aperta". 
        // Se sono tutte chiuse, prendiamo l'ultima gestita.
        var rif = righe.FirstOrDefault(r => r.StatoDiscrepanza == StatoDiscrepanza.Aperta)
                  ?? righe.OrderByDescending(r => r.DataRisoluzione).First();

        var dto = new DiscrepanzaDTO
        {
            Barcode = gruppo.Key,
            Descrizione = rif.SemiLavorato.Descrizione,
            SemiLavoratoId = rif.SemiLavoratoId,
            Stato = rif.StatoDiscrepanza,
            DataRilevamento = righe.Max(r => r.DataRilevamento),
            DataGestione = rif.DataRisoluzione,
            GestitaDa = rif.RisoltoDaUser != null
                ? $"{rif.RisoltoDaUser.FirstName} {rif.RisoltoDaUser.LastName}"
                : null
        };

        // --- NUOVA LOGICA STRICT ---
        // Uno spostamento esiste SOLO se abbiamo confermato sia l'assenza (Mancante) 
        // che la nuova presenza (Extra).
        
        if (haMancante && haExtra)
        {
            // CASO 1: SPOSTATO
            dto.Tipo = TipoDiscrepanzaOperativa.Spostato;
            
            var rigaMancante = righe.First(r => r.Stato == StatoRigaInventario.Mancante);
            var rigaExtra = righe.First(r => r.Stato == StatoRigaInventario.Extra);

            dto.UbicazioneSnapshot = rigaMancante.UbicazioneSnapshot?.Posizione ?? "N/D";
            dto.UbicazioneRilevata = rigaExtra.UbicazioneRilevata?.Posizione ?? "N/D";
            dto.UbicazioneRilevataId = rigaExtra.UbicazioneRilevataId;
        }
        else if (haExtra)
        {
            // CASO 2: EXTRA
            // Anche se il pezzo aveva un UbicazioneSnapshotId, finché quella vecchia posizione 
            // non viene dichiarata "Mancante" (è ancora InAttesa), questo rimane un Extra puro.
            dto.Tipo = TipoDiscrepanzaOperativa.Extra;
            
            var rigaExtra = righe.First(r => r.Stato == StatoRigaInventario.Extra);
            dto.UbicazioneRilevata = rigaExtra.UbicazioneRilevata?.Posizione ?? "N/D";
            dto.UbicazioneRilevataId = rigaExtra.UbicazioneRilevataId;
            
            // Non impostiamo UbicazioneSnapshot per non confondere l'utente nel Tab Extra
            dto.UbicazioneSnapshot = null; 
        }
        else
        {
            // CASO 3: MANCANTE
            dto.Tipo = TipoDiscrepanzaOperativa.Mancante;
            
            var rigaMancante = righe.First(r => r.Stato == StatoRigaInventario.Mancante);
            dto.UbicazioneSnapshot = rigaMancante.UbicazioneSnapshot?.Posizione ?? "N/D";
        }

        risultato.Add(dto);
    }

    return risultato.OrderByDescending(x => x.DataRilevamento).ToList();
}

    public async Task AnnullaDiscrepanzaAsync(Guid sessioneId, string barcode, Guid userId)
    {
        var righe = await _context.RigheInventario
            .Where(r => r.SessioneInventarioId == sessioneId && r.SemiLavorato.Barcode == barcode)
            .ToListAsync();

        foreach (var riga in righe)
        {
            riga.StatoDiscrepanza = StatoDiscrepanza.Annullata;
            riga.RisoltoDaUserId = userId;
            riga.DataRisoluzione = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RisolviDiscrepanzaAsync(Guid sessioneId, DiscrepanzaDTO d, TipoRisoluzione tipo, Guid userId)
    {
        // 1. RECUPERO TUTTE LE RIGHE PER QUEL BARCODE
        // Invece di FirstOrDefault (una sola), usiamo Where + ToListAsync (tutte)
        // Usiamo il Barcode come chiave di ricerca perché è l'elemento comune tra Missing ed Extra
        var righeCoinvolte = await _context.RigheInventario
            .Include(r => r.SemiLavorato)
            .Where(r => r.SessioneInventarioId == sessioneId &&
                        r.SemiLavorato.Barcode == d.Barcode)
            .ToListAsync();

        // Se non trovo nulla e non è un'aggiunta (nuovo pezzo), errore
        if (!righeCoinvolte.Any() && tipo != TipoRisoluzione.Aggiungi)
            throw new InvalidOperationException("Nessuna riga inventario trovata per questo barcode.");

        // Cerchiamo se in una delle righe (quella rilevata) c'è una nuova descrizione
        var rigaConNuovaDesc = righeCoinvolte.FirstOrDefault(r => !string.IsNullOrEmpty(r.DescrizioneRilevata));
        string descrizioneDaUsare = d.Descrizione;

        if (rigaConNuovaDesc != null)
        {
            descrizioneDaUsare = rigaConNuovaDesc.DescrizioneRilevata;
            var slMaster = await _context.SemiLavorati.FindAsync(rigaConNuovaDesc.SemiLavoratoId);
            if (slMaster != null)
            {
                slMaster.Descrizione = rigaConNuovaDesc.DescrizioneRilevata;
            }
        }

        switch (tipo)
        {
            case TipoRisoluzione.Sposta:
                if (d.SemiLavoratoId.HasValue && d.UbicazioneRilevataId.HasValue)
                {
                    await _slService.ModificaSemiLavorato(new ModificaSemiLavoratoDTO
                    {
                        Id = d.SemiLavoratoId.Value,
                        UbicazioneId = d.UbicazioneRilevataId.Value,
                        IsRettificaInventario = true,
                        Barcode = d.Barcode,
                        Descrizione = descrizioneDaUsare,
                        UserId = userId
                    });
                }

                break;

            case TipoRisoluzione.Aggiungi:
                // Se il semilavorato esiste già (perché creato in AggiungiExtra o esistente), 
                // dobbiamo solo confermare la sua posizione (come se fosse uno spostamento/conferma).
                if (d.SemiLavoratoId.HasValue && d.UbicazioneRilevataId.HasValue)
                {
                    // Recuperiamo i dati attuali per il DTO
                    var slEsistente = await _context.SemiLavorati.FindAsync(d.SemiLavoratoId.Value);
                    if (slEsistente != null)
                    {
                        await _slService.ModificaSemiLavorato(new ModificaSemiLavoratoDTO
                        {
                            Id = slEsistente.Id,
                            Barcode = slEsistente.Barcode,
                            Descrizione = slEsistente.Descrizione,
                            UbicazioneId = d.UbicazioneRilevataId.Value,
                            IsRettificaInventario = true,
                            UserId = userId
                        });
                    }
                }

                break;

            case TipoRisoluzione.Rimuovi:
                if (d.SemiLavoratoId.HasValue)
                {
                    await _slService.EliminaSemiLavoratoAsync(new EliminaSemiLavoratoDTO
                    {
                        SemiLavoratoId = d.SemiLavoratoId.Value,
                        IsRettificaInventario = true
                    });
                }

                break;
        }

        // Ciclo su tutte le righe trovate (Mancante + Extra) e le chiudo tutte.
        foreach (var riga in righeCoinvolte)
        {
            // Aggiorno solo se è ancora aperta
            if (riga.StatoDiscrepanza == StatoDiscrepanza.Aperta)
            {
                riga.StatoDiscrepanza = StatoDiscrepanza.Risolta;
                riga.TipoRisoluzione = tipo;
                riga.RisoltoDaUserId = userId;
                riga.DataRisoluzione = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task RisolviTuttoAsync(Guid sessioneId, Guid userId)
    {
        var discrepanze = (await OttieniDiscrepanzeAsync(sessioneId))
            .Where(x => x.Stato == StatoDiscrepanza.Aperta);

        foreach (var d in discrepanze)
        {
            var azione = d.Tipo switch
            {
                TipoDiscrepanzaOperativa.Spostato => TipoRisoluzione.Sposta,
                TipoDiscrepanzaOperativa.Extra => TipoRisoluzione.Aggiungi,
                TipoDiscrepanzaOperativa.Mancante => TipoRisoluzione.Rimuovi,
                _ => throw new ArgumentOutOfRangeException()
            };

            await RisolviDiscrepanzaAsync(sessioneId, d, azione, userId);
        }
    }

    #endregion
}