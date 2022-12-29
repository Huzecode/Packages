using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace Packages.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly HttpClient _httpClient;

        private readonly ILogger<WeatherForecastController> _logger;
        /// <summary>
        /// polly without return type as dynamic can accept any return type.
        /// </summary>
        private readonly AsyncRetryPolicy _asyncRetryPolicy;
        /// <summary>
        /// Polly with return type.
        /// </summary>
        private readonly AsyncRetryPolicy<WeatherForecast> _asyncRetryPolicyWithReturnType;
        private bool _isReturnTypePolicy = true;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            _asyncRetryPolicy = Policy.Handle<ArgumentException>()
                                      .RetryAsync(2);

            _asyncRetryPolicyWithReturnType = Policy<WeatherForecast>.Handle<ArgumentException>()
                                      .RetryAsync(2);

            // in case we want to wait between each retry
            // then we can apply wait and retry function to the policy
            // and specify the number of retries and waiting time.
            _asyncRetryPolicy = Policy.Handle<NullReferenceException>()
                                      .WaitAndRetryAsync(2, time => TimeSpan.FromMilliseconds(30));

            // in case we want not to retry on a specific scenario
            // for example if the service is down something like 503 etc.
            // then we can add a condition over the exception to our policy like below.
            _asyncRetryPolicy = Policy.Handle<AppDomainUnloadedException>(exption =>
            {
                return exption.Message != "<any thing you want>";
            }).RetryAsync(2);

            _httpClient = new HttpClient();
        }

        private async Task<dynamic> RetryWithPolly()
        {
            if (!_isReturnTypePolicy)
                return await _asyncRetryPolicy.ExecuteAsync(async () =>
                {
                    // http client call logic
                    var oResult = await _httpClient.GetStringAsync("<your url can be here.>");
                    return oResult;
                });

            // (_asyncRetryPolicyWithReturnType)
            // specific return type for the policy is exactly the same only
            // we are forcing the ExecuteAsync method to return this specific type.

            else
                return await _asyncRetryPolicyWithReturnType.ExecuteAsync(async () =>
                {
                    // http client call logic
                    var oResult = await _httpClient.GetStringAsync("<your url can be here.>");
                    return JsonConvert.DeserializeObject<WeatherForecast>(oResult);
                });

        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}