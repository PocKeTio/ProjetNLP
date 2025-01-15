using System.Text.RegularExpressions;

namespace ProjetNLP.Services
{
    public class PretraitementTextuel
    {
        private static readonly HashSet<string> StopWordsAnglais = new()
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
            "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
            "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
            "or", "an", "will", "my", "one", "all", "would", "there", "their", "what"
        };

        private static readonly HashSet<string> StopWordsFrancais = new()
        {
            "le", "la", "les", "de", "des", "du", "un", "une", "et", "est",
            "en", "que", "qui", "dans", "pour", "sur", "avec", "par", "au", "aux",
            "ce", "ces", "dans", "il", "je", "nous", "vous", "ils", "elle", "sont",
            "mais", "ou", "où", "donc", "car", "si", "leur", "leurs", "dont", "tout"
        };

        // Expressions régulières pour détecter les contextes Extend or Pay
        private static readonly Dictionary<string, Regex> ExtendOrPayPatternsAnglais = new()
        {
            // Détection de la structure "extend or pay" et ses variations
            {"extend_or_pay", new Regex(@"
                \b(?:
                    (?:extend|prolong|renew|roll\s*over|defer|postpone|delay)
                    (?:\s+(?:and|or|(?:,\s*)?alternatively|(?:,\s*)?otherwise)\s+)?
                    (?:pay|settle|remit|honor|honour|discharge|liquidate|reimburse)
                    |
                    (?:pay|settle|remit|honor|honour|discharge|liquidate|reimburse)
                    (?:\s+(?:and|or|(?:,\s*)?alternatively|(?:,\s*)?otherwise)\s+)?
                    (?:extend|prolong|renew|roll\s*over|defer|postpone|delay)
                )\b
                ", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)},

            // Détection des conditions de paiement
            {"pay_condition", new Regex(@"
                (?:
                    \b(?:otherwise|else|if\s+not|failing\s+(?:this|which)|in\s+default\s+(?:thereof|of\s+which)|alternatively|or\s+else|failing\s+extension)
                    .{0,50}?
                    \b(?:pay|settle|remit|honor|honour|discharge|liquidate|reimburse|payment|settlement)
                    |
                    \b(?:pay|settle|remit|honor|honour|discharge|liquidate|reimburse|payment|settlement)
                    .{0,50}?
                    \b(?:if\s+not|unless|failing|in\s+case\s+(?:of|that)|in\s+the\s+event\s+(?:of|that))
                    .{0,30}?
                    \b(?:extend|prolong|renew|roll\s*over|defer|postpone|delay)
                )\b
                ", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)},

            // Détection des conditions d'extension
            {"extend_condition", new Regex(@"
                (?:
                    \b(?:either|choice|option|alternatively|(?:kindly\s+)?(?:request|asking)\s+(?:to|for)|please|we\s+(?:wish|want|would\s+like))
                    .{0,50}?
                    \b(?:extend|prolong|renew|roll\s*over|defer|postpone|delay|extension|prolongation|renewal)
                    |
                    \b(?:extend|prolong|renew|roll\s*over|defer|postpone|delay|extension|prolongation|renewal)
                    .{0,50}?
                    \b(?:is\s+(?:requested|required)|(?:would|could)\s+be\s+(?:appreciated|preferred))
                )\b
                ", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)}
        };

        private static readonly Dictionary<string, Regex> ExtendOrPayPatternsFrancais = new()
        {
            // Détection de la structure "proroger ou payer" et ses variations
            {"extend_or_pay", new Regex(@"
                \b(?:
                    (?:prorog(?:er|ation)|prolong(?:er|ation)|étend(?:re|re)|extend(?:re|sion)|renouvell?(?:er|ement)|report(?:er)?|différer|ajourner)
                    (?:\s+(?:et|ou|(?:,\s*)?alternativement|(?:,\s*)?sinon)\s+)?
                    (?:pay(?:er|ement)|règl(?:er|ement)|rembours(?:er|ement)|liquid(?:er|ation)|honor(?:er|ation)|acquitt(?:er|ement))
                    |
                    (?:pay(?:er|ement)|règl(?:er|ement)|rembours(?:er|ement)|liquid(?:er|ation)|honor(?:er|ation)|acquitt(?:er|ement))
                    (?:\s+(?:et|ou|(?:,\s*)?alternativement|(?:,\s*)?sinon)\s+)?
                    (?:prorog(?:er|ation)|prolong(?:er|ation)|étend(?:re|re)|extend(?:re|sion)|renouvell?(?:er|ement)|report(?:er)?|différer|ajourner)
                )\b
                ", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)},

            // Détection des conditions de paiement
            {"pay_condition", new Regex(@"
                (?:
                    \b(?:sinon|autrement|à\s+défaut|dans\s+le\s+cas\s+contraire|faute\s+de\s+quoi|en\s+cas\s+de\s+non|sans\s+quoi|à\s+défaut\s+de\s+quoi)
                    .{0,50}?
                    \b(?:pay(?:er|ement)|règl(?:er|ement)|rembours(?:er|ement)|liquid(?:er|ation)|honor(?:er|ation)|acquitt(?:er|ement))
                    |
                    \b(?:pay(?:er|ement)|règl(?:er|ement)|rembours(?:er|ement)|liquid(?:er|ation)|honor(?:er|ation)|acquitt(?:er|ement))
                    .{0,50}?
                    \b(?:si\s+non|sauf\s+si|à\s+moins\s+(?:que|d[e'])|en\s+l[']absence\s+de)
                    .{0,30}?
                    \b(?:prorog(?:er|ation)|prolong(?:er|ation)|étend(?:re|re)|extend(?:re|sion)|renouvell?(?:er|ement)|report(?:er)?|différer|ajourner)
                )\b
                ", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)},

            // Détection des conditions d'extension
            {"extend_condition", new Regex(@"
                (?:
                    \b(?:soit|choix|option|alternative|(?:nous\s+)?(?:demand(?:ons|e)|souhait(?:ons|e)|désir(?:ons|e))|merci\s+de|prière\s+de|veuillez)
                    .{0,50}?
                    \b(?:prorog(?:er|ation)|prolong(?:er|ation)|étend(?:re|re)|extend(?:re|sion)|renouvell?(?:er|ement)|report(?:er)?|différer|ajourner)
                    |
                    \b(?:prorog(?:er|ation)|prolong(?:er|ation)|étend(?:re|re)|extend(?:re|sion)|renouvell?(?:er|ement)|report(?:er)?|différer|ajourner)
                    .{0,50}?
                    \b(?:est\s+(?:demandée?|requise?|souhaitée?)|serait\s+(?:appréciée?|préférée?))
                )\b
                ", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)}
        };

        // Termes spécifiques aux montants et dates
        private static readonly Regex DatePattern = new(@"\b\d{1,2}[-/.]\d{1,2}[-/.]\d{2,4}\b|\b\d{1,2}(?:st|nd|rd|th|er|ère|ème|e)?\s*(?:jan(?:vier|uary|v)?|fév(?:rier)?|feb(?:ruary)?|mar(?:ch|s)?|avr(?:il)?|apr(?:il)?|mai|may|juin|jun(?:e)?|juil(?:let)?|jul(?:y)?|août|aug(?:ust)?|sep(?:tembre)?t?|oct(?:obre)?|nov(?:embre)?|déc(?:embre)?|dec(?:ember)?)\s+\d{2,4}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex MontantPattern = new(@"\b\d+(?:[,.]\d+)?\s*(?:usd|eur|gbp|jpy|chf|[$€£¥]|dollars?|euros?|pounds?|sterling|francs?)\b|\b(?:usd|eur|gbp|jpy|chf|[$€£¥]|dollars?|euros?|pounds?|sterling|francs?)\s*\d+(?:[,.]\d+)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public (string TexteNettoye, Dictionary<string, bool> Caracteristiques) NettoyerTexte(string texte, int langue)
        {
            if (string.IsNullOrWhiteSpace(texte))
                return (string.Empty, new Dictionary<string, bool>());

            var caracteristiques = new Dictionary<string, bool>();
            var patterns = langue == 1 ? ExtendOrPayPatternsAnglais : ExtendOrPayPatternsFrancais;

            // Détection des patterns Extend or Pay
            foreach (var pattern in patterns)
            {
                caracteristiques[pattern.Key] = pattern.Value.IsMatch(texte);
            }

            // Conversion en minuscules
            texte = texte.ToLowerInvariant();

            // Remplacement des dates et montants par des tokens génériques
            texte = DatePattern.Replace(texte, " DATE ");
            texte = MontantPattern.Replace(texte, " AMOUNT ");

            // Suppression des caractères spéciaux tout en gardant certains symboles importants
            texte = Regex.Replace(texte, @"[^\p{L}\s\-\/]", " ");
            
            // Normalisation des espaces
            texte = Regex.Replace(texte, @"\s+", " ");

            return (texte.Trim(), caracteristiques);
        }

        public List<string> TokeniserTexte(string texte, int langue, bool supprimerStopWords = true)
        {
            var tokens = texte.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!supprimerStopWords)
                return tokens;

            var stopWords = langue == 1 ? StopWordsAnglais : StopWordsFrancais;
            return tokens.Where(t => !stopWords.Contains(t)).ToList();
        }
    }
}
