using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Models.Shared;
using LogiHub.Services.Shared.SemiLavorati;

namespace LogiHub.Services.Shared
{
    public partial class SharedService
    {
        public async Task<SemiLavoratiIndexDTO> GetSemiLavoratiListAsync(SemilavoratiIndexQuery qry)
        {
            var queryable = _dbContext.SemiLavorati
                .AsNoTracking()
                .Where(x => !x.Eliminato)
                .Include(s => s.Ubicazione)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(qry.Filter))
            {
                var filterLower = qry.Filter.ToLower();
                queryable = queryable.Where(x =>
                    x.Descrizione.ToLower().Contains(filterLower) ||
                    x.Barcode.ToLower().Contains(filterLower) ||
                    (x.Ubicazione != null && x.Ubicazione.Posizione.ToLower().Contains(filterLower))
                );
            }

            var totalCount = await queryable.CountAsync();

            var items = await queryable
                .OrderByDescending(x => x.UltimaModifica)
                .Skip((qry.Page - 1) * qry.PageSize)
                .Take(qry.PageSize)
                .Select(x => new SemiLavoratiIndexDTO.RigaSemiLavorato
                {
                    Id = x.Id,
                    Barcode = x.Barcode,
                    Descrizione = x.Descrizione,
                    CodiceUbicazione = x.Ubicazione != null ? x.Ubicazione.Posizione : "-",
                    UltimaModifica = x.UltimaModifica
                })
                .ToListAsync();

            return new SemiLavoratiIndexDTO
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<SemiLavoratiDetailsDTO> GetSemiLavoratoDetailsAsync(SemiLavoratiDetailsQuery qry)
        {
            var item = await _dbContext.SemiLavorati
                .Include(x => x.Ubicazione)
                .Include(x => x.AziendaEsterna)
                .Include(x => x.Azioni)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(x => x.Id == qry.Id && !x.Eliminato);

            if (item == null) return null;

            return new SemiLavoratiDetailsDTO
            {
                Id = item.Id,
                Barcode = item.Barcode,
                Descrizione = item.Descrizione,
                Uscito = item.Uscito,
        
                // Dati per la visualizzazione (Etichette)
                CodiceUbicazione = item.Ubicazione?.Posizione ?? "-",
                AziendaEsterna = item.AziendaEsterna?.Nome ?? "Interno",
        
                // ID tecnici
                UbicazioneId = item.UbicazioneId,
                AziendaEsternaId = item.AziendaEsternaId,
        
                // Date di sistema
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
}
