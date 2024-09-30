using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace DesigneFinal.View
{
    public partial class SecondView : Page
    {
        private string salleName;
        private bool isImageLoopRunning = false;
        private DispatcherTimer timer;

        public SecondView(string salleName)
        {
            InitializeComponent();
            this.salleName = salleName;
            LoadImages(salleName);

            // Initialisation du timer pour l'heure et la date
            InitializeDateTime();
        }

        private void InitializeDateTime()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TBdate.Text = DateTime.Now.ToString("dddd dd MMMM yyyy");
            TBheure.Text = DateTime.Now.ToString("HH:mm");
        }

        private async void LoadImages(string salleName)
        {
            string baseImageUrl = "https://quentinvrns.fr/Document/";
            string salleUrl = $"{baseImageUrl}{salleName}/";
            string listUrl = $"{salleUrl}image.json";

            List<string> availableImages = new List<string>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(listUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        availableImages = JsonConvert.DeserializeObject<List<string>>(responseBody);

                        if (availableImages == null || availableImages.Count == 0)
                        {
                            MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Erreur HTTP lors de la récupération des images : {response.ReasonPhrase}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des images : {ex.Message}");
                return;
            }

            isImageLoopRunning = true;
            await DisplayImagesLoop(availableImages, salleUrl);
        }

        private async Task DisplayImagesLoop(List<string> availableImages, string salleUrl)
        {
            while (isImageLoopRunning)
            {
                foreach (var imageFileName in availableImages)
                {
                    string imageUrl = $"{salleUrl}{imageFileName}";

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.GetAsync(imageUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                                BitmapImage bitmap = new BitmapImage();
                                using (MemoryStream ms = new MemoryStream(imageData))
                                {
                                    ms.Seek(0, SeekOrigin.Begin);
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = ms;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                }
                                imageControl.Source = bitmap;
                            }
                            await Task.Delay(5000); // Délai entre les images
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la récupération de l'image : {ex.Message}");
                        return;
                    }
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Retour à la vue d'accueil ou précédente
        }
    }
}
