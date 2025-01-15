using Microsoft.ML.Data;

namespace ProjetNLP.Models
{
    public class DonneesSwift
    {
        [LoadColumn(0)]
        public string SWIFT { get; set; } = string.Empty;

        [LoadColumn(1)]
        public float ExtendOrPay { get; set; }

        [LoadColumn(2)]
        public float Langue { get; set; }

        // Caractéristiques contextuelles
        public float HasExtendOrPay { get; set; }
        public float HasPayCondition { get; set; }
        public float HasExtendCondition { get; set; }

        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        public float Score { get; set; }
        public float Probability { get; set; }
    }
}
