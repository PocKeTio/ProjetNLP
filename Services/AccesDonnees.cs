using System.Data.OleDb;
using ProjetNLP.Models;

namespace ProjetNLP.Services
{
    public class AccesDonnees
    {
        private readonly string _connectionString;

        public AccesDonnees(string cheminFichier)
        {
            _connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={cheminFichier};";
        }

        public List<DonneesSwift> ChargerDonnees()
        {
            var donnees = new List<DonneesSwift>();

            using var connection = new OleDbConnection(_connectionString);
            connection.Open();

            using var command = new OleDbCommand("SELECT * FROM DonneesSwift", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                donnees.Add(new DonneesSwift
                {
                    SWIFT = reader["SWIFT"].ToString() ?? string.Empty,
                    ExtendOrPay = Convert.ToSingle(reader["ExtendOrPay"]),
                    Langue = Convert.ToSingle(reader["Langue"])
                });
            }

            return donnees;
        }
    }
}
