using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiHub.Models.Shared;

// 1. IL CONTENITORE (La Sessione)
public class SessioneInventario
{
    [Key] public Guid Id { get; set; }

    [Required]
    public string NomeSessione { get; set; }
    public DateTime DataCreazione { get; set; } = DateTime.Now;
    public DateTime? DataChiusura { get; set; }
    public bool Chiuso { get; set; } = false;
    
    public Guid CreatoDaUserId { get; set; }
    [ForeignKey(nameof(CreatoDaUserId))] public User CreatoDaUser { get; set; }
    
    public ICollection<RigaInventario> Righe { get; set; } = new List<RigaInventario>();

    // Lista degli stati delle ubicazioni
    public ICollection<SessioneUbicazione> StatiUbicazioni { get; set; } = new List<SessioneUbicazione>();
}

// 2. IL FLUSSO DI LAVORO (Gestione Zone/Ubicazioni e Lock) - NUOVA
public class SessioneUbicazione
{
    [Key] public Guid Id { get; set; }

    // Collegamento alla sessione
    public Guid SessioneInventarioId { get; set; }
    [ForeignKey(nameof(SessioneInventarioId))] public SessioneInventario Sessione { get; set; }

    // L'ubicazione fisica da controllare
    public Guid UbicazioneId { get; set; }
    [ForeignKey(nameof(UbicazioneId))] public Ubicazione Ubicazione { get; set; }
    
    // Se diverso da null, "Mario" sta lavorando qui e gli altri non possono entrare.
    public Guid? OperatoreCorrenteId { get; set; }
    [ForeignKey(nameof(OperatoreCorrenteId))] public User OperatoreCorrente { get; set; }

    // Quando Mario finisce, mette true. La zona diventa verde.
    public bool Completata { get; set; } = false;
    public DateTime? DataCompletamento { get; set; }
}

public enum StatoRigaInventario 
{ 
    InAttesa = 0,   // Creato da snapshot
    Trovato = 1,    // Trovato correttamente nell'ubicazione iniziale
    Mancante = 2,   // Confermato come non presente nell'ubicazione iniziale (potrebbe essere altrove)
    Extra = 3       // Trovato fuori posizione o nuovo da creare
}

public class RigaInventario
{
    [Key] public Guid Id { get; set; }
    
    public Guid SessioneInventarioId { get; set; }
    [ForeignKey(nameof(SessioneInventarioId))] 
    public SessioneInventario Sessione { get; set; }

    [Required]
    public Guid SemiLavoratoId { get; set; } 
    [ForeignKey(nameof(SemiLavoratoId))] 
    public SemiLavorato SemiLavorato { get; set; }

    // DOVE DOVEVA ESSERE (Snapshot iniziale)
    public Guid? UbicazioneSnapshotId { get; set; }
    [ForeignKey(nameof(UbicazioneSnapshotId))] 
    public Ubicazione? UbicazioneSnapshot { get; set; }

    // DOVE È STATO TROVATO (Durante l'inventario)
    public Guid? UbicazioneRilevataId { get; set; }
    [ForeignKey(nameof(UbicazioneRilevataId))] 
    public Ubicazione? UbicazioneRilevata { get; set; }

    public StatoRigaInventario Stato { get; set; } = StatoRigaInventario.InAttesa;

    public DateTime? DataRilevamento { get; set; }
    
    public Guid? RilevatoDaUserId { get; set; }
    [ForeignKey(nameof(RilevatoDaUserId))] 
    public User? RilevatoDaUser { get; set; }
}