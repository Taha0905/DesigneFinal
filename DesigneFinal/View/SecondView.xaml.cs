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
using DesigneFinal;
using System.Windows.Media;

namespace DesigneFinal.View
{
    public partial class SecondView : Page
    {
        private string salleName;
        private bool isImageLoopRunning = false;
        private DispatcherTimer timer;
        private object previousContent; // Variable pour stocker la vue précédente
        private int iterationCount = 0; // Compteur d'itérations

        // Modification du constructeur pour accepter 'previousContent'
        public SecondView(string salleName, object previousContent)
        {
            InitializeComponent();
            this.salleName = salleName;
            this.previousContent = previousContent; // Stocker la vue précédente
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
                // 1. Afficher la météo en premier
                await DisplayMeteo();
                await Task.Delay(5000); // Délai de 10 secondes pour la météo

                // 2. Ensuite, afficher les images
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
                            await Task.Delay(3000); // Délai de 10 secondes pour chaque image
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

        // Méthode pour afficher la météo
        private async Task DisplayMeteo()
        {
            try
            {
                // Créer une instance de la page météo
                Meteo meteoPage = new Meteo();

                // Récupérer les données météo pour Annecy
                await meteoPage.GetMeteo("Annecy");

                // Attendre quelques millisecondes pour s'assurer que les données sont bien appliquées au XAML
                await Task.Delay(500);

                // Forcer le calcul des dimensions de la page météo
                meteoPage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                meteoPage.Arrange(new Rect(0, 0, meteoPage.DesiredSize.Width, meteoPage.DesiredSize.Height));

                // Vérifier si les dimensions sont valides avant de continuer
                if (meteoPage.ActualWidth > 0 && meteoPage.ActualHeight > 0)
                {
                    // Convertir le contenu de la page météo en image à afficher dans imageControl
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)meteoPage.ActualWidth, (int)meteoPage.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(meteoPage);
                    imageControl.Source = renderTargetBitmap;
                }
                else
                {
                    MessageBox.Show("Erreur : la page météo n'a pas des dimensions valides pour l'affichage.");
                }

                // Attendre avant de passer à l'image suivante
                await Task.Delay(5000); // Délai de 10 secondes pour afficher la météo
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage de la météo : {ex.Message}");
            }
        }

        // Modification du bouton 'Retour' pour restaurer la vue précédente
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (previousContent != null)
            {
                // Restaurer la vue précédente
                ((Window)this.Parent).Content = previousContent; // Utilisation correcte pour changer le contenu de la fenêtre
                                                                 // Ne pas réinitialiser previousContent, pour pouvoir revenir plusieurs fois
            }
            else
            {
                MessageBox.Show("Aucune page précédente trouvée.");
            }
        }
    }
}
