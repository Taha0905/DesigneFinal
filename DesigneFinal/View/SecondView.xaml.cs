﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Windows.Media;

namespace DesigneFinal.View
{
    public partial class SecondView : Page
    {
        private string salleName;
        private bool isImageLoopRunning = false;
        private DispatcherTimer timer;
        private object previousContent; // Variable pour stocker la vue précédente
        private List<string> currentImages; // Liste des images actuellement affichées

        public SecondView(string salleName, object previousContent)
        {
            InitializeComponent();
            this.salleName = salleName;
            this.previousContent = previousContent;
            currentImages = new List<string>();

            LoadImages(salleName);

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

            await FetchAndDisplayImages(salleUrl, listUrl);
        }

        private async Task FetchAndDisplayImages(string salleUrl, string listUrl)
        {
            List<string> availableImages = await GetAvailableImages(listUrl);

            if (availableImages == null || availableImages.Count == 0)
            {
                MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                return;
            }

            currentImages = availableImages;
            isImageLoopRunning = true;
            await DisplayImagesLoop(salleUrl, listUrl);
        }

        private async Task<List<string>> GetAvailableImages(string listUrl)
        {
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
                        availableImages = availableImages.Where(img => !string.IsNullOrWhiteSpace(img) && img != "." && img != "..").ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des images : {ex.Message}");
            }

            return availableImages;
        }

        private async Task DisplayImagesLoop(string salleUrl, string listUrl)
        {
            while (isImageLoopRunning)
            {
                await DisplayMeteo();
                await Task.Delay(2000);

                foreach (var mediaFileName in currentImages)
                {
                    string mediaUrl = $"{salleUrl}{mediaFileName}";

                    try
                    {
                        if (mediaFileName.EndsWith(".mp4") || mediaFileName.EndsWith(".avi") || mediaFileName.EndsWith(".mov"))
                        {
                            mediaControl.Source = new Uri(mediaUrl);
                            mediaControl.Visibility = Visibility.Visible;
                            imageControl.Visibility = Visibility.Collapsed;

                            mediaControl.Play();

                            // Attendre que la durée de la vidéo soit disponible
                            while (!mediaControl.NaturalDuration.HasTimeSpan)
                            {
                                await Task.Delay(100); // Attendre que la durée soit chargée
                            }

                            // Utiliser la durée de la vidéo pour définir le délai
                            var videoDuration = mediaControl.NaturalDuration.TimeSpan;
                            await Task.Delay(videoDuration);

                            mediaControl.Stop();
                        }
                        else
                        {
                            // Afficher l'image
                            using (HttpClient client = new HttpClient())
                            {
                                HttpResponseMessage response = await client.GetAsync(mediaUrl);
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
                                    imageControl.Visibility = Visibility.Visible;
                                    mediaControl.Visibility = Visibility.Collapsed;
                                }
                            }
                            await Task.Delay(3000); // Délai de 3 secondes pour chaque image
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la récupération du média : {ex.Message}");
                        currentImages.Remove(mediaFileName);
                        break;
                    }
                }

                var availableImages = await GetAvailableImages(listUrl);
                if (availableImages != null)
                {
                    foreach (var image in currentImages.ToArray())
                    {
                        if (!availableImages.Contains(image))
                        {
                            currentImages.Remove(image);
                        }
                    }
                    currentImages.AddRange(availableImages.Where(img => !currentImages.Contains(img)));
                }
            }
        }

        private async Task DisplayMeteo()
        {
            try
            {
                Meteo meteoPage = new Meteo();
                await meteoPage.GetMeteo("Annecy");

                await Task.Delay(500);

                meteoPage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                meteoPage.Arrange(new Rect(0, 0, meteoPage.DesiredSize.Width, meteoPage.DesiredSize.Height));

                if (meteoPage.ActualWidth > 0 && meteoPage.ActualHeight > 0)
                {
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)meteoPage.ActualWidth, (int)meteoPage.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(meteoPage);
                    imageControl.Source = renderTargetBitmap;
                    imageControl.Visibility = Visibility.Visible;
                    mediaControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show("Erreur : la page météo n'a pas des dimensions valides pour l'affichage.");
                }

                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage de la météo : {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (previousContent != null)
            {
                ((Window)this.Parent).Content = previousContent;
            }
            else
            {
                MessageBox.Show("Aucune page précédente trouvée.");
            }
        }
    }
}
