using System.Text.RegularExpressions;
using ProjetNLP.Models;

namespace ProjetNLP.Services
{
    public class AnalyseFrequentielle
    {
        private Dictionary<string, (int Total, int PositifCount)> _statistiquesMots;
        private Dictionary<string, double> _fiabiliteMots;
        private HashSet<string> _indicateursFiables;
        private int _seuilOccurrencesMin;
        private double _seuilFiabilite;

        public AnalyseFrequentielle(int seuilOccurrencesMin = 2, double seuilFiabilite = 0.8)
        {
            _statistiquesMots = new Dictionary<string, (int, int)>();
            _fiabiliteMots = new Dictionary<string, double>();
            _indicateursFiables = new HashSet<string>();
            _seuilOccurrencesMin = seuilOccurrencesMin;
            _seuilFiabilite = seuilFiabilite;
        }

        public void ApprendreDesDonnees(List<DonneesSwift> donnees)
        {
            _statistiquesMots.Clear();
            _fiabiliteMots.Clear();
            _indicateursFiables.Clear();

            // 1. Collecter les statistiques
            foreach (var donnee in donnees)
            {
                var mots = ExtraireMotsEtPhrases(donnee.SWIFT);
                var estPositif = donnee.ExtendOrPay > 0.5;

                foreach (var mot in mots)
                {
                    if (!_statistiquesMots.ContainsKey(mot))
                    {
                        _statistiquesMots[mot] = (0, 0);
                    }

                    var (total, positifCount) = _statistiquesMots[mot];
                    _statistiquesMots[mot] = (total + 1, estPositif ? positifCount + 1 : positifCount);
                }
            }

            // 2. Calculer la fiabilité pour chaque mot
            foreach (var (mot, (total, positifCount)) in _statistiquesMots)
            {
                if (total >= _seuilOccurrencesMin)
                {
                    // Calcul de la fiabilité (% de fois où le mot indique correctement un Extend or Pay)
                    var fiabilitePositive = (double)positifCount / total;
                    var fiabiliteNegative = (double)(total - positifCount) / total;
                    
                    // On prend la fiabilité la plus élevée (positive ou négative)
                    var fiabilite = Math.Max(fiabilitePositive, fiabiliteNegative);
                    
                    // Le signe indique si c'est un indicateur positif ou négatif
                    _fiabiliteMots[mot] = fiabilitePositive > fiabiliteNegative ? fiabilite : -fiabilite;

                    if (Math.Abs(_fiabiliteMots[mot]) >= _seuilFiabilite)
                    {
                        _indicateursFiables.Add(mot);
                    }
                }
            }
        }

        private HashSet<string> ExtraireMotsEtPhrases(string texte)
        {
            var resultat = new HashSet<string>();
            
            // Normalisation du texte
            texte = texte.ToLowerInvariant();
            
            // 1. Extraction des mots individuels
            var mots = Regex.Split(texte, @"\W+")
                           .Where(m => m.Length >= 2)  // Ignorer les mots trop courts
                           .ToList();
            
            foreach (var mot in mots)
            {
                resultat.Add(mot);
            }
            
            // 2. Extraction des groupes de mots (2-3 mots)
            for (int i = 0; i < mots.Count - 1; i++)
            {
                // Groupes de 2 mots
                resultat.Add($"{mots[i]} {mots[i + 1]}");
                
                // Groupes de 3 mots
                if (i < mots.Count - 2)
                {
                    resultat.Add($"{mots[i]} {mots[i + 1]} {mots[i + 2]}");
                }
            }
            
            // 3. Recherche d'abréviations potentielles
            var abreviations = Regex.Matches(texte, @"\b[A-Z]{2,}\b")
                                  .Select(m => m.Value.ToLowerInvariant());
            foreach (var abr in abreviations)
            {
                resultat.Add(abr);
            }
            
            return resultat;
        }

        public class ResultatAnalyse
        {
            public double Score { get; set; }
            public List<(string Indicateur, double Fiabilite, int Total, int PositifCount)> IndicateursTrouves { get; set; }
            public Dictionary<string, double> ContributionsParIndicateur { get; set; }

            public ResultatAnalyse()
            {
                IndicateursTrouves = new List<(string, double, int, int)>();
                ContributionsParIndicateur = new Dictionary<string, double>();
            }
        }

        public ResultatAnalyse AnalyserTexte(string texte)
        {
            var resultat = new ResultatAnalyse();
            var motsTexte = ExtraireMotsEtPhrases(texte);
            var indicateursTrouves = new List<(string mot, double fiabilite)>();

            foreach (var mot in motsTexte)
            {
                if (_fiabiliteMots.ContainsKey(mot))
                {
                    var fiabilite = _fiabiliteMots[mot];
                    var (total, positifCount) = _statistiquesMots[mot];
                    
                    indicateursTrouves.Add((mot, fiabilite));
                    resultat.ContributionsParIndicateur[mot] = fiabilite;

                    if (_indicateursFiables.Contains(mot))
                    {
                        resultat.IndicateursTrouves.Add((mot, fiabilite, total, positifCount));
                    }
                }
            }

            // Calcul du score final
            if (indicateursTrouves.Any())
            {
                // Moyenne pondérée des fiabilités, avec plus de poids pour les indicateurs très fiables
                var sommeScores = indicateursTrouves.Sum(i => i.fiabilite * Math.Pow(Math.Abs(i.fiabilite), 2));
                var sommePoids = indicateursTrouves.Sum(i => Math.Pow(Math.Abs(i.fiabilite), 2));
                resultat.Score = sommeScores / sommePoids;
            }
            else
            {
                resultat.Score = 0;
            }

            // Trier les indicateurs par fiabilité absolue décroissante
            resultat.IndicateursTrouves = resultat.IndicateursTrouves
                .OrderByDescending(x => Math.Abs(x.Fiabilite))
                .ToList();

            return resultat;
        }

        public void AfficherStatistiques()
        {
            Console.WriteLine("\nAnalyse des indicateurs :");
            Console.WriteLine($"Nombre total d'indicateurs analysés : {_statistiquesMots.Count}");
            Console.WriteLine($"Nombre d'indicateurs fiables (fiabilité ≥ {_seuilFiabilite:P0}) : {_indicateursFiables.Count}");

            Console.WriteLine("\nIndicateurs fiables pour 'Extend or Pay' :");
            AfficherIndicateursFiables(true);

            Console.WriteLine("\nIndicateurs fiables pour 'Non Extend or Pay' :");
            AfficherIndicateursFiables(false);
        }

        private void AfficherIndicateursFiables(bool positifs, int limit = 10)
        {
            var indicateursTriés = _fiabiliteMots
                .Where(kv => positifs ? kv.Value > 0 : kv.Value < 0)
                .OrderByDescending(kv => Math.Abs(kv.Value))
                .Take(limit);

            foreach (var (indicateur, fiabilite) in indicateursTriés)
            {
                var (total, positifCount) = _statistiquesMots[indicateur];
                Console.WriteLine($"  - {indicateur,-30} " +
                                $"Fiabilité: {Math.Abs(fiabilite):P1} " +
                                $"({positifCount}/{total} occurrences)");
            }
        }

        public void AfficherAnalyseDetaillee(ResultatAnalyse resultat)
        {
            Console.WriteLine("\nAnalyse détaillée du texte :");
            
            var tendance = resultat.Score > 0 ? "Extend or Pay" : "Non Extend or Pay";
            var confiance = Math.Abs(resultat.Score);
            
            Console.WriteLine($"Tendance : {tendance}");
            Console.WriteLine($"Niveau de confiance : {confiance:P1}");

            if (resultat.IndicateursTrouves.Any())
            {
                Console.WriteLine("\nIndicateurs fiables trouvés :");
                foreach (var (indicateur, fiabilite, total, positifCount) in resultat.IndicateursTrouves)
                {
                    var type = fiabilite > 0 ? "→ Extend or Pay" : "→ Non Extend or Pay";
                    Console.WriteLine($"  - {indicateur,-30} " +
                                    $"Fiabilité: {Math.Abs(fiabilite):P1} {type}");
                    Console.WriteLine($"    Statistiques historiques : {positifCount}/{total} cas positifs");
                }
            }
            else
            {
                Console.WriteLine("\nAucun indicateur fiable trouvé dans ce texte.");
            }
        }
    }
}
