
namespace Omnikeeper.Base.Entity
{
    public interface ITemplateErrorAttribute
    {
        string ErrorMessage { get; }
    }
    public class TemplateErrorAttributeGeneric : ITemplateErrorAttribute
    {
        public string ErrorMessage { get; private set; }

        public TemplateErrorAttributeGeneric(string message)
        {
            ErrorMessage = message;
        }
    }
}
