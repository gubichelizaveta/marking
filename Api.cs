using Marking_TZ.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marking_TZ
{
    internal class Api
    {
        private readonly HttpClient httpClient;

        public Api()
        {
            httpClient = new HttpClient();
        }

        public async Task<Obj> GetMissionDataAsync(string url)
        {
            var response = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<Obj>(response);
        }
    }
}
