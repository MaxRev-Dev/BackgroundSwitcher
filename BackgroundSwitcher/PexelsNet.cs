
using Newtonsoft.Json;
using PexelsNet;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class PexelsClient
{
    private readonly string _apiKey;
    private const string BaseUrl = "http://api.pexels.com/v1/";

    public PexelsClient(string apiKey)
    {
        _apiKey = apiKey;
    }

    private HttpClient InitHttpClient()
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _apiKey);

        return client;
    }

    public async Task<Page> SearchAsync(string query, int page = 1, int perPage = 15)
    {
        using (var client = InitHttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(BaseUrl + "search?query=" + Uri.EscapeDataString(query) + "&per_page=" + perPage + "&page=" + page).ConfigureAwait(false);

            return await GetResultAsync(response).ConfigureAwait(false);
        }
    }

    public async Task<Page> PopularAsync(int page = 1, int perPage = 15)
    {
        using (var client = InitHttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(BaseUrl + "popular?per_page=" + perPage + "&page=" + page).ConfigureAwait(false);

            return await GetResultAsync(response).ConfigureAwait(false);
        }
    }

    private static async Task<Page> GetResultAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        if (response.IsSuccessStatusCode)
        {
            return JsonConvert.DeserializeObject<Page>(body);
        }

        throw new PexelsNetException(response.StatusCode, body);
    }

    public class Source
    {
        [JsonProperty("original")]
        public string Original { get; set; }

        [JsonProperty("large")]
        public string Large { get; set; }

        [JsonProperty("medium")]
        public string Medium { get; set; }

        [JsonProperty("small")]
        public string Small { get; set; }

        [JsonProperty("portrait")]
        public string Portrait { get; set; }

        [JsonProperty("square")]
        public string Square { get; set; }

        [JsonProperty("landscape")]
        public string Landscape { get; set; }

        [JsonProperty("tiny")]
        public string Tiny { get; set; }
    }

    public class Photo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("photographer")]
        public string Photographer { get; set; }

        [JsonProperty("src")]
        public Source Src { get; set; }
        public string Local { get; internal set; }
    }

    public class Page
    {
        [JsonProperty("page")]
        public int PageNumber { get; set; }

        [JsonProperty("per_page")]
        public int PerPage { get; set; }

        [JsonProperty("total_results")]
        public int TotalResults { get; set; }

        [JsonProperty("next_page")]
        public string NextPage { get; set; }

        [JsonProperty("photos")]
        public List<Photo> Photos { get; set; }
    }
}