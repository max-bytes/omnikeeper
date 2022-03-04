namespace Omnikeeper.Base.Model
{
    public interface IGeneratedDataHandling
    {
    }

    public class GeneratedDataHandlingInclude : IGeneratedDataHandling
    {
        private GeneratedDataHandlingInclude() { }

        public static GeneratedDataHandlingInclude Instance = new GeneratedDataHandlingInclude();
    }

    public class GeneratedDataHandlingExclude : IGeneratedDataHandling
    {
        private GeneratedDataHandlingExclude() { }

        public static GeneratedDataHandlingExclude Instance = new GeneratedDataHandlingExclude();
    }
}
