using System;

public enum TipoDiscrepanzaOperativa 
{ 
    Mancante = 0, 
    Extra = 1, 
    Spostato = 2 
}

public enum TipoRisoluzione

{
    Sposta = 0, // Chiama ModificaSemiLavorato
    Aggiungi = 1, // Chiama AggiungiSemiLavoratoAsync
    Rimuovi = 2 // Chiama EliminaSemiLavoratoAsync
}

public class DiscrepanzaDTO
{
    public Guid? SemiLavoratoId { get; set; }
    public Guid? UbicazioneRilevataId { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }
    
    // Gestiamo entrambe le posizioni
    public string UbicazioneSnapshot { get; set; }
    public string UbicazioneRilevata { get; set; }
    
    public TipoDiscrepanzaOperativa Tipo { get; set; }
    
    public string RilevatoDa { get; set; }
    public DateTime? DataRilevamento { get; set; }
}