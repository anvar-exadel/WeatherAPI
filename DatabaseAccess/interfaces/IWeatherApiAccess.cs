﻿using DatabaseAccess.models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseAccess.interfaces
{
    public interface IDbAccess<T>
    {
        DbResponse<T> GetWeatherData(string uri);
    }
}