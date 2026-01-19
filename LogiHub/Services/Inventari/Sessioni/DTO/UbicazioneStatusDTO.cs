namespace LogiHub.Services.Inventari.Sessioni.DTO;

public class UbicazioneStatusDTO
{
    public int Totali { get; set; }
    public int Rilevati { get; set; }
    public int ConteggioExtra { get; set; }
    public int InAttesa => Totali - Rilevati;
    public bool GiaCompletata { get; set; }
}