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
using uPLibrary.Networking.M2Mqtt.Exceptions;


namespace DesigneFinal.View
{
    public partial class SecondView : Page
    {
        private MqttClient client;
        private string salleName;
        private bool isImageLoopRunning = false;
        private DispatcherTimer timer;
        private DispatcherTimer quoteTimer;
        private DispatcherTimer alertTimer; // Timer pour surveiller les alertes
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

            // Initialiser la surveillance des alertes
            InitializeAlertMonitoring();
        }

        private async void InitializeMqttClient()
        {
            try
            {
                if (client != null && client.IsConnected)
                {
                    return;
                }

                var cancellationTokenSource = new System.Threading.CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(7)); // Timeout après 7 secondes

                await Task.Run(() =>
                {
                    try
                    {
                        string brokerAddress = "TQN"; // Adresse de votre broker
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
                    catch (MqttConnectionException)
                    {
                      
                    }
                }, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
               
            }
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

                    TBPM2.Text = $"PM2.5 {pm25} µg/m³";
                    TBPM10.Text = $"PM10 {pm10} µg/m³";
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
                // Ajoute DisplayMessages dans la boucle existante comme pour la météo
                await DisplayMessages();
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
                            // Animation de transition (fade-out) pour les images
                            await FadeOutControl(imageControl);

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

                                    // Animation de transition (fade-in) pour les images
                                    await FadeInControl(imageControl);
                                }
                            }

                            await Task.Delay(3000); // Durée de l'affichage de l'image
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


        private async Task DisplayMessages()
        {
            try
            {
                MessagesView messagesPage = new MessagesView(salleName);
                await Task.Delay(500);

                // Animation de transition (fade-out) pour l'élément affiché avant
                await FadeOutControl(imageControl);

                messagesPage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                messagesPage.Arrange(new Rect(0, 0, messagesPage.DesiredSize.Width, messagesPage.DesiredSize.Height));

                if (messagesPage.ActualWidth > 0 && messagesPage.ActualHeight > 0)
                {
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)messagesPage.ActualWidth, (int)messagesPage.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(messagesPage);
                    imageControl.Source = renderTargetBitmap;

                    // Animation de transition (fade-in) pour les messages
                    await FadeInControl(imageControl);

                    imageControl.Visibility = Visibility.Visible;
                    mediaControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show("Erreur lors de l'affichage des messages.");
                }

                await Task.Delay(5000);  // Temps d'affichage des messages
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage des messages : {ex.Message}");
            }
        }

        private async Task DisplayMeteo()
        {
            try
            {
                Meteo meteoPage = new Meteo();
                await meteoPage.GetMeteo("Annecy");

                await Task.Delay(500);

                // Animation de transition (fade-out) pour la météo
                await FadeOutControl(imageControl);

                meteoPage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                meteoPage.Arrange(new Rect(0, 0, meteoPage.DesiredSize.Width, meteoPage.DesiredSize.Height));

                if (meteoPage.ActualWidth > 0 && meteoPage.DesiredSize.Height > 0)
                {
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)meteoPage.ActualWidth, (int)meteoPage.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(meteoPage);
                    imageControl.Source = renderTargetBitmap;

                    // Animation de transition (fade-in) pour la météo
                    await FadeInControl(imageControl);

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

        private async Task FadeInControl(UIElement control)
        {
            control.Opacity = 0;
            control.Visibility = Visibility.Visible;

            for (double i = 0; i <= 1; i += 0.1)
            {
                control.Opacity = i;
                await Task.Delay(20); // Réduit la durée du délai pour accélérer l'animation
            }

            control.Opacity = 1;
        }

        private async Task FadeOutControl(UIElement control)
        {
            for (double i = 1; i >= 0; i -= 0.1)
            {
                control.Opacity = i;
                await Task.Delay(20); // Réduit la durée du délai pour accélérer l'animation
            }

            control.Opacity = 0;
            control.Visibility = Visibility.Collapsed;
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

        private void InitializeAlertMonitoring()
        {
            alertTimer = new DispatcherTimer();
            alertTimer.Interval = TimeSpan.FromSeconds(2); 
            alertTimer.Tick += AlertTimer_Tick;
            alertTimer.Start();
        }

        private async void AlertTimer_Tick(object sender, EventArgs e)
        {
            string alertUrl = "https://quentinvrns.fr/Document/alerte.txt";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string alertContent = await client.GetStringAsync(alertUrl);

                    if (alertContent.Contains("ALERTE INCENDIE"))
                    {
                        DisplayAlert("ALERTE INCENDIE");
                    }
                    else if (alertContent.Contains("ALERTE INTRUSION"))
                    {
                        DisplayAlert("ALERTE INTRUSION");
                    }
                    else if (alertContent.Contains("ALERTE EVACUATION AUTRE DANGER"))
                    {
                        DisplayAlert("ALERTE EVACUATION AUTRE DANGER");
                    }
                    else
                    {
                        ClearAlert();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des alertes : {ex.Message}");
            }
        }

        //ALERTE

        private MediaPlayer alarmPlayer = new MediaPlayer();

        private bool isAlertActive = false;  // Indicateur pour savoir si une alerte est active
        private bool isAlarmPlaying = false;

        private void DisplayAlert(string alertType)
        {
            isAlertActive = true;
            mediaControl.Visibility = Visibility.Collapsed;
            imageControl.Visibility = Visibility.Collapsed;

            try
            {
                string alertImagePath = string.Empty;

                if (alertType.Contains("ALERTE INCENDIE"))
                {
                    alertImagePath = "pack://application:,,,/Image/incendie.png";
                }
                else if (alertType.Contains("ALERTE INTRUSION"))
                {
                    alertImagePath = "pack://application:,,,/Image/intrusion.png";
                }
                else if (alertType.Contains("ALERTE EVACUATION AUTRE DANGER"))
                {
                    alertImagePath = "pack://application:,,,/Image/evacuation.png";
                }

                if (!string.IsNullOrEmpty(alertImagePath))
                {
                    BitmapImage alertImage = new BitmapImage(new Uri(alertImagePath, UriKind.Absolute));
                    fullScreenAlertImage.Source = alertImage;
                    fullScreenAlertImage.Visibility = Visibility.Visible;
                    fullScreenAlertImage.Stretch = Stretch.Fill;

                    // Jouer le son d'alarme si ce n'est pas déjà en cours
                    if (!isAlarmPlaying)
                    {
                        PlayAlarmSound();
                        isAlarmPlaying = true;
                    }
                }
                else
                {
                    MessageBox.Show("Type d'alerte non pris en charge", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage de l'image d'alerte : {ex.Message}");
            }
        }

        private void PlayAlarmSound()
        {
            try
            {
                // Utiliser l'URL distante pour le fichier audio
                string soundUrl = "https://quentinvrns.fr/Document/alarme.wav";

                // Définir la source du MediaElement pour lire l'alarme
                AlarmMediaElement.Source = new Uri(soundUrl, UriKind.Absolute);
                AlarmMediaElement.Volume = 1.0; // Assurez-vous que le volume est à 100 %

                // Jouer l'alarme
                AlarmMediaElement.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la lecture du son d'alarme : {ex.Message}");
            }
        }

        private void ClearAlert()
        {
            isAlertActive = false;
            fullScreenAlertImage.Visibility = Visibility.Collapsed;
            imageControl.Stretch = Stretch.Uniform;

            // Arrêter le son d'alarme
            if (isAlarmPlaying)
            {
                StopAlarmSound();
                isAlarmPlaying = false;
            }
        }

        private void StopAlarmSound()
        {
            AlarmMediaElement.Stop();
        }
    }
}
