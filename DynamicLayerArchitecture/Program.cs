﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using DynamicLayerArchitecture.Config;
using DynamicLayerArchitecture.CustomAttributes;
using Newtonsoft.Json;

namespace DynamicLayerArchitecture
{
    [Repository]
    public interface IRepository
    {

        [Query("SELECT * FROM sys.sys_config")]
        List<object> TestMethod();
    }
    

    [EnableConfiguration]
    internal abstract class Program
    {
        private static void Main()
        {
            var watch = new Stopwatch();
            watch.Start();
            DynamicContainer.Configuration["SqlDriver"] = "MySqlConnector";
            DynamicContainer.Configuration["connectionString"] = "Server=localhost; Port = 3308; User=root; Database=sys; password=123456;";
            
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            typeof(Program).GetCustomAttributes(false);
            var repository = DynamicContainer.Create(typeof(IRepository)) as IRepository;
            Console.WriteLine(GC.GetTotalMemory(true));
            var result = repository?.TestMethod();
            Console.WriteLine(watch.ElapsedMilliseconds);
            Console.WriteLine(GC.GetTotalMemory(true));
            Console.WriteLine("{0}", JsonConvert.SerializeObject(result));
        }
    }
}