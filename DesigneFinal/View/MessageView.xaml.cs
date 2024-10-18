using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Linq;

namespace DesigneFinal.View
{
    public partial class MessagesView : Page
    {
        private string salleName;



        public MessagesView(string salleName)
        {
            InitializeComponent();
            this.salleName = salleName;
            LoadMessages();
        }

        private async void LoadMessages()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllMessages";
            string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    List<MessageData> messages = JsonConvert.DeserializeObject<List<MessageData>>(responseBody);

                    // Récupérer le ClasseId à partir du nom de la salle
                    int classeId = await ConvertSalleToClasseIdAsync(salleName);

                    List<MessageData> filteredMessages = messages.FindAll(m => m.ClasseId == classeId);

                    if (filteredMessages.Count > 0)
                    {
                        // Créer une chaîne avec des messages séparés par un trait ou un séparateur
                        TB_Messages.Text = string.Join("\n──────────────\n", filteredMessages.Select(m => m.Message));
                    }
                    else
                    {
                        TB_Messages.Text = "Aucun message pour cette salle.";
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Erreur lors de la récupération des messages : {e.Message}");
                }
            }
        }

        private async Task<int> ConvertSalleToClasseIdAsync(string salleName)
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse";
            string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    List<Salle> salles = JsonConvert.DeserializeObject<List<Salle>>(responseBody);

                    // Trouver l'ID correspondant au nom de la salle
                    var salle = salles.Find(s => s.NomSalle.Equals(salleName, StringComparison.OrdinalIgnoreCase));

                    if (salle != null)
                    {
                        return salle.Id; // Retourne le ClasseId correspondant
                    }
                    else
                    {
                        MessageBox.Show($"La salle {salleName} n'existe pas dans la base de données.");
                        return 0; // Retourne 0 si la salle n'existe pas
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Erreur lors de la récupération des salles : {e.Message}");
                    return 0;
                }
            }
        }

        // Classe Salle pour désérialisation
        public class Salle
        {
            public int Id { get; set; } // Correspond à ClasseId
            public string NomSalle { get; set; }
        }

    }

    public class MessageData
    {
        public int Id { get; set; }
        public int ClasseId { get; set; }
        public string Message { get; set; }
    }
}