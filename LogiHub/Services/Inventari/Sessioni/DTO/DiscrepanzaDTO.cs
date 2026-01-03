using System;
using LogiHub.Models.Shared;

namespace LogiHub.Services.Inventari.Sessioni.DTO;

public class DiscrepanzaDTO
{
    public Guid RigaId { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }
    public string Ubicazione { get; set; }
    public StatoRigaInventario TipoDiscrepanza { get; set; }
    public string RilevatoDa { get; set; }
    public DateTime? DataRilevamento { get; set; }
}