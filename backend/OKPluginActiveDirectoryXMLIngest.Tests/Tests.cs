using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Ingest.ActiveDirectoryXML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OKPluginActiveDirectoryXMLIngest.Tests
{
    class Tests
    {
        [Test]
        public void Basic()
        {
            var ingestService = new ActiveDirectoryXMLIngestService();
            var logger = new CountLogger<Tests>();
            var (ciCandidates, relationCandidates) = ingestService.Files2IngestCandidates(new List<(Func<Stream>, string)>()
            {
                (() => File.OpenRead(FileUtils.GetFilepath("ADComputers.xml", Path.Combine("files"))), "ADComputers.xml"),
                (() => File.OpenRead(FileUtils.GetFilepath("ADUsers.xml", Path.Combine( "files"))), "ADUsers.xml"),
                (() => File.OpenRead(FileUtils.GetFilepath("ADGroups.xml", Path.Combine("files"))), "ADGroups.xml"),
            }, new LayerSet(new string[] { "1", "2" }), logger);

            Assert.AreEqual(0, logger.GetCount(Microsoft.Extensions.Logging.LogLevel.Warning));
            Assert.AreEqual(0, logger.GetCount(Microsoft.Extensions.Logging.LogLevel.Error));

            ciCandidates.Select(c => c.Attributes).Should().BeEquivalentTo(new List<CICandidateAttributeData>()
            {
                //groups
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD group: HR Team")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("group")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("HR Team")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=HR Team,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/HR Team")),
                    new CICandidateAttributeData.Fragment("ad.description", new AttributeScalarValueText("all members of the HR department")),
                }),
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD group: IT Team")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("group")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("IT Team")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=IT Team,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/IT Team")),
                    new CICandidateAttributeData.Fragment("ad.description", new AttributeScalarValueText("all members of the IT department")),
                }),

                // users
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD user: Dan Jump")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("user")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("Dan Jump")),
                    new CICandidateAttributeData.Fragment("user.first_name", new AttributeScalarValueText("Dan")),
                    new CICandidateAttributeData.Fragment("user.last_name", new AttributeScalarValueText("Jump")),
                    new CICandidateAttributeData.Fragment("user.username", new AttributeScalarValueText("danj")),
                    new CICandidateAttributeData.Fragment("ad.samAccountName", new AttributeScalarValueText("danj")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=Dan Jump,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/Dan Jump")),
                    new CICandidateAttributeData.Fragment("user.email", new AttributeScalarValueText("danj@contoso.com")),
                }),
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD user: Adam Barr")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("user")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("Adam Barr")),
                    new CICandidateAttributeData.Fragment("user.first_name", new AttributeScalarValueText("Adam")),
                    new CICandidateAttributeData.Fragment("user.last_name", new AttributeScalarValueText("Barr")),
                    new CICandidateAttributeData.Fragment("user.username", new AttributeScalarValueText("adamb")),
                    new CICandidateAttributeData.Fragment("ad.samAccountName", new AttributeScalarValueText("adamb")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=Adam Barr,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/Adam Barr")),
                    new CICandidateAttributeData.Fragment("user.email", new AttributeScalarValueText("adamb@contoso.com")),
                }),
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD user: Alan Steiner")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("user")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("Alan Steiner")),
                    new CICandidateAttributeData.Fragment("user.first_name", new AttributeScalarValueText("Alan")),
                    new CICandidateAttributeData.Fragment("user.last_name", new AttributeScalarValueText("Steiner")),
                    new CICandidateAttributeData.Fragment("user.username", new AttributeScalarValueText("alans")),
                    new CICandidateAttributeData.Fragment("ad.samAccountName", new AttributeScalarValueText("alans")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=Alan Steiner,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/Alan Steiner")),
                    new CICandidateAttributeData.Fragment("user.email", new AttributeScalarValueText("alans@contoso.com")),
                }),

                // computers
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD computer: PC-1")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("computer")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("PC-1")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=PC-1,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/PC-1")),
                    new CICandidateAttributeData.Fragment("ad.description", new AttributeScalarValueText("PC of Dan Jump")),
                }),
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD computer: PC-2")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("computer")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("PC-2")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=PC-2,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/PC-2")),
                    new CICandidateAttributeData.Fragment("ad.description", new AttributeScalarValueText("PC of Adam Barr")),
                }),
                new CICandidateAttributeData(new CICandidateAttributeData.Fragment[] {
                    new CICandidateAttributeData.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText("AD computer: PC-3")),
                    new CICandidateAttributeData.Fragment("ad.type", new AttributeScalarValueText("computer")),
                    new CICandidateAttributeData.Fragment("ad.name", new AttributeScalarValueText("PC-3")),
                    new CICandidateAttributeData.Fragment("ad.distinguishedName", new AttributeScalarValueText("CN=PC-3,OU=ACME,DC=acme,DC=local")),
                    new CICandidateAttributeData.Fragment("ad.canonicalName", new AttributeScalarValueText("acme.local/ACME/PC-3")),
                    new CICandidateAttributeData.Fragment("ad.description", new AttributeScalarValueText("PC of Alan Steiner")),
                }),
            });

            relationCandidates.Should().HaveCount(7);
        }
    }
}
