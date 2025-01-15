using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using ProjetNLP.Models;
using System.IO;

namespace ProjetNLP.Services
{
    public class ModeleNLP
    {
        private readonly MLContext _mlContext;
        private readonly PretraitementTextuel _pretraitement;
        private ITransformer? _trainedModel;

        public ModeleNLP(PretraitementTextuel pretraitement)
        {
            _mlContext = new MLContext(seed: 1);
            _pretraitement = pretraitement;
        }

        public ITransformer CreerEtEntrainerModele(IEnumerable<DonneesSwift> donnees)
        {
            // Prétraitement des données
            var donneesPretraitees = donnees.Select(d => 
            {
                var (texteNettoye, caracteristiques) = _pretraitement.NettoyerTexte(d.SWIFT, (int)d.Langue);
                return new DonneesSwift
                {
                    SWIFT = texteNettoye,
                    ExtendOrPay = d.ExtendOrPay,
                    Langue = d.Langue,
                    HasExtendOrPay = caracteristiques["extend_or_pay"] ? 1 : 0,
                    HasPayCondition = caracteristiques["pay_condition"] ? 1 : 0,
                    HasExtendCondition = caracteristiques["extend_condition"] ? 1 : 0
                };
            });

            // Chargement des données
            var trainingDataView = _mlContext.Data.LoadFromEnumerable(donneesPretraitees);

            // Création du pipeline
            var pipeline = _mlContext.Transforms.Text
                .FeaturizeText("TextFeatures", nameof(DonneesSwift.SWIFT))
                .Append(_mlContext.Transforms.NormalizeMinMax("TextFeatures"))
                .Append(_mlContext.Transforms.Concatenate("Features", 
                    "TextFeatures", 
                    nameof(DonneesSwift.Langue),
                    nameof(DonneesSwift.HasExtendOrPay),
                    nameof(DonneesSwift.HasPayCondition),
                    nameof(DonneesSwift.HasExtendCondition)))
                .AppendCacheCheckpoint(_mlContext)
                .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: nameof(DonneesSwift.ExtendOrPay),
                    featureColumnName: "Features",
                    numberOfLeaves: 31,
                    numberOfTrees: 200,
                    minimumExampleCountPerLeaf: 20));

            // Entraînement avec validation croisée
            var cvResults = _mlContext.BinaryClassification.CrossValidate(
                trainingDataView, 
                pipeline, 
                numberOfFolds: 5,
                labelColumnName: nameof(DonneesSwift.ExtendOrPay));

            // Affichage des résultats de la validation croisée
            Console.WriteLine($"\nRésultats de la validation croisée (5-fold):");
            
            var avgMetrics = new
            {
                Accuracy = (float)cvResults.Average(r => r.Metrics.Accuracy),
                AreaUnderRocCurve = (float)cvResults.Average(r => r.Metrics.AreaUnderRocCurve),
                F1Score = (float)cvResults.Average(r => r.Metrics.F1Score),
                PositivePrecision = (float)cvResults.Average(r => r.Metrics.PositivePrecision),
                NegativePrecision = (float)cvResults.Average(r => r.Metrics.NegativePrecision)
            };

            Console.WriteLine($"Accuracy moyenne: {avgMetrics.Accuracy:P2}");
            Console.WriteLine($"AUC moyen: {avgMetrics.AreaUnderRocCurve:P2}");
            Console.WriteLine($"F1 Score moyen: {avgMetrics.F1Score:P2}");
            Console.WriteLine($"Précision sur les positifs: {avgMetrics.PositivePrecision:P2}");
            Console.WriteLine($"Précision sur les négatifs: {avgMetrics.NegativePrecision:P2}");

            // Entraînement final sur toutes les données
            _trainedModel = pipeline.Fit(trainingDataView);
            return _trainedModel;
        }

        public float PredireProbabilite(string texte, int langue)
        {
            if (_trainedModel == null)
                throw new InvalidOperationException("Le modèle doit être entraîné avant de faire des prédictions.");

            var (texteNettoye, caracteristiques) = _pretraitement.NettoyerTexte(texte, langue);
            var input = new DonneesSwift 
            { 
                SWIFT = texteNettoye, 
                Langue = langue,
                HasExtendOrPay = caracteristiques["extend_or_pay"] ? 1 : 0,
                HasPayCondition = caracteristiques["pay_condition"] ? 1 : 0,
                HasExtendCondition = caracteristiques["extend_condition"] ? 1 : 0
            };

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<DonneesSwift, DonneesSwift>(_trainedModel);
            var prediction = predictionEngine.Predict(input);

            return prediction.Probability;
        }

        public void SauvegarderModele(string cheminFichier)
        {
            if (_trainedModel == null)
                throw new InvalidOperationException("Le modèle doit être entraîné avant d'être sauvegardé.");

            using var fs = File.Create(cheminFichier);
            _mlContext.Model.Save(_trainedModel, null, fs);
        }

        public void ChargerModele(string cheminFichier)
        {
            using var stream = File.OpenRead(cheminFichier);
            _trainedModel = _mlContext.Model.Load(stream, out _);
        }
    }
}
