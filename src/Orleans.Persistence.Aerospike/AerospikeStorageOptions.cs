﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Persistence.Aerospike
{
    public class AerospikeStorageOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3000;
        public string Namespace { get; set; } = "dev";
        /// <summary>
        /// Stage of silo lifecycle where storage should be initialized.  Storage must be initialzed prior to use.
        /// </summary>
        public int InitStage { get; set; } = ServiceLifecycleStage.ApplicationServices;
        public bool VerifyEtagGenerations { get; set; } = true;
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
