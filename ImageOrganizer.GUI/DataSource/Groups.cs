﻿using ImageOrganizer.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ImageOrganizer.GUI.DataSource
{
    class Groups
    {
        public static Groups Instance { get; } = new Groups();

        private const string baseUri = "http://localhost:55850/api/";

        private HttpClient client;

        private Groups()
        {
            client = new HttpClient
            {
                BaseAddress = new Uri(baseUri)
            };
        }
        
        // Retrieve list of groups from the database.
        public async Task<Group[]> getGroups()
        {
            try
            {
                var json = await client.GetStringAsync("groups").ConfigureAwait(false);
                Group[] groups = JsonConvert.DeserializeObject<Group[]>(json);
                return groups;
            }
            catch (Exception)
            {
                throw;
            }     
        }
        
    }
}
