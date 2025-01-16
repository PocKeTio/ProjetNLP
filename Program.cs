using System;
using System.CommandLine;
using System.IO;
using ProjetNLP.Models;
using ProjetNLP.Services;

namespace ProjetNLP
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            var pretraitement = new PretraitementTextuel();
            var modeleNLP = new ModeleNLP(pretraitement);
            var analyseStatistique = new AnalyseStatistique();
            var analyseFrequentielle = new AnalyseFrequentielle();

            // Commande TRAIN
            var trainCommand = new Command("TRAIN", "Entraîne le modèle NLP");
            var modelPathArg = new Argument<string>("CheminModele", "Chemin vers le fichier modèle");
            var dataPathArg = new Argument<string>("CheminBaseDeDonnees", "Chemin vers la base de données");
            trainCommand.AddArgument(modelPathArg);
            trainCommand.AddArgument(dataPathArg);

            trainCommand.SetHandler(async (string modelPath, string dataPath) =>
            {
                try
                {
                    Console.WriteLine("Chargement des données d'entraînement...");
                    var accesDonnees = new AccesDonnees(dataPath);
                    var donnees = accesDonnees.ChargerDonnees();

                    Console.WriteLine("Entraînement du modèle ML.NET...");
                    var modele = modeleNLP.CreerEtEntrainerModele(donnees);

                    Console.WriteLine("Entraînement de l'analyse fréquentielle...");
                    analyseFrequentielle.ApprendreDesDonnees(donnees);
                    analyseFrequentielle.AfficherStatistiques();

                    Console.WriteLine($"\nSauvegarde du modèle vers {modelPath}...");
                    modeleNLP.SauvegarderModele(modelPath);

                    Console.WriteLine("Entraînement terminé avec succès!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'entraînement : {ex.Message}");
                }
            }, modelPathArg, dataPathArg);

            // Commande PREDICT
            var predictCommand = new Command("PREDICT", "Prédit la classe d'un texte avec le modèle ML.NET");
            var modelPathPredictArg = new Argument<string>("CheminModele", "Chemin vers le fichier modèle");
            var textArg = new Argument<string>("TexteAPredire", "Texte à prédire");
            var langArg = new Argument<int>("CodeLangue", "Code de la langue (1 pour anglais, 2 pour français)");
            predictCommand.AddArgument(modelPathPredictArg);
            predictCommand.AddArgument(textArg);
            predictCommand.AddArgument(langArg);

            predictCommand.SetHandler(async (string modelPath, string text, int lang) =>
            {
                try
                {
                    Console.WriteLine("Chargement du modèle...");
                    modeleNLP.ChargerModele(modelPath);

                    Console.WriteLine("Prédiction en cours...");
                    var probabilite = modeleNLP.PredireProbabilite(text, lang);

                    Console.WriteLine($"Probabilité que le texte soit un Extend or Pay : {probabilite:P2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la prédiction : {ex.Message}");
                }
            }, modelPathPredictArg, textArg, langArg);

            // Commande ANALYSE
            var analyseCommand = new Command("ANALYSE", "Analyse statistique d'un texte");
            var analyseTextArg = new Argument<string>("TexteAAnalyser", "Texte à analyser");
            var analyseLangArg = new Argument<int>("CodeLangue", "Code de la langue (1 pour anglais, 2 pour français)");
            analyseCommand.AddArgument(analyseTextArg);
            analyseCommand.AddArgument(analyseLangArg);

            analyseCommand.SetHandler(async (string text, int lang) =>
            {
                try
                {
                    Console.WriteLine("Analyse statistique en cours...");
                    var resultat = analyseStatistique.AnalyserTexte(text, lang);
                    analyseStatistique.AfficherAnalyseDetaillee(resultat);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'analyse : {ex.Message}");
                }
            }, analyseTextArg, analyseLangArg);

            // Nouvelle commande FREQUENCE
            var frequenceCommand = new Command("FREQUENCE", "Analyse fréquentielle d'un texte");
            var frequenceTextArg = new Argument<string>("TexteAAnalyser", "Texte à analyser");
            var frequenceDataArg = new Argument<string>("CheminBaseDeDonnees", "Chemin vers la base de données d'apprentissage");
            frequenceCommand.AddArgument(frequenceDataArg);
            frequenceCommand.AddArgument(frequenceTextArg);

            frequenceCommand.SetHandler(async (string dataPath, string text) =>
            {
                try
                {
                    Console.WriteLine("Chargement des données d'apprentissage...");
                    var accesDonnees = new AccesDonnees(dataPath);
                    var donnees = accesDonnees.ChargerDonnees();

                    Console.WriteLine("Apprentissage des motifs...");
                    analyseFrequentielle.ApprendreDesDonnees(donnees);
                    analyseFrequentielle.AfficherStatistiques();

                    Console.WriteLine("\nAnalyse du texte...");
                    var resultat = analyseFrequentielle.AnalyserTexte(text);
                    analyseFrequentielle.AfficherAnalyseDetaillee(resultat);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'analyse fréquentielle : {ex.Message}");
                }
            }, frequenceDataArg, frequenceTextArg);

            // Root command
            var rootCommand = new RootCommand("Application de classification de textes SWIFT");
            rootCommand.AddCommand(trainCommand);
            rootCommand.AddCommand(predictCommand);
            rootCommand.AddCommand(analyseCommand);
            rootCommand.AddCommand(frequenceCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
