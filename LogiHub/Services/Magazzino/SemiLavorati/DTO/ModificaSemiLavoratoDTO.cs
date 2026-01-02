using System;

namespace LogiHub.Services.Magazzino.SemiLavorati.DTO;

public class ModificaSemiLavoratoDTO
{
    public Guid Id { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public Guid? UbicazioneId { get; set; }

    public Guid? AziendaEsternaId { get; set; }

    public Guid UserId { get; set; }
    public bool Uscito { get; set; }
}