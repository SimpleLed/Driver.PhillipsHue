using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Driver.PhillipsHue.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Driver.PhillipsHue
{
    public class SimpleHueApi
    {
        private HttpClient client = new HttpClient();

        private string user;// = "ld0BIK4roHk9XrYoXsxuEThKAKAM4RUVjDE2w5xi";
        private string ip;// = "192.168.1.2";
        private string ApiBase => "https://" + ip + "/api/" + user + "/";
        public async Task<IEnumerable<Light>> GetLightsAsync()
        {
            // CheckInitialized();


            string stringResult = await client.GetStringAsync(new Uri(String.Format("{0}lights", ApiBase))).ConfigureAwait(false);

            List<Light> results = new List<Light>();

            JToken token = JToken.Parse(stringResult);
            if (token.Type == JTokenType.Object)
            {
                //Each property is a light
                var jsonResult = (JObject)token;

                foreach (var prop in jsonResult.Properties())
                {
                    Light newLight = JsonConvert.DeserializeObject<Light>(prop.Value.ToString());
                    newLight.Id = prop.Name;
                    results.Add(newLight);
                }
            }
            return results;
        }

        public Task<HueResults> SendCommandAsync(LightCommand command, string specifiedLight)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            string jsonCommand = JsonConvert.SerializeObject(command, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            return SendCommandRawAsync(jsonCommand, specifiedLight);
        }

        public async Task<HueResults> SendCommandRawAsync(string command, string specifiedLight)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            HueResults results = new HueResults();


            HttpResponseMessage result = await client.PutAsync(new Uri(ApiBase + string.Format("lights/{0}/state", specifiedLight)), new JsonContent(command)).ConfigureAwait(false);

            string jsonResult = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (DeserializeDefaultHueResult(jsonResult));
        }

        protected static HueResults DeserializeDefaultHueResult(string json)
        {
            HueResults result = new HueResults();

            try
            {
                result = JsonConvert.DeserializeObject<HueResults>(json);
            }
            catch (JsonSerializationException)
            {
                //Ignore JsonSerializationException
            }

            return result;

        }

        public void SetColor(int r, int g, int b, string light)
        {
            LightCommand command = new LightCommand();
            command.TurnOn();
            command.SetColor(new RGBColor(r, g, b));

            SendCommandAsync(command, light);
        }
    }
}
