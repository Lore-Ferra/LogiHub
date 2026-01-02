using System;
using System.Collections.Generic;

namespace LogiHub.Services.Magazzino.SemiLavorati.DTO;

public class SemiLavoratiIndexDTO
{
    public IEnumerable<RigaSemiLavorato> Items { get; set; }
    public int TotalCount { get; set; }

    public class RigaSemiLavorato
    {
        public Guid Id { get; set; }
        public string Barcode { get; set; }
        public string Descrizione { get; set; }
        public string CodiceUbicazione { get; set; }
        public DateTime UltimaModifica { get; set; }
        public bool Uscito { get; set; }
    }
}