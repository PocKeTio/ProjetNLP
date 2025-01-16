using System.Text.RegularExpressions;
using ProjetNLP.Models;

namespace ProjetNLP.Services
{
    public class AnalyseStatistique
    {
        private readonly Dictionary<string, double> _poidsTermes;
        private readonly Dictionary<(string, string), double> _poidsCooccurrences;
        private readonly HashSet<string> _termesPositifs;
        private readonly HashSet<string> _termesNegatifs;

        public AnalyseStatistique()
        {
            // Initialisation des termes positifs (indicateurs d'Extend or Pay)
            _termesPositifs = new HashSet<string>
            {
                // Termes d'extension
                "extend", "prolong", "renew", "roll over", "defer", "postpone", "delay",
                "prorog", "prolongation", "renouvellement", "report", "différer", "ajourner",
                
                // Termes de paiement
                "pay", "settle", "remit", "honor", "honour", "discharge", "liquidate", "reimburse",
                "payer", "règlement", "rembourser", "honorer", "liquider", "acquitter",
                
                // Termes de condition
                "otherwise", "alternatively", "failing which", "in default", "unless",
                "sinon", "autrement", "à défaut", "faute de quoi", "sauf si"
            };

            // Initialisation des termes négatifs (contre-indicateurs)
            _termesNegatifs = new HashSet<string>
            {
                "already extended", "previously extended", "not extend", "cannot extend",
                "déjà prorogé", "précédemment prolongé", "ne pas prolonger", "impossible de prolonger",
                "standard extension", "extension habituelle", "prorogation standard",
                "regular payment", "paiement régulier", "règlement habituel"
            };

            // Initialisation des poids des termes individuels
            _poidsTermes = new Dictionary<string, double>
            {
                // Termes très fortement indicatifs (poids élevé)
                {"extend or pay", 0.8}, {"proroger ou payer", 0.8},
                {"pay or extend", 0.8}, {"payer ou proroger", 0.8},
                {"extend and pay", 0.7}, {"proroger et payer", 0.7},
                
                // Termes modérément indicatifs
                {"extend", 0.4}, {"proroger", 0.4},
                {"pay", 0.3}, {"payer", 0.3},
                {"otherwise", 0.4}, {"sinon", 0.4},
                {"alternatively", 0.4}, {"alternativement", 0.4},
                
                // Termes faiblement indicatifs
                {"option", 0.2}, {"choice", 0.2}, {"choix", 0.2},
                {"request", 0.2}, {"demande", 0.2},
                {"please", 0.2}, {"prière", 0.2}
            };

            // Initialisation des poids des cooccurrences
            _poidsCooccurrences = new Dictionary<(string, string), double>
            {
                // Combinaisons fortement indicatives
                {("extend", "pay"), 0.6},
                {("proroger", "payer"), 0.6},
                {("otherwise", "pay"), 0.5},
                {("sinon", "payer"), 0.5},
                {("alternatively", "pay"), 0.5},
                {("alternativement", "payer"), 0.5},
                
                // Combinaisons modérément indicatives
                {("extend", "option"), 0.3},
                {("proroger", "option"), 0.3},
                {("pay", "option"), 0.3},
                {("payer", "option"), 0.3}
            };
        }

        public class AnalyseResultat
        {
            public double Probabilite { get; set; }
            public List<string> TermesTrouves { get; set; } = new();
            public List<(string, string)> CooccurrencesTrouvees { get; set; } = new();
            public List<string> ContreIndicateursTrouves { get; set; } = new();
            public Dictionary<string, double> ContributionTermes { get; set; } = new();
        }

        public AnalyseResultat AnalyserTexte(string texte, int langue)
        {
            var resultat = new AnalyseResultat();
            var texteLower = texte.ToLowerInvariant();
            
            // Normalisation du texte
            texteLower = Regex.Replace(texteLower, @"\s+", " ");
            var mots = texteLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double score = 0;
            double totalPoids = 0;

            // 1. Recherche des termes individuels
            foreach (var terme in _poidsTermes.Keys)
            {
                if (texteLower.Contains(terme))
                {
                    resultat.TermesTrouves.Add(terme);
                    var contribution = _poidsTermes[terme];
                    score += contribution;
                    totalPoids += Math.Abs(contribution);
                    resultat.ContributionTermes[terme] = contribution;
                }
            }

            // 2. Recherche des cooccurrences
            foreach (var cooccurrence in _poidsCooccurrences.Keys)
            {
                if (texteLower.Contains(cooccurrence.Item1) && texteLower.Contains(cooccurrence.Item2))
                {
                    // Vérification de la proximité (dans une fenêtre de 10 mots)
                    if (SontProches(texteLower, cooccurrence.Item1, cooccurrence.Item2, 10))
                    {
                        resultat.CooccurrencesTrouvees.Add(cooccurrence);
                        var contribution = _poidsCooccurrences[cooccurrence];
                        score += contribution;
                        totalPoids += Math.Abs(contribution);
                        resultat.ContributionTermes[$"{cooccurrence.Item1} + {cooccurrence.Item2}"] = contribution;
                    }
                }
            }

            // 3. Recherche des contre-indicateurs
            foreach (var terme in _termesNegatifs)
            {
                if (texteLower.Contains(terme))
                {
                    resultat.ContreIndicateursTrouves.Add(terme);
                    var contribution = -0.5; // Pénalité forte pour les contre-indicateurs
                    score += contribution;
                    totalPoids += Math.Abs(contribution);
                    resultat.ContributionTermes[$"(contre) {terme}"] = contribution;
                }
            }

            // 4. Normalisation du score
            if (totalPoids > 0)
            {
                resultat.Probabilite = Math.Max(0, Math.Min(1, (score + totalPoids) / (2 * totalPoids)));
            }
            else
            {
                resultat.Probabilite = 0;
            }

            return resultat;
        }

        private bool SontProches(string texte, string terme1, string terme2, int fenetre)
        {
            var mots = texte.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var positions1 = new List<int>();
            var positions2 = new List<int>();

            // Trouver toutes les positions des termes
            for (int i = 0; i < mots.Length; i++)
            {
                if (mots[i].Contains(terme1)) positions1.Add(i);
                if (mots[i].Contains(terme2)) positions2.Add(i);
            }

            // Vérifier si au moins une paire de positions est proche
            foreach (var pos1 in positions1)
            {
                foreach (var pos2 in positions2)
                {
                    if (Math.Abs(pos1 - pos2) <= fenetre)
                        return true;
                }
            }

            return false;
        }

        public void AfficherAnalyseDetaillee(AnalyseResultat resultat)
        {
            Console.WriteLine("\nAnalyse détaillée du texte :");
            Console.WriteLine($"Probabilité finale : {resultat.Probabilite:P2}\n");

            if (resultat.TermesTrouves.Any())
            {
                Console.WriteLine("Termes indicatifs trouvés :");
                foreach (var terme in resultat.TermesTrouves)
                {
                    Console.WriteLine($"  - {terme} (contribution: {resultat.ContributionTermes[terme]:+0.00;-0.00})");
                }
            }

            if (resultat.CooccurrencesTrouvees.Any())
            {
                Console.WriteLine("\nCooccurrences significatives :");
                foreach (var cooccurrence in resultat.CooccurrencesTrouvees)
                {
                    var key = $"{cooccurrence.Item1} + {cooccurrence.Item2}";
                    Console.WriteLine($"  - {cooccurrence.Item1} avec {cooccurrence.Item2} " +
                                    $"(contribution: {resultat.ContributionTermes[key]:+0.00;-0.00})");
                }
            }

            if (resultat.ContreIndicateursTrouves.Any())
            {
                Console.WriteLine("\nContre-indicateurs trouvés :");
                foreach (var terme in resultat.ContreIndicateursTrouves)
                {
                    var key = $"(contre) {terme}";
                    Console.WriteLine($"  - {terme} (contribution: {resultat.ContributionTermes[key]:+0.00;-0.00})");
                }
            }
        }
    }
}
