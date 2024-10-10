using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DesigneFinal.Control
{
    public class DAO_MQTT
    {
        private IMqttClient mqttClient;
        private string brokerAddress = "172.31.254.92";  // Adresse du serveur MQTT
        private int brokerPort = 1883;
        private string username = "Taha";
        private string password = "Taha";

        public DAO_MQTT()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
        }

        // Méthode pour se connecter au broker MQTT
        public async Task ConnectAsync()
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(brokerAddress, brokerPort)
                .WithCredentials(username, password)
                .WithCleanSession()
                .Build();

            mqttClient.ConnectedAsync += async e =>
            {
                Console.WriteLine("Connected to MQTT Broker!");

                // Subscribe to the topics (Capteur de température, CO2, et Son)
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("Batiment_3/1er/KM_102/Afficheur_n°1")
                    .Build();

                await mqttClient.SubscribeAsync(subscribeOptions);
                Console.WriteLine("Subscribed to topic");
            };

            mqttClient.DisconnectedAsync += e =>
            {
                Console.WriteLine("Disconnected from MQTT Broker!");
                return Task.CompletedTask;
            };

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string topic = e.ApplicationMessage.Topic;
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                // Traite le message reçu en fonction du topic
                HandleMessage(topic, payload);

                return Task.CompletedTask;
            };

            await mqttClient.ConnectAsync(options);
        }

        // Méthode pour gérer les messages reçus
        private void HandleMessage(string topic, string message)
        {
            if (topic == "Batiment_3/1er/KM_102/Afficheur_n°1")
            {
                Console.WriteLine($"Message reçu sur le topic {topic} : {message}");

                // Ici, tu peux extraire les valeurs des capteurs depuis le message
                // Exemple : Capteur_de_temperature_et_son = Temp: 69.8 F / 21.0 C Humidity: 57%
                if (message.Contains("Capteur_de_temperature_et_son"))
                {
                    // Extraire les valeurs du message (Température, Humidité)
                    var tempData = ExtractTemperatureAndHumidity(message);
                    Console.WriteLine($"Température: {tempData.tempC}°C, Humidité: {tempData.humidity}%");
                }

                if (message.Contains("Capteur_de_CO2"))
                {
                    // Extraire les valeurs du CO2
                    var co2Data = ExtractCO2Data(message);
                    Console.WriteLine($"PM2.5: {co2Data.pm25} microg/m³, PM10: {co2Data.pm10} microg/m³");
                }

                if (message.Contains("Capteur_de_son"))
                {
                    // Extraire le niveau sonore
                    var soundData = ExtractSoundLevel(message);
                    Console.WriteLine($"Niveau sonore: {soundData} dB");
                }
            }
        }

        // Méthode pour extraire les données de température et d'humidité
        private (double tempC, double humidity) ExtractTemperatureAndHumidity(string message)
        {
            // Logique pour extraire les valeurs de température et d'humidité
            // Ex : "Temp: 69.8 F / 21.0 C Humidity: 57%"
            var tempC = 21.0;  // Exemple de valeur extraite
            var humidity = 57.0;  // Exemple de valeur extraite
            return (tempC, humidity);
        }

        // Méthode pour extraire les données du capteur CO2
        private (double pm25, double pm10) ExtractCO2Data(string message)
        {
            // Logique pour extraire les valeurs de PM2.5 et PM10
            // Ex : "PM2.5: 0.80 microg/m³, PM10: 3.20 microg/m³"
            var pm25 = 0.80;  // Exemple de valeur extraite
            var pm10 = 3.20;  // Exemple de valeur extraite
            return (pm25, pm10);
        }

        // Méthode pour extraire les données du capteur de son
        private double ExtractSoundLevel(string message)
        {
            // Logique pour extraire les valeurs du niveau sonore
            // Ex : "Capteur_de_son = 43.3 dB"
            var soundLevel = 43.3;  // Exemple de valeur extraite
            return soundLevel;
        }

        // Méthode pour se déconnecter du broker MQTT
        public async Task DisconnectAsync()
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
                Console.WriteLine("Disconnected from MQTT Broker");
            }
        }
    }
}
