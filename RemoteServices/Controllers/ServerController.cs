﻿// Server controller.
// Copyright (C) 2014  Lex Li
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Reflection;
using RemObjects.Mono.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Http;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace RemoteServicesHost.Controllers
{
    [RoutePrefix("api/server")]
    [RequireHttps]
    public class ServerController : ApiController
    {
        [Route("")]
        [HttpGet]
        public IDictionary<string, List<string>> Get()
        {
            return JexusServer.ServerVariables;
        }

        [Route("")]
        [HttpPut]
        public void Put(SortedDictionary<string, List<string>> variables)
        {
            JexusServer.ServerVariables = variables;
            JexusServer.Save();
        }

        [Route("start")]
        [HttpGet]
        public bool GetStart()
        {
            var onLinux = JexusServer.IsRunningOnMono() && PlatformSupport.Platform != PlatformType.Windows;
            var process = Process.Start(onLinux ? "jws" : "jws.exe", "start");
            process.WaitForExit();
            return true;
        }

        [Route("stop")]
        [HttpGet]
        public bool GetStop()
        {
            var onLinux = JexusServer.IsRunningOnMono() && PlatformSupport.Platform != PlatformType.Windows;
            var process = Process.Start(onLinux ? "jws" : "jws.exe", "stop");
            process.WaitForExit();
            return true;
        }

        [Route("state")]
        [HttpGet]
        public bool GetState()
        {
            var onLinux = JexusServer.IsRunningOnMono() && PlatformSupport.Platform != PlatformType.Windows;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = onLinux ? "jws" : "jws.exe",
                    Arguments = "status",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
            var content = process.StandardOutput.ReadToEnd();
            return content.Contains("running");
        }
        
        [Route("version")]
        [HttpGet]
        public string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        [Route("cert")]
        [HttpPost]
        public X509Certificate2 GetCertificate([FromBody] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            
            if (path.Contains(".."))
            {
                // IMPORTANT: prevent parent paths.
                return null;
            }
            
            var current = Directory.GetCurrentDirectory();
            if (!path.StartsWith(current, StringComparison.OrdinalIgnoreCase))
            {
                // IMPORTANT: prevent any path.
                return null;
            }
            
            if (!File.Exists(path))
            {
                return null;
            }

            return new X509Certificate2(path);
        }
        
        [Route("cert/save")]
        [HttpPost]
        public string SaveCertificate([FromBody] string text)
        {
            if (!Request.IsLocal())
            {
                return string.Empty;
            }
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "jexus.crt");
            File.WriteAllText(path, text);
            return path;
        }
        
        [Route("key/save")]
        [HttpPost]
        public string SaveKey([FromBody] string text)
        {
            if (!Request.IsLocal())
            {
                return string.Empty;
            }
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "jexus.key");            
            File.WriteAllText(path, text);
            return path;
        }
        
        [Route("test")]
        [HttpPost]
        public string GetString([FromBody] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            
            if (path.Contains(".."))
            {
                // IMPORTANT: prevent parent paths.
                // TODO: does this catch all cases?
                return string.Empty;
            }
            
            var current = Directory.GetCurrentDirectory();
            if (!path.StartsWith(current, StringComparison.OrdinalIgnoreCase))
            {
                // IMPORTANT: prevent any path.
                return string.Empty;
            }
            
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            return File.ReadAllText(path);
        }
        
        [Route("hello")]
        [HttpPost]
        public string Hello([FromBody] string address)
        {            
            if (!string.IsNullOrEmpty(JexusServer.CurrentClient) && JexusServer.CurrentClient != address)
            {
                return Request.IsLocal() ? JexusServer.CurrentClient : "another host";
            }
            
            JexusServer.CurrentClient = address;
            return address;
        }
        
        [Route("bye")]
        [HttpPost]
        public string Bye([FromBody] string address)
        {
            if (JexusServer.CurrentClient != address)
            {
                return Request.IsLocal() ? JexusServer.CurrentClient : "another host";
            }
            
            JexusServer.CurrentClient = string.Empty;
            return address;
        }
    }
}
