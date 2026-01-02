using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Services.Magazzino.SemiLavorati.DTO;
using LogiHub.Services.Shared.SemiLavorati;

namespace LogiHub.Services.Shared
{
    public partial class SharedService
    {
        public async Task<SemiLavoratiIndexDTO> GetSemiLavoratiListAsync(SemilavoratiIndexQuery qry)
        {
            var queryable = _dbContext.SemiLavorati
                .AsNoTracking()
                .Include(s => s.Ubicazione)
                .AsQueryable();


            // Filtro USCITO
            if (qry.Uscito == TriState.True)
            {
                queryable = queryable.Where(x => x.Uscito);
            }
            else if (qry.Uscito == TriState.False)
            {
                queryable = queryable.Where(x => !x.Uscito);
            }

            // Ricerca Testuale (Solo se c'è testo)
            if (!string.IsNullOrWhiteSpace(qry.SearchText))
            {
                var filterLower = qry.SearchText.ToLower();

                // Se la lista colonne è null o vuota, cerco ovunque
                var searchAll = qry.SearchInColumns == null || !qry.SearchInColumns.Any();


                queryable = queryable.Where(x =>
                    ((searchAll || qry.SearchInColumns.Contains("Barcode")) &&
                     x.Barcode.ToLower().Contains(filterLower)) ||
                    ((searchAll || qry.SearchInColumns.Contains("Descrizione")) &&
                     x.Descrizione.ToLower().Contains(filterLower)) ||
                    ((searchAll || qry.SearchInColumns.Contains("Ubicazione")) && (x.Ubicazione != null &&
                        x.Ubicazione.Posizione.ToLower().Contains(filterLower))) ||
                    ((searchAll || qry.SearchInColumns.Contains("UltimaModifica")) &&
                     x.UltimaModifica.ToString("dd/MM/yyyy").Contains(filterLower))
                );
            }

            // Paginazione e Risultato
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
                    Uscito = x.Uscito,
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
                .FirstOrDefaultAsync(x => x.Id == qry.Id);

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