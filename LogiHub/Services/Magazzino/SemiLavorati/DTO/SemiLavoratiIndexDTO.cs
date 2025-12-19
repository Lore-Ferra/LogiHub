using System;
using System.Collections.Generic;

namespace LogiHub.Services.Shared.SemiLavorati
{
    public class SemiLavoratiIndexDTO
    {
        public IEnumerable<RigaSemiLavorato> Items { get; set; }
        public int TotalCount { get; set; }

        public class RigaSemiLavorato
        {
            public string Id { get; set; }
            public string Descrizione { get; set; }
            public string CodiceUbicazione { get; set; }
            public DateTime UltimaModifica { get; set; }
        }
    }
}