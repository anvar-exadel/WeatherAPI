﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WeatherApp.Models
{
    public class WeatherForecast
    {
        public string Name { get; set; }
        public int Cnt { get; set; }
        public string Comment { get; set; }
        public List<DailyInner> Daily { get; set; }
    }
    public class DailyInner
    {
        public Temp Temp { get; set; }
    }

    public class Temp
    {
        public double Day { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }
}
