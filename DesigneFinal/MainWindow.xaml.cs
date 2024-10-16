using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using DesigneFinal.View;

namespace DesigneFinal
{
    public partial class MainWindow : Window
    {
        private string selectedSalle;

        public MainWindow()
        {
            InitializeComponent();
            LoadSalles();
        }

        public object previousContent; // Variable pour stocker la vue précédente

        // Classe pour représenter les salles
        public class Salle
        {
            public int Id { get; set; }
            public string NomSalle { get; set; }
            public int EtageId { get; set; }
        }

        // Fonction pour charger les salles depuis l'API
        private async void LoadSalles()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse";
            string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0OTgsImV4cCI6MTAxNzI3NjMwNDk4LCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.zoOGRIEnJR1-lpKkKbD7soukVxlQJmyeu5-CN9x7s_I";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    List<Salle> salles = JsonConvert.DeserializeObject<List<Salle>>(responseBody);

                    if (salles == null || salles.Count == 0)
                    {
                        MessageBox.Show("Aucune salle disponible.");
                        return;
                    }

                    // Remplir le ComboBox avec les salles
                    salleComboBox.Items.Clear();
                    foreach (var salle in salles)
                    {
                        salleComboBox.Items.Add(salle.NomSalle);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Erreur lors de la récupération des salles : {e.Message}");
                }
            }
        }

        // Gestion de la sélection dans le ComboBox
        private void salleComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedSalle = salleComboBox.SelectedItem as string;
        }

        // Gestion du clic sur le bouton de validation
        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedSalle))
            {
                MessageBox.Show("Veuillez sélectionner une salle.");
                return;
            }

            // Stocker la vue actuelle avant de la remplacer
            previousContent = this.Content;

            // Naviguer vers SecondView avec selectedSalle et previousContent
            SecondView secondView = new SecondView(selectedSalle, previousContent);
            this.Content = secondView;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0); // Arrête complètement le programme
        }
    }
}
