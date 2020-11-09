using FluentAssertions;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Controllers.Ingest;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Ingest.ActiveDirectoryXML;
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
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD group: HR Team")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("group")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("HR Team")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=HR Team,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/HR Team")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.BuildFromString("all members of the HR department")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD group: IT Team")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("group")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("IT Team")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=IT Team,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/IT Team")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.BuildFromString("all members of the IT department")),
                }),

                // users
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD user: Dan Jump")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("user")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("Dan Jump")),
                    CICandidateAttributeData.Fragment.Build("user.first_name", AttributeScalarValueText.BuildFromString("Dan")),
                    CICandidateAttributeData.Fragment.Build("user.last_name", AttributeScalarValueText.BuildFromString("Jump")),
                    CICandidateAttributeData.Fragment.Build("user.username", AttributeScalarValueText.BuildFromString("danj")),
                    CICandidateAttributeData.Fragment.Build("ad.samAccountName", AttributeScalarValueText.BuildFromString("danj")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=Dan Jump,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/Dan Jump")),
                    CICandidateAttributeData.Fragment.Build("user.email", AttributeScalarValueText.BuildFromString("danj@contoso.com")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD user: Adam Barr")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("user")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("Adam Barr")),
                    CICandidateAttributeData.Fragment.Build("user.first_name", AttributeScalarValueText.BuildFromString("Adam")),
                    CICandidateAttributeData.Fragment.Build("user.last_name", AttributeScalarValueText.BuildFromString("Barr")),
                    CICandidateAttributeData.Fragment.Build("user.username", AttributeScalarValueText.BuildFromString("adamb")),
                    CICandidateAttributeData.Fragment.Build("ad.samAccountName", AttributeScalarValueText.BuildFromString("adamb")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=Adam Barr,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/Adam Barr")),
                    CICandidateAttributeData.Fragment.Build("user.email", AttributeScalarValueText.BuildFromString("adamb@contoso.com")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD user: Alan Steiner")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("user")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("Alan Steiner")),
                    CICandidateAttributeData.Fragment.Build("user.first_name", AttributeScalarValueText.BuildFromString("Alan")),
                    CICandidateAttributeData.Fragment.Build("user.last_name", AttributeScalarValueText.BuildFromString("Steiner")),
                    CICandidateAttributeData.Fragment.Build("user.username", AttributeScalarValueText.BuildFromString("alans")),
                    CICandidateAttributeData.Fragment.Build("ad.samAccountName", AttributeScalarValueText.BuildFromString("alans")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=Alan Steiner,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/Alan Steiner")),
                    CICandidateAttributeData.Fragment.Build("user.email", AttributeScalarValueText.BuildFromString("alans@contoso.com")),
                }),

                // computers
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD computer: PC-1")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("computer")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("PC-1")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=PC-1,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/PC-1")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.BuildFromString("PC of Dan Jump")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD computer: PC-2")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("computer")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("PC-2")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=PC-2,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/PC-2")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.BuildFromString("PC of Adam Barr")),
                }),
                CICandidateAttributeData.Build(new CICandidateAttributeData.Fragment[] {
                    CICandidateAttributeData.Fragment.Build("__name", AttributeScalarValueText.BuildFromString("AD computer: PC-3")),
                    CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("computer")),
                    CICandidateAttributeData.Fragment.Build("ad.name", AttributeScalarValueText.BuildFromString("PC-3")),
                    CICandidateAttributeData.Fragment.Build("ad.distinguishedName", AttributeScalarValueText.BuildFromString("CN=PC-3,OU=ACME,DC=acme,DC=local")),
                    CICandidateAttributeData.Fragment.Build("ad.canonicalName", AttributeScalarValueText.BuildFromString("acme.local/ACME/PC-3")),
                    CICandidateAttributeData.Fragment.Build("ad.description", AttributeScalarValueText.BuildFromString("PC of Alan Steiner")),
                }),
            });

            relationCandidates.Should().HaveCount(7);
        }
    }
}
