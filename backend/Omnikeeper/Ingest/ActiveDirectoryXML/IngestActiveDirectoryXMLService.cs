using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Omnikeeper.Ingest.ActiveDirectoryXML
{
    public class IngestActiveDirectoryXMLService
    {
        private readonly string[] IdentifiableUserAttributes = new string[] { "ad.type", "user.email" };
        private readonly string[] IdentifiableGroupAttributes = new string[] { "ad.type", "ad.distinguishedName" };
        private readonly string[] IdentifiableComputerAttributes = new string[] { "ad.type", "ad.distinguishedName" };

        private readonly XNamespace @namespace = "http://schemas.microsoft.com/powershell/2004/04";

        public (IDictionary<Guid, CICandidate>, IEnumerable<RelationCandidate>) Files2IngestCandidates(IEnumerable<(Func<Stream> stream, string filename)> files, LayerSet searchLayers, ILogger logger)
        {
            var ciCandidatesGroups = new Dictionary<Guid, CICandidate>();
            var ciCandidatesUsers = new Dictionary<Guid, CICandidate>();
            var relationCandidates = new List<RelationCandidate>();

            // resolve order of processing between files manually and report error if a dependency is missing (f.e. ADUsers.xml must be present, is basis for ADGroups.xml and ADComputers.xml)
            var fUsers = files.FirstOrDefault(f => "ADUsers.xml".Equals(f.filename));
            if (fUsers == default)
            {
                throw new Exception("Missing mandatory file ADUsers.xml");
            }
            else
            {
                using var fs = fUsers.stream();
                foreach (var (ciid, cic) in ParseUsers(fs, searchLayers, logger))
                    ciCandidatesUsers.Add(ciid, cic);
            }

            // build a lookup table of users with distinguished names as keys
            Dictionary<string, (Guid Key, CICandidate Value)> userLookupViaDN = ciCandidatesUsers.ToDictionary(kv => kv.Value.Attributes.Fragments.First(f => f.Name.Equals("ad.distinguishedName")).Value.Value2String(), kv => (kv.Key, kv.Value));

            var fComputers = files.FirstOrDefault(f => "ADComputers.xml".Equals(f.filename));
            if (fComputers != default)
            {
                using var fs = fComputers.stream();
                var (computers, relations) = ParseComputersAndRelationsToUsers(userLookupViaDN, fs, searchLayers, logger);
                foreach (var (ciid, cic) in computers) ciCandidatesGroups.Add(ciid, cic);
                relationCandidates.AddRange(relations);
            }
            var fGroups = files.FirstOrDefault(f => "ADGroups.xml".Equals(f.filename));
            if (fGroups != default)
            {
                using var fs = fGroups.stream();
                var (groups, relations) = ParseGroupsAndRelationsToUsers(userLookupViaDN, fs, searchLayers, logger);
                foreach (var (ciid, cic) in groups) ciCandidatesGroups.Add(ciid, cic);
                relationCandidates.AddRange(relations);
            }

            return (ciCandidatesUsers.Concat(ciCandidatesGroups).ToDictionary(k => k.Key, k => k.Value), relationCandidates);
        }

        IEnumerable<(Guid, CICandidate)> ParseUsers(Stream fs, LayerSet searchLayers, ILogger logger)
        {
            var @base = XElement.Load(fs);
            foreach (var group in @base.Elements(@namespace + "Obj"))
            {
                var SProps = group.Element(@namespace + "Props")?.Elements(@namespace + "S");
                if (SProps == null)
                {
                    logger.LogWarning("Could not find element \"Props\" in XML... malformed XML?");
                    continue;
                }
                var fragments = new List<CICandidateAttributeData.Fragment>();
                fragments.Add(CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("user")));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Name", "__name", "AD user: "));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Name", "ad.name"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "EmailAddress", "user.email"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "CanonicalName", "ad.canonicalName"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "DistinguishedName", "ad.distinguishedName"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "SamAccountName", "ad.samAccountName"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "SamAccountName", "user.username"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "GivenName", "user.first_name"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Surname", "user.last_name"));

                var ad = CICandidateAttributeData.Build(fragments);
                var ciCandidate = CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(IdentifiableUserAttributes, ad, searchLayers), ad);
                yield return (Guid.NewGuid(), ciCandidate);
            }
        }

        (IEnumerable<(Guid, CICandidate)>, IEnumerable<RelationCandidate>) ParseComputersAndRelationsToUsers(Dictionary<string, (Guid Key, CICandidate Value)> userLookupViaDN, Stream fs, LayerSet searchLayers, ILogger logger)
        {
            var computers = new List<(Guid, CICandidate)>();
            var relations = new List<RelationCandidate>();
            var @base = XElement.Load(fs);

            foreach (var group in @base.Elements(@namespace + "Obj"))
            {
                var props = group.Element(@namespace + "Props");
                var SProps = props?.Elements(@namespace + "S");
                if (props == null || SProps == null)
                {
                    logger.LogWarning("Could not find element \"Props\" in XML... malformed XML?");
                    continue;
                }

                var computerGuid = Guid.NewGuid();

                var fragments = new List<CICandidateAttributeData.Fragment>();
                fragments.Add(CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("computer")));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Name", "__name", "AD computer: "));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Name", "ad.name"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "CanonicalName", "ad.canonicalName"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Description", "ad.description"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "DistinguishedName", "ad.distinguishedName"));

                var managedByUserDN = SProps.FirstOrDefault(d => d.Attribute("N") != null && d.Attribute("N").Value.Equals("ManagedBy"))?.Value;
                if (managedByUserDN != null)
                {
                    if (userLookupViaDN.TryGetValue(managedByUserDN, out var foundUser))
                    {
                        var r = RelationCandidate.Build(CIIdentificationMethodByTemporaryCIID.Build(computerGuid), CIIdentificationMethodByTemporaryCIID.Build(foundUser.Key), "managed_by");
                        relations.Add(r);
                    }
                    else
                    {
                        logger.LogWarning($"Could not find managedByUserDN in users; userDN: \"{managedByUserDN}\"");
                    }
                }
                else
                {
                    logger.LogWarning("Could not parse ManagedBy property of computer... malformed XML?");
                }

                var ad = CICandidateAttributeData.Build(fragments);
                var ciCandidate = CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(IdentifiableComputerAttributes, ad, searchLayers), ad);
                computers.Add((computerGuid, ciCandidate));
            }

            return (computers, relations);
        }

        (IEnumerable<(Guid, CICandidate)>, IEnumerable<RelationCandidate>) ParseGroupsAndRelationsToUsers(Dictionary<string, (Guid Key, CICandidate Value)> userLookupViaDN, Stream fs, LayerSet searchLayers, ILogger logger)
        {
            var groups = new List<(Guid, CICandidate)>();
            var relations = new List<RelationCandidate>();
            var @base = XElement.Load(fs);

            foreach (var group in @base.Elements(@namespace + "Obj"))
            {
                var props = group.Element(@namespace + "Props");
                var SProps = props?.Elements(@namespace + "S");
                if (props == null || SProps == null)
                {
                    logger.LogWarning("Could not find element \"Props\" in XML... malformed XML?");
                    continue;
                }

                var groupGuid = Guid.NewGuid();

                var fragments = new List<CICandidateAttributeData.Fragment>();
                fragments.Add(CICandidateAttributeData.Fragment.Build("ad.type", AttributeScalarValueText.BuildFromString("group")));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Name", "__name", "AD group: "));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Name", "ad.name"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "CanonicalName", "ad.canonicalName"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "Description", "ad.description"));
                AddFragmentIfNotNull(fragments, ParseFragmentFromProps(SProps, "DistinguishedName", "ad.distinguishedName"));

                var userDNs = props.Elements(@namespace + "Obj").FirstOrDefault(d => d.Attribute("N") != null && d.Attribute("N").Value.Equals("Members"))?.Element(@namespace + "LST")?.Elements(@namespace + "S")?.Select(e => e.Value);
                if (userDNs != null)
                {
                    foreach (var userDN in userDNs)
                    { // find user CICandidate by distinguished name
                        if (userLookupViaDN.TryGetValue(userDN, out var foundUser))
                        {
                            var r = RelationCandidate.Build(CIIdentificationMethodByTemporaryCIID.Build(foundUser.Key), CIIdentificationMethodByTemporaryCIID.Build(groupGuid), "member_of_group");
                            relations.Add(r);
                        }
                        else
                        {
                            logger.LogWarning($"Could not find a member of group in users; userDN: \"{userDN}\"");
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Could not parse members of groups element... malformed XML?");
                }

                var ad = CICandidateAttributeData.Build(fragments);
                var ciCandidate = CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(IdentifiableGroupAttributes, ad, searchLayers), ad);
                groups.Add((groupGuid, ciCandidate));
            }

            return (groups, relations);
        }


        CICandidateAttributeData.Fragment ParseFragmentFromProps(IEnumerable<XElement> props, string propertyValue, string attributeName, string prefixValue = "")
        {
            var dn = props.FirstOrDefault(d => d.Attribute("N") != null && d.Attribute("N").Value.Equals(propertyValue))?.Value;
            if (dn == null) return null;
            return CICandidateAttributeData.Fragment.Build(attributeName, AttributeScalarValueText.BuildFromString(prefixValue + dn));
        }
        void AddFragmentIfNotNull(List<CICandidateAttributeData.Fragment> fragments, CICandidateAttributeData.Fragment f)
        {
            if (f != null) fragments.Add(f);
        }
    }
}
