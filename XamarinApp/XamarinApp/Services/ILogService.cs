﻿using System;
using System.Collections.Generic;

namespace XamarinApp.Services
{
    public interface ILogService
    {
        void LogException(Exception e);
        void LogException(Exception e, params KeyValuePair<string, string>[] parameters);
        void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters);
    }
}