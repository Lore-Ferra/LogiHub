using System;
using System.Linq;
using LogiHub.Services;
using LogiHub.Models.Shared;

namespace LogiHub.Infrastructure
{
    public class DataGenerator
    {
        public static void InitializeUsers(TemplateDbContext context)
        {
            
            var idAzione1 = Guid.NewGuid(); 

            var idAzienda1 = Guid.NewGuid();
            var idAzienda2 = Guid.NewGuid();

            string idSemiLavorato1 = "TELAIO-001";
            string idSemiLavorato2 = "FERRO-001";
            string idSemiLavorato3 = "LASTRA-001";

            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User
                    {
                        Id = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300"), // Forced to specific Guid for tests
                        Email = "email1@test.it",
                        Password = "M0Cuk9OsrcS/rTLGf5SY6DUPqU2rGc1wwV2IL88GVGo=", // SHA-256 of text "Prova"
                        FirstName = "Nome1",
                        LastName = "Cognome1",
                        NickName = "Nickname1"
                    },
                    new User
                    {
                        Id = Guid.Parse("a030ee81-31c7-47d0-9309-408cb5ac0ac7"), // Forced to specific Guid for tests
                        Email = "email2@test.it",
                        Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                        FirstName = "Nome2",
                        LastName = "Cognome2",
                        NickName = "Nickname2"
                    },
                    new User
                    {
                        Id = Guid.Parse("bfdef48b-c7ea-4227-8333-c635af267354"), // Forced to specific Guid for tests
                        Email = "email3@test.it",
                        Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                        FirstName = "Nome3",
                        LastName = "Cognome3",
                        NickName = "Nickname3"
                    });
            }

            if (!context.Ubicazioni.Any())
            {
                var righe = new[] { "A", "B", "C" };
                int colonne = 5;

                foreach (var riga in righe)
                {
                    for (int i = 1; i <= colonne; i++)
                    {
                        context.Ubicazioni.Add(new Ubicazione
                        {
                            UbicazioneId = Guid.NewGuid(),
                            Posizione = $"{riga}{i}"
                        });
                    }
                }
            }

            if (!context.AziendeEsterne.Any())
            {
                context.AziendeEsterne.AddRange(
                    new AziendaEsterna { Id = idAzienda1, Nome = "Azienda Esterna 1", Indirizzo = "Indirizzo 1" },
                    new AziendaEsterna { Id = idAzienda2, Nome = "Azienda Esterna 2", Indirizzo = "Indirizzo 2" }
                );
            }

            context.SaveChanges();

            if (!context.SemiLavorati.Any())
            {
                context.SemiLavorati.AddRange(
                    new SemiLavorato
                    {
                        Id = idSemiLavorato1,
                        AziendaEsternaId = idAzienda1,
                        Descrizione = "Sbarra di metallo",
                        DataCreazione = DateTime.Now,
                        UltimaModifica = DateTime.Now.AddDays(-2),
                        UbicazioneId = context.Ubicazioni.First(x => x.Posizione == "A1").UbicazioneId
                    },
                    new SemiLavorato
                    {
                        Id = idSemiLavorato2,
                        AziendaEsternaId = idAzienda2,
                        Descrizione = "Trave",
                        DataCreazione = DateTime.Now,
                        UltimaModifica = DateTime.Now.AddDays(-3),
                        UbicazioneId = context.Ubicazioni.First(x => x.Posizione == "B1").UbicazioneId
                    },
                    new SemiLavorato
                    {
                        Id = idSemiLavorato3,
                        AziendaEsternaId = idAzienda1,
                        Descrizione = "Lastra",
                        DataCreazione = DateTime.Now,
                        UltimaModifica = DateTime.Now.AddDays(-4),
                        UbicazioneId = context.Ubicazioni.First(x => x.Posizione == "C1").UbicazioneId
                    }
                );

                context.SaveChanges();
            }

            if (!context.Azioni.Any())
            {
                context.Azioni.AddRange(
                    new Azione
                    {
                        Id = idAzione1,
                        Dettagli = "Arrivo Merce da fornitore",
                        SemiLavoratoId = idSemiLavorato1,
                        TipoOperazione = "Spostamento",
                        UserId = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300"),
                        DataOperazione = DateTime.Now.AddDays(-10)
                    },
                    new Azione
                    {
                        Id = idAzione2,
                        Dettagli = "Spostato in verniciatura",
                        SemiLavoratoId = idSemiLavorato2,
                        TipoOperazione = "Spostamento",
                        UserId = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300"),
                        DataOperazione = DateTime.Now.AddDays(-5)
                    }
                );

                context.SaveChanges();
            }
        }
    }
}