namespace OKPluginCLBNaemonVariableResolution
{
    public interface IInstanceRules
    {
        void ApplyInstanceRules(HostOrService hs, IDictionary<Guid, Group> groups);
        bool FilterTarget(HostOrService hs);
        bool FilterCustomer(Customer customer);
        bool FilterProfileFromCmdbCategory(Category category);
        bool FilterNaemonInstance(NaemonInstanceV1 naemonInstance);
    }
}
