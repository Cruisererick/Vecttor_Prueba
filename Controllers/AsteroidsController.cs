using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Nasa_Ateroids.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AsteroidsController : ControllerBase
    {
        private readonly string NASA_API_KEY = "zdUP8ElJv1cehFM0rsZVSQN7uBVxlDnu4diHlLSb";
        private readonly string NASA_API_BASE_URL = "https://api.nasa.gov/neo/rest/v1/feed";
        private readonly HttpClient _httpClient;

        public AsteroidsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<ActionResult<List<Asteroid>>> GetAsteroids([FromQuery] int days)
        {
            if (days < 1 || days > 7)
            {
                return BadRequest("The 'days' parameter must be a value between 1 and 7.");
            }

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var endDate = DateTime.Today.AddDays(days).ToString("yyyy-MM-dd");

            var url = $"{NASA_API_BASE_URL}?start_date={today}&end_date={endDate}&api_key={NASA_API_KEY}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, response.ReasonPhrase);
            }

            var json = await response.Content.ReadAsStringAsync();
            var asteroids = GetAsteroidsFromJson(json);

            return Ok(asteroids.OrderByDescending(a => a.Diameter).Take(3).ToList());
        }

        private List<Asteroid> GetAsteroidsFromJson(string json)
        {
            var result_asteroids = new List<Asteroid>();

            var jsonObject = JObject.Parse(json);
            var nearEarthObjects = jsonObject["near_earth_objects"];

            foreach (var date in nearEarthObjects.Children())
            {
                foreach (var asteroids in date)
                {
                    foreach (var asteroid in asteroids)
                    {
                        if ((bool)asteroid["is_potentially_hazardous_asteroid"])
                        {
                            var diameterMin = (double)asteroid["estimated_diameter"]["kilometers"]["estimated_diameter_min"];
                            var diameterMax = (double)asteroid["estimated_diameter"]["kilometers"]["estimated_diameter_max"];
                            var diameter = (diameterMin + diameterMax) / 2;
                            var velocity = (double)asteroid["close_approach_data"][0]["relative_velocity"]["kilometers_per_hour"];
                            var dateStr = (string)asteroid["close_approach_data"][0]["close_approach_date"];
                            var planet = (string)asteroid["close_approach_data"][0]["orbiting_body"];
                            var name = (string)asteroid["name"];

                            result_asteroids.Add(new Asteroid
                            {
                                Name = name,
                                Diameter = diameter,
                                Velocity = velocity,
                                Date = dateStr,
                                Planet = planet
                            });
                        }
                    }
                }
            }

            return result_asteroids;
        }
    }
}
