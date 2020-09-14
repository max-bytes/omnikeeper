using FluentAssertions;
using Landscape.Base.Entity;
using Landscape.Base.Service;
using LandscapeRegistry.Controllers.Ingest;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Ingest.ActiveDirectoryXML;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Ingest
{
    class IngestActiveDirectoryXMLTest
    {
        [Test]
        public void Basic()
        {
            var ingestService = new IngestActiveDirectoryXMLService();
            var logger = new CountLogger<IngestActiveDirectoryXMLTest>();

            var (ciCandidates, relationCandidates) = ingestService.Files2IngestCandidates(new List<(Func<Stream>, string)>()
            {
                (() => File.OpenRead(FileUtils.GetFilepath("ADComputers.xml", Path.Combine("Ingest", "active-directory-xml"))), "ADComputers.xml"),
                (() => File.OpenRead(FileUtils.GetFilepath("ADUsers.xml", Path.Combine("Ingest", "active-directory-xml"))), "ADUsers.xml"),
                (() => File.OpenRead(FileUtils.GetFilepath("ADGroups.xml", Path.Combine("Ingest", "active-directory-xml"))), "ADGroups.xml"),
            }, new LayerSet(new long[] { 1, 2 }), logger);

            Assert.AreEqual(0, logger.GetCount(Microsoft.Extensions.Logging.LogLevel.Warning));
            Assert.AreEqual(0, logger.GetCount(Microsoft.Extensions.Logging.LogLevel.Error));

            ciCandidates.Values.Select(c => c.Attributes).Should().BeEquivalentTo(new List<CICandidateAttributeData>()
            {
                //groups
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD group: HR Team")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("group")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("HR Team")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=HR Team,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/HR Team")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.Build("all members of the HR department")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD group: IT Team")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("group")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("IT Team")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=IT Team,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/IT Team")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.Build("all members of the IT department")),
                }),

                // users
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD user: Dan Jump")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("user")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("Dan Jump")),
                    CICandidateAttributeData.Fragment.Build("user.first_name", AttributeScalarValueText.Build("Dan")),
                    CICandidateAttributeData.Fragment.Build("user.last_name", AttributeScalarValueText.Build("Jump")),
                    CICandidateAttributeData.Fragment.Build("user.username", AttributeScalarValueText.Build("danj")),
                    CICandidateAttributeData.Fragment.Build("ad.samAccountName", AttributeScalarValueText.Build("danj")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=Dan Jump,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/Dan Jump")),
                    CICandidateAttributeData.Fragment.Build("user.email", AttributeScalarValueText.Build("danj@contoso.com")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD user: Adam Barr")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("user")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("Adam Barr")),
                    CICandidateAttributeData.Fragment.Build("user.first_name", AttributeScalarValueText.Build("Adam")),
                    CICandidateAttributeData.Fragment.Build("user.last_name", AttributeScalarValueText.Build("Barr")),
                    CICandidateAttributeData.Fragment.Build("user.username", AttributeScalarValueText.Build("adamb")),
                    CICandidateAttributeData.Fragment.Build("ad.samAccountName", AttributeScalarValueText.Build("adamb")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=Adam Barr,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/Adam Barr")),
                    CICandidateAttributeData.Fragment.Build("user.email", AttributeScalarValueText.Build("adamb@contoso.com")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD user: Alan Steiner")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("user")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("Alan Steiner")),
                    CICandidateAttributeData.Fragment.Build("user.first_name", AttributeScalarValueText.Build("Alan")),
                    CICandidateAttributeData.Fragment.Build("user.last_name", AttributeScalarValueText.Build("Steiner")),
                    CICandidateAttributeData.Fragment.Build("user.username", AttributeScalarValueText.Build("alans")),
                    CICandidateAttributeData.Fragment.Build("ad.samAccountName", AttributeScalarValueText.Build("alans")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=Alan Steiner,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/Alan Steiner")),
                    CICandidateAttributeData.Fragment.Build("user.email", AttributeScalarValueText.Build("alans@contoso.com")),
                }),

                // computers
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD computer: PC-1")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("computer")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("PC-1")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=PC-1,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/PC-1")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.Build("PC of Dan Jump")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD computer: PC-2")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("computer")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("PC-2")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=PC-2,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/PC-2")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.Build("PC of Adam Barr")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.Build("AD computer: PC-3")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.Build("computer")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.Build("PC-3")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.Build("CN=PC-3,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.Build("acme.local/ACME/PC-3")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.Build("PC of Alan Steiner")),
                }),
            });

            relationCandidates.Should().HaveCount(7);
        }
    }
}
