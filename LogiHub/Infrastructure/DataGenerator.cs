using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LogiHub.Models.Shared;
using LogiHub.Services;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Infrastructure
{
    public class DataGenerator
    {
        private static readonly Random Random = new Random(12345);
        private static readonly DateTime BaseDate = new DateTime(2026, 1, 1);

        public static void Initialize(TemplateDbContext context)
        {
            SeedUsers(context);
            SeedUbicazioni(context);
            SeedAziende(context);
            context.SaveChanges();

            SeedSemiLavorati(context);
            context.SaveChanges();

            SeedInventariDemo(context);
            context.SaveChanges();

            SeedAzioni(context);
            context.SaveChanges();
        }

        private static Guid StableGuid(string input)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes);
        }

        private static void SeedUsers(TemplateDbContext context)
        {
            if (context.Users.Any()) return;

            context.Users.AddRange(
                new User
                {
                    Id = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300"),
                    Email = "email1@test.it",
                    Password = "M0Cuk9OsrcS/rTLGf5SY6DUPqU2rGc1wwV2IL88GVGo=", // SHA-256 of text "Prova"
                    FirstName = "Mario", LastName = "Rossi"
                },
                new User
                {
                    Id = Guid.Parse("a030ee81-31c7-47d0-9309-408cb5ac0ac7"),
                    Email = "email2@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Alessio", LastName = "Verdi"
                },
                new User
                {
                    Id = Guid.Parse("bfdef48b-c7ea-4227-8333-c635af267354"),
                    Email = "email3@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Francesco", LastName = "Bianchi"
                }
            );
        }

        private static void SeedUbicazioni(TemplateDbContext context)
        {
            if (context.Ubicazioni.Any()) return;

            var righe = new[] { "A", "B", "C", "D", "E" };

            foreach (var riga in righe)
            {
                for (int i = 1; i <= 5; i++)
                {
                    var posizione = $"{riga}{i}";

                    context.Ubicazioni.Add(new Ubicazione
                    {
                        UbicazioneId = StableGuid($"UBI-{posizione}"),
                        Posizione = posizione
                    });
                }
            }
        }

        private static void SeedAziende(TemplateDbContext context)
        {
            if (context.AziendeEsterne.Any()) return;

            var aziende = new[]
            {
                new AziendaEsterna
                {
                    Id = StableGuid("AZ-ALFA"),
                    Nome = "Fornitore Alfa S.p.A.",
                    Indirizzo = "Via delle Industrie 1"
                },
                new AziendaEsterna
                {
                    Id = StableGuid("AZ-BETA"),
                    Nome = "Fornitore Beta S.p.A.",
                    Indirizzo = "Via delle Industrie 2"
                },
                new AziendaEsterna
                {
                    Id = StableGuid("AZ-GAMMA"),
                    Nome = "Fornitore Gamma S.p.A.",
                    Indirizzo = "Via delle Industrie 3"
                },
                new AziendaEsterna
                {
                    Id = StableGuid("AZ-DELTA"),
                    Nome = "Fornitore Delta S.p.A.",
                    Indirizzo = "Via delle Industrie 4"
                }
            };

            context.AziendeEsterne.AddRange(aziende);
        }

        private static void SeedSemiLavorati(TemplateDbContext context)
        {
            if (context.SemiLavorati.Any()) return;

            var cluster = new (string Posizione, int Qta)[]
            {
                ("A3", 8),
                ("B1", 5),
                ("C2", 6),
                ("E4", 4)
            };

            var ubicazioni = context.Ubicazioni.ToDictionary(u => u.Posizione, u => u.UbicazioneId);

            var aziendeIds = context.AziendeEsterne.Select(a => a.Id).ToList();

            string[] descrizioni =
            {
                "Telaio metallico",
                "Lastra in alluminio",
                "Trave in acciaio",
                "Componente elettronico"
            };

            int counter = 1;

            foreach (var c in cluster)
            {
                for (int k = 0; k < c.Qta; k++)
                {
                    var barcode = $"#{counter:D4}";
                    var desc = $"{descrizioni[(counter - 1) % descrizioni.Length]} (Mod. {counter})";

                    context.SemiLavorati.Add(new SemiLavorato
                    {
                        Id = StableGuid($"SL-{barcode}"),

                        Barcode = barcode,
                        Descrizione = desc,

                        Uscito = false,
                        AziendaEsternaId = null,
                        UbicazioneId = ubicazioni[c.Posizione],

                        DataCreazione = BaseDate.AddDays(-30),
                        UltimaModifica = BaseDate.AddDays(-7),

                        Eliminato = false
                    });

                    counter++;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                var barcode = $"#{counter:D4}";
                var desc = $"{descrizioni[(counter - 1) % descrizioni.Length]} (Mod. {counter})";

                var aziendaId = aziendeIds[(counter - 1) % aziendeIds.Count];

                context.SemiLavorati.Add(new SemiLavorato
                {
                    Id = StableGuid($"SL-{barcode}"),

                    Barcode = barcode,
                    Descrizione = desc,

                    Uscito = true,
                    AziendaEsternaId = aziendaId,
                    UbicazioneId = null,

                    DataCreazione = BaseDate.AddDays(-60),
                    UltimaModifica = BaseDate.AddDays(-10),

                    Eliminato = false
                });

                counter++;
            }

            for (int i = 0; i < 3; i++)
            {
                var barcode = $"#{counter:D4}";
                var desc = $"{descrizioni[(counter - 1) % descrizioni.Length]} (Mod. {counter})";

                context.SemiLavorati.Add(new SemiLavorato
                {
                    Id = StableGuid($"SL-{barcode}"),

                    Barcode = barcode,
                    Descrizione = desc,

                    Uscito = false,
                    AziendaEsternaId = null,
                    UbicazioneId = ubicazioni["A1"],

                    DataCreazione = BaseDate.AddDays(-90),
                    UltimaModifica = BaseDate.AddDays(-20),

                    Eliminato = true
                });

                counter++;
            }

            Console.WriteLine("=== SEMILAVORATI SEED ===");
            foreach (var sl in context.SemiLavorati
                         .OrderBy(x => x.Barcode)
                         .Select(x => new
                         {
                             x.Barcode,
                             x.Descrizione,
                             x.Uscito,
                             x.Eliminato,
                             x.UbicazioneId,
                             x.AziendaEsternaId
                         }))
            {
                Console.WriteLine(
                    $"{sl.Barcode} | Uscito: {sl.Uscito} | Eliminato: {sl.Eliminato} | " +
                    $"UbiId: {(sl.UbicazioneId?.ToString() ?? "-")} | AziendaId: {(sl.AziendaEsternaId?.ToString() ?? "-")} | " +
                    $"{sl.Descrizione}"
                );
            }

            Console.WriteLine("=========================");
        }

        private static void SeedAzioni(TemplateDbContext context)
        {
            if (context.Azioni.Any()) return;

            var ubicazioni = context.Ubicazioni
                .Select(u => new { u.UbicazioneId, u.Posizione })
                .ToList();

            var posById = ubicazioni.ToDictionary(x => x.UbicazioneId, x => x.Posizione);

            string Pos(Guid? id)
                => (id != null && posById.TryGetValue(id.Value, out var p)) ? p : "N/D";

            var userIds = context.Users.Select(u => u.Id).ToList();
            if (userIds.Count == 0) return;

            Guid PickUser() => userIds[Random.Next(userIds.Count)];

            var semilavorati = context.SemiLavorati
                .IgnoreQueryFilters()
                .Where(sl => !sl.Eliminato)
                .OrderBy(sl => sl.Barcode)
                .ToList();

            if (semilavorati.Count == 0) return;

            var azioni = new List<Azione>();

            foreach (var sl in semilavorati)
            {
                var t = BaseDate.AddDays(-Random.Next(60, 240));

                azioni.Add(new Azione
                {
                    Id = StableGuid($"AZ-{sl.Barcode}-CREA"),
                    SemiLavoratoId = sl.Id,
                    UserId = PickUser(),
                    TipoOperazione = TipoOperazione.Creazione,
                    DataOperazione = t,
                    Dettagli = $"Creato semilavorato {sl.Barcode} - {sl.Descrizione}"
                });

                t = t.AddMinutes(Random.Next(10, 120));

                var targetInside = !sl.Uscito;
                bool inMagazzino = false;

                int cicli = Random.Next(1, 4);

                for (int c = 0; c < cicli; c++)
                {
                    azioni.Add(new Azione
                    {
                        Id = StableGuid($"AZ-{sl.Barcode}-IN-{c:D2}"),
                        SemiLavoratoId = sl.Id,
                        UserId = PickUser(),
                        TipoOperazione = TipoOperazione.Entrata,
                        DataOperazione = t,
                        Dettagli = $"Entrata a magazzino {sl.Barcode} in ubicazione {Pos(sl.UbicazioneId)}"
                    });

                    inMagazzino = true;
                    t = t.AddMinutes(Random.Next(15, 180));

                    int spostamenti = Random.Next(0, 5);
                    var currentPos = Pos(sl.UbicazioneId);

                    for (int m = 0; m < spostamenti; m++)
                    {
                        var newPos = ubicazioni.Count > 0 ? ubicazioni[Random.Next(ubicazioni.Count)].Posizione : "N/D";

                        azioni.Add(new Azione
                        {
                            Id = StableGuid($"AZ-{sl.Barcode}-MOVE-{c:D2}-{m:D2}"),
                            SemiLavoratoId = sl.Id,
                            UserId = PickUser(),
                            TipoOperazione = TipoOperazione.CambioUbicazione,
                            DataOperazione = t,
                            Dettagli = $"Cambio ubicazione {sl.Barcode}: {currentPos} → {newPos}"
                        });

                        currentPos = newPos;
                        t = t.AddMinutes(Random.Next(5, 90));
                    }

                    var lastCycle = (c == cicli - 1);

                    if (lastCycle && targetInside)
                    {
                        break;
                    }

                    azioni.Add(new Azione
                    {
                        Id = StableGuid($"AZ-{sl.Barcode}-OUT-{c:D2}"),
                        SemiLavoratoId = sl.Id,
                        UserId = PickUser(),
                        TipoOperazione = TipoOperazione.Uscita,
                        DataOperazione = t,
                        Dettagli = $"Uscita da magazzino {sl.Barcode}"
                    });

                    inMagazzino = false;
                    t = t.AddMinutes(Random.Next(60, 300));

                    if (lastCycle && !targetInside)
                    {
                        break;
                    }
                }

                if (!targetInside)
                {
                    var last = azioni.LastOrDefault(a => a.SemiLavoratoId == sl.Id);
                    if (last == null || last.TipoOperazione != TipoOperazione.Uscita)
                    {
                        azioni.Add(new Azione
                        {
                            Id = StableGuid($"AZ-{sl.Barcode}-OUT-FINAL"),
                            SemiLavoratoId = sl.Id,
                            UserId = PickUser(),
                            TipoOperazione = TipoOperazione.Uscita,
                            DataOperazione = t,
                            Dettagli = $"Uscita da magazzino {sl.Barcode}"
                        });
                    }
                }
                else
                {
                    var last = azioni.LastOrDefault(a => a.SemiLavoratoId == sl.Id);
                    if (last == null || last.TipoOperazione == TipoOperazione.Uscita)
                    {
                        azioni.Add(new Azione
                        {
                            Id = StableGuid($"AZ-{sl.Barcode}-IN-FINAL"),
                            SemiLavoratoId = sl.Id,
                            UserId = PickUser(),
                            TipoOperazione = TipoOperazione.Entrata,
                            DataOperazione = t,
                            Dettagli = $"Entrata a magazzino {sl.Barcode} in ubicazione {Pos(sl.UbicazioneId)}"
                        });
                    }
                }
            }

            context.Azioni.AddRange(azioni);
        }

        private static void SeedInventariDemo(TemplateDbContext context)
        {
            if (context.SessioniInventario.Any()) return;

            var marioId = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300");
            var now = BaseDate.AddDays(-5);

            var ubi = context.Ubicazioni.ToDictionary(u => u.Posizione, u => u.UbicazioneId);

            var semilavoratiMagazzino = context.SemiLavorati
                .IgnoreQueryFilters()
                .Where(sl => !sl.Uscito && !sl.Eliminato && sl.UbicazioneId != null)
                .OrderBy(sl => sl.Barcode)
                .ToList();

            if (semilavoratiMagazzino.Count < 6) return;

            RigaInventario RigaRisolta(
                Guid sessioneId,
                SemiLavorato sl,
                Guid? ubiSnap,
                Guid? ubiRil,
                StatoRigaInventario stato,
                TipoRisoluzione tipoRisoluzione,
                int minutesOffset)
            {
                return new RigaInventario
                {
                    Id = StableGuid($"SEED-RIGA-{sessioneId}-{sl.Barcode}-{stato}-{minutesOffset}"),
                    SessioneInventarioId = sessioneId,
                    SemiLavoratoId = sl.Id,

                    UbicazioneSnapshotId = ubiSnap,
                    UbicazioneRilevataId = ubiRil,

                    Stato = stato,
                    RilevatoDaUserId = marioId,
                    DataRilevamento = now.AddMinutes(minutesOffset),

                    StatoDiscrepanza = StatoDiscrepanza.Risolta,
                    TipoRisoluzione = tipoRisoluzione,
                    RisoltoDaUserId = marioId,
                    DataRisoluzione = now.AddMinutes(minutesOffset + 30)
                };
            }

            var inv1Id = StableGuid("INV-DEMO-1");
            var inv1 = new SessioneInventario
            {
                Id = inv1Id,
                NomeSessione = "Inventario #1",
                DataCreazione = now.AddDays(-10),
                DataCompletamento = now.AddDays(-10).AddHours(1),
                Chiuso = true,
                CreatoDaUserId = marioId
            };

            var inv1Ubi = new[] { "A3", "B1", "C2", "E4" }
                .Where(pos => ubi.ContainsKey(pos))
                .ToList();

            foreach (var pos in inv1Ubi)
            {
                inv1.StatiUbicazioni.Add(new SessioneUbicazione
                {
                    Id = StableGuid($"INV1-UBI-{pos}"),
                    SessioneInventarioId = inv1Id,
                    UbicazioneId = ubi[pos],
                    OperatoreCorrenteId = marioId,
                    Completata = true,
                    DataCompletamento = now.AddDays(-10).AddMinutes(40)
                });
            }

            foreach (var (sl, idx) in semilavoratiMagazzino.Take(10).Select((x, i) => (x, i)))
            {
                inv1.Righe.Add(new RigaInventario
                {
                    Id = StableGuid($"INV1-RIGA-OK-{sl.Barcode}"),
                    SessioneInventarioId = inv1Id,
                    SemiLavoratoId = sl.Id,
                    UbicazioneSnapshotId = sl.UbicazioneId,
                    UbicazioneRilevataId = sl.UbicazioneId,
                    Stato = StatoRigaInventario.Trovato,
                    RilevatoDaUserId = marioId,
                    DataRilevamento = now.AddDays(-10).AddMinutes(10 + idx),
                    StatoDiscrepanza = StatoDiscrepanza.Annullata
                });
            }

            var m1 = semilavoratiMagazzino[10];
            var m2 = semilavoratiMagazzino[11];

            inv1.Righe.Add(RigaRisolta(
                inv1Id, m1,
                ubiSnap: m1.UbicazioneId,
                ubiRil: null,
                stato: StatoRigaInventario.Mancante,
                tipoRisoluzione: TipoRisoluzione.Rimuovi,
                minutesOffset: 200));

            inv1.Righe.Add(RigaRisolta(
                inv1Id, m2,
                ubiSnap: m2.UbicazioneId,
                ubiRil: null,
                stato: StatoRigaInventario.Mancante,
                tipoRisoluzione: TipoRisoluzione.Rimuovi,
                minutesOffset: 210));

            var inv2Id = StableGuid("INV-DEMO-2");
            var inv2 = new SessioneInventario
            {
                Id = inv2Id,
                NomeSessione = "Inventario #2",
                DataCreazione = now.AddDays(-5),
                DataCompletamento = now.AddDays(-5).AddHours(2),
                Chiuso = true,
                CreatoDaUserId = marioId
            };

            var inv2Ubi = new[] { "A3", "B1", "C2", "E4" }
                .Where(pos => ubi.ContainsKey(pos))
                .ToList();

            foreach (var pos in inv2Ubi)
            {
                inv2.StatiUbicazioni.Add(new SessioneUbicazione
                {
                    Id = StableGuid($"INV2-UBI-{pos}"),
                    SessioneInventarioId = inv2Id,
                    UbicazioneId = ubi[pos],
                    OperatoreCorrenteId = marioId,
                    Completata = true,
                    DataCompletamento = now.AddDays(-5).AddHours(1)
                });
            }

            foreach (var (sl, idx) in semilavoratiMagazzino.Take(8).Select((x, i) => (x, i)))
            {
                inv2.Righe.Add(new RigaInventario
                {
                    Id = StableGuid($"INV2-RIGA-OK-{sl.Barcode}"),
                    SessioneInventarioId = inv2Id,
                    SemiLavoratoId = sl.Id,
                    UbicazioneSnapshotId = sl.UbicazioneId,
                    UbicazioneRilevataId = sl.UbicazioneId,
                    Stato = StatoRigaInventario.Trovato,
                    RilevatoDaUserId = marioId,
                    DataRilevamento = now.AddDays(-5).AddMinutes(10 + idx),
                    StatoDiscrepanza = StatoDiscrepanza.Annullata
                });
            }

            context.SessioniInventario.AddRange(inv1, inv2);

            Console.WriteLine("=== INVENTARI DEMO CREATI ===");
            Console.WriteLine(
                $"- {inv1.NomeSessione} (Chiuso={inv1.Chiuso}) | Righe={inv1.Righe.Count} | Ubi={inv1.StatiUbicazioni.Count}");
            Console.WriteLine(
                $"- {inv2.NomeSessione} (Chiuso={inv2.Chiuso}) | Righe={inv2.Righe.Count} | Ubi={inv2.StatiUbicazioni.Count}");
            Console.WriteLine("=============================");
        }
    }
}