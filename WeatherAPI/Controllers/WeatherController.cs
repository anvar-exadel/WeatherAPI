﻿using AutoMapper;
using BusinessLogic.interfaces;
using BusinessLogic.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using WeatherAPI.data;
using WeatherAPI.models;

namespace WeatherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public WeatherController(IWeatherService weatherService, IConfiguration configuration, AppDbContext context, IMapper mapper)
        {
            _configuration = configuration;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("current/{city}")]
        public ActionResult<BusinessServiceResponse<Weather>> GetCurrentWeather(string city)
        {
            int time = _configuration.GetValue<int>("WeatherAppSettings:timeoutInMilliseconds");
            city = city.ToLower();

            //check whether city is allowed to be saved if not return weather info without saving
            Dictionary<string, int> cities = _configuration.GetSection("WebCities").GetChildren().ToDictionary(x => x.Key, x => int.Parse(x.Value));
            if (!cities.ContainsKey(city.ToLower())) return _weatherService.GetWeatherInfo(city, time);

            RemoveInvalidWeathers(cities);

            //return data if is exists in db
            WebWeather weather = _context.WebWeathers.FirstOrDefault(w => w.Name.ToLower() == city && w.WeatherDay == DateTime.Today);
            if (weather != null)
            {
                Weather weatherResponse = _mapper.Map<Weather>(weather);
                weatherResponse.Main = new Main();
                weatherResponse.Main.Temp = weather.Temperature;

                return new BusinessServiceResponse<Weather>(weatherResponse, true, ResponseType.Success);
            }

            //get response from business layer
            BusinessServiceResponse<Weather> response = _weatherService.GetWeatherInfo(city, time);
            if (!response.Success) return response;

            //successfull response save to database and return 
            WebWeather webWeather = _mapper.Map<WebWeather>(response.Data);
            webWeather.CreatedDate = DateTime.Now;
            webWeather.WeatherDay = DateTime.Today;
            webWeather.Temperature = response.Data.Main.Temp;

            _context.WebWeathers.Add(webWeather);
            _context.SaveChanges();

            return response;
        }

        [HttpGet("forecast/{city}/{days}")]
        public ActionResult<BusinessServiceResponse<WeatherForecast>> GetForecast(string city, int days)
        {
            int time = _configuration.GetValue<int>("WeatherAppSettings:timeoutInMilliseconds");
            int maxDays = _configuration.GetValue<int>("WeatherAppSettings:maxForecastDays");
            city = city.ToLower();

            Dictionary<string, int> cities = _configuration.GetSection("WebCities").GetChildren().ToDictionary(x => x.Key, x => int.Parse(x.Value));
            if (!cities.ContainsKey(city.ToLower())) return _weatherService.GetWeatherForecast(city, days, maxDays, time);

            RemoveInvalidWeathers(cities);

            //return data if is exists in db
            WebWeatherForecast weatherForecast = _context.WebWeatherForecasts.Include(w => w.Daily).FirstOrDefault(w => w.Name.ToLower() == city && w.Cnt == days);
            if (weatherForecast != null)
            {
                WeatherForecast weatherResponse = new WeatherForecast();
                Map_WebWeatherForecast_To_WeatherForecast(weatherForecast, weatherResponse);

                return new BusinessServiceResponse<WeatherForecast>(weatherResponse, true, ResponseType.Success);
            }

            BusinessServiceResponse<WeatherForecast> response = _weatherService.GetWeatherForecast(city, days, maxDays, time);
            if (!response.Success) return response;

            //successfull response save to database and return
            WebWeatherForecast webWeatherForecast = new WebWeatherForecast();
            Map_WeatherForecast_To_WebWeatherForecast(response.Data, webWeatherForecast);
            webWeatherForecast.CreatedTime = DateTime.Now;

            _context.WebWeatherForecasts.Add(webWeatherForecast);
            _context.SaveChanges();

            return response;
        }

        private void Map_WeatherForecast_To_WebWeatherForecast(WeatherForecast src, WebWeatherForecast dest)
        {
            dest.Name = src.Name;
            dest.Cnt = src.Cnt;
            dest.Comment = src.Comment;
            dest.Daily = src.Daily.Select(d => new WebDailyTemp(d.Temp.Day, d.Temp.Max, d.Temp.Min)).ToList();
        }
        private void Map_WebWeatherForecast_To_WeatherForecast(WebWeatherForecast src, WeatherForecast dest)
        {
            dest.Name = src.Name;
            dest.Cnt = src.Cnt;
            dest.Comment = src.Comment;
            dest.Daily = src.Daily.Select(d => new DailyInner(new Temp(d.Day, d.Min, d.Max))).ToList();
        }

        private void RemoveInvalidWeathers(Dictionary<string, int> cities)
        {
            DateTime curDate = DateTime.Now;

            List<WebWeather> weathers = _context.WebWeathers.ToList();
            foreach (WebWeather w in weathers)
                if (w.CreatedDate.AddSeconds(cities[w.Name.ToLower()]) <= curDate)
                    _context.Remove(w);

            _context.SaveChanges();
        }
    }
}
