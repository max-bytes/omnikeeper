﻿using Keycloak.Net;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class KeycloakModel
    {
        private readonly IConfiguration configuration;
        public KeycloakModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        //public async Task<bool> CreateGroup(string groupName)
        //{
        //    var keycloakURL = configuration.GetSection("Keycloak")["URL"];
        //    var realm = configuration.GetSection("Authentication")["Audience"];
        //    var client = new KeycloakClient(keycloakURL, "mcsuk", "123123"); // TODO

        //    return await client.CreateGroupAsync(realm, new Keycloak.Net.Models.Groups.Group()
        //    {
        //        Name = groupName
        //    });
        //}
    }
}
