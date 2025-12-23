using System;

public class AggiungiSemiLavoratoDTO
{
    public string Barcode { get; set; }
    public string Descrizione { get; set; }
    public Guid? UbicazioneId { get; set; }
    public Guid? AziendaEsternaId { get; set; }
    public Guid UserId { get; set; }
}