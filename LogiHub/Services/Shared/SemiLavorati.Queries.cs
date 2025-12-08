using LogiHub.Services;
using LogiHub.Services.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogiHub.Services.Shared;

//Oggetto che contiene i filtri per la ricerca di un Semilavorato
public class SemilavoratiIndexQuery
{
    public string Filter { get; set; }
}

// Dati che riceve la pagina
public class SemiLavoratiIndexDTO
{
    public IEnumerable<RigaSemiLavorato> Items { get; set; }
    public int Count { get; set; }

    public class RigaSemiLavorato
    {
        //dati da mostrare nella view della tabella SemiLavorati
        public string Id { get; set; }
        public string Descrizione { get; set; }
        public string CodiceUbicazione { get; set; }
        public DateTime UltimaModifica { get; set; }
    }
}

//query per i dettagli del SemiLavorato
public class SemiLavoratiDetailsQuery
{
    public string Id { get; set; }
}

public class SemiLavoratiDetailsDTO
{
    public string Id { get; set; } 
    public string Descrizione { get; set; }
    public string CodiceUbicazione { get; set; }
    public string AziendaEsterna { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime UltimaModifica { get; set; }
    public IEnumerable<AzioniDTO> StoricoAzioni { get; set; }

    public class AzioniDTO
    {
        public Guid Id { get; set; }
        public string TipoOperazione { get; set; }
        public string Utente { get; set; }
        public DateTime DataOperazione { get; set; }
        public string Dettagli { get; set; }
    }
}

public partial class SharedService
{
    public async Task<SemiLavoratiIndexDTO> Query(SemilavoratiIndexQuery qry)
    {
        var queryable = _dbContext.SemiLavorati
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(qry.Filter))
        {
            queryable = queryable.Where(x =>
                x.Descrizione.Contains(qry.Filter, StringComparison.OrdinalIgnoreCase) ||
                x.Id.Contains(qry.Filter, StringComparison.OrdinalIgnoreCase)
            );
        }

        var items = await queryable
        .Include(s => s.Ubicazione)
        .Select(x => new SemiLavoratiIndexDTO.RigaSemiLavorato
        {
            Id = x.Id,
            Descrizione = x.Descrizione,
            CodiceUbicazione = x.Ubicazione != null ? x.Ubicazione.Posizione : "-",
            UltimaModifica = x.UltimaModifica
        })
        .OrderByDescending(x => x.UltimaModifica)
        .ToListAsync();

        return new SemiLavoratiIndexDTO
        {
            Items = items,
            Count = await queryable.CountAsync()
        };
    }

    public async Task<SemiLavoratiDetailsDTO> Query(SemiLavoratiDetailsQuery qry)
    {
        var item = await _dbContext.SemiLavorati
            .Include(x => x.Ubicazione)
            .Include(x => x.AziendaEsterna)
            .Include(x => x.Azioni)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(x => x.Id == qry.Id);

        if (item == null) return null;

        return new SemiLavoratiDetailsDTO
        {
            Id = item.Id,
            Descrizione = item.Descrizione,
            AziendaEsterna = item.AziendaEsterna?.Nome ?? "Interno",
            CodiceUbicazione = item.Ubicazione?.Posizione ?? "-",
            UltimaModifica = item.UltimaModifica,
            DataCreazione = item.DataCreazione,

            StoricoAzioni = item.Azioni
                .OrderByDescending(x => x.DataOperazione)
                .Select(x => new SemiLavoratiDetailsDTO.AzioniDTO
                {
                    Id = x.Id,
                    TipoOperazione = x.TipoOperazione,
                    Dettagli = x.Dettagli,
                    DataOperazione = x.DataOperazione,
                    Utente = x.User != null
                        ? $"{x.User.FirstName} {x.User.LastName}"
                        : "System"
                })
                .ToList()
        };
    }
}
