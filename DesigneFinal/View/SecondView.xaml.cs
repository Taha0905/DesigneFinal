using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace DesigneFinal.View
{
    public partial class SecondView : Page
    {
        private MqttClient client;
        private string salleName;
        private bool isImageLoopRunning = false;
        private DispatcherTimer timer;
        private DispatcherTimer quoteTimer;
        private object previousContent; // Variable pour stocker la vue précédente
        private List<string> currentImages; // Liste des images actuellement affichées
        private List<string> quotes; // Liste des citations
        private int currentQuoteIndex = 0; // Index de la citation actuelle

        public SecondView(string salleName, object previousContent)
        {
            InitializeComponent();
            this.salleName = salleName;
            this.previousContent = previousContent;
            currentImages = new List<string>();
            quotes = new List<string>();

            LoadImages(salleName);
            LoadQuotes(); // Charger les citations
            InitializeDateTime(); // Initialiser l'affichage de la date et de l'heure
            InitializeQuoteTimer(); // Initialiser le timer pour les citations

            // Initialiser le client MQTT pour la récupération des valeurs des capteurs
            InitializeMqttClient();
        }

        private void InitializeMqttClient()
        {
            string brokerAddress = "172.31.254.123"; // Adresse de votre broker
            client = new MqttClient(brokerAddress);

            // Abonnement à l'événement de réception de message
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, "Taha", "Taha"); // Connexion avec identifiants

            // Abonnement aux topics des capteurs
            client.Subscribe(new string[] {
                "Batiment_3/1er/KM_102/Afficheur_n_1/Capteur_temperature_et_humidité",
                "Batiment_3/1er/KM_102/Afficheur_n_1/Capteur_de_CO2",
                "Batiment_3/1er/KM_102/Afficheur_n_1/Capteur_de_son"
            },
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);
            string topic = e.Topic;

            Dispatcher.Invoke(() =>
            {
                if (topic.Contains("Capteur_temperature_et_humidité"))
                {
                    string temperature = ExtractValue(message, "Temp", "C");
                    string humidity = ExtractValue(message, "Humidity", "%");

                    TBtemp.Text = $"Température {temperature} °C";
                    TBhum.Text = $"Humidité {humidity}%";
                }
                else if (topic.Contains("Capteur_de_CO2"))
                {
                    string pm25 = ExtractValue(message, "PM2.5", "microg/m³");
                    string pm10 = ExtractValue(message, "PM10", "microg/m³");

                    TBPM2.Text = $"PM2.5{pm25} µg/m³";
                    TBPM10.Text = $"PM10{pm10} µg/m³";
                }
                else if (topic.Contains("Capteur_de_son"))
                {
                    TBson.Text = $"Son : {message} dB";
                }
            });
        }

        private string ExtractValue(string message, string key, string delimiter)
        {
            try
            {
                if (message.Contains(key) && message.Contains(delimiter))
                {
                    int startIndex = message.IndexOf(key) + key.Length;
                    int endIndex = message.IndexOf(delimiter, startIndex);

                    if (startIndex >= 0 && endIndex > startIndex)
                    {
                        return message.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private async void LoadQuotes()
        {
            string quoteUrl = "https://quentinvrns.fr/Document/citation.txt";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string content = await client.GetStringAsync(quoteUrl);
                    var matches = Regex.Matches(content, @"\d+\.\s""([^""]+)""\s–\s(.+)");

                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            string citation = $"{match.Groups[1].Value} – {match.Groups[2].Value}";
                            quotes.Add(citation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des citations : {ex.Message}");
                TBinfo.Text = "Impossible de charger les citations.";
            }

            if (quotes.Count > 0)
            {
                TBinfo.Text = quotes[currentQuoteIndex];
            }
        }

        private void InitializeQuoteTimer()
        {
            quoteTimer = new DispatcherTimer();
            quoteTimer.Interval = TimeSpan.FromHours(12);
            quoteTimer.Tick += QuoteTimer_Tick;
            quoteTimer.Start();
        }

        private void QuoteTimer_Tick(object sender, EventArgs e)
        {
            if (quotes.Count == 0) return;

            currentQuoteIndex = (currentQuoteIndex + 1) % quotes.Count;
            TBinfo.Text = quotes[currentQuoteIndex];
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
            // Récupération des images disponibles
            List<string> availableImages = await GetAvailableImages(listUrl);

            if (availableImages == null || availableImages.Count == 0)
            {
                MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                return;
            }

            currentImages = availableImages;
            isImageLoopRunning = true;

            // Démarrer la boucle d'affichage des images
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

                            while (!mediaControl.NaturalDuration.HasTimeSpan)
                            {
                                await Task.Delay(100);
                            }

                            var videoDuration = mediaControl.NaturalDuration.TimeSpan;
                            await Task.Delay(videoDuration);

                            mediaControl.Stop();
                        }
                        else
                        {
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
                            await Task.Delay(3000);
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

                if (meteoPage.ActualWidth > 0 && meteoPage.DesiredSize.Height > 0)
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
