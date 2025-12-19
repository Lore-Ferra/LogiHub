using System;
using System.Collections.Generic;

namespace LogiHub.Services.Shared.SemiLavorati
{
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
}