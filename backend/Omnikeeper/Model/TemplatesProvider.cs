//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Model;
//using Omnikeeper.Base.Utils.ModelContext;
//using System.Threading.Tasks;

//namespace Omnikeeper.Model
//{
//    public class TemplatesProvider : ITemplatesProvider
//    {
//        public async Task<Templates> GetTemplates(IModelContext trans)
//        {
//            return await Templates.Build();
//        }
//    }

//    public class CachedTemplatesProvider : ITemplatesProvider
//    {
//        private readonly ITemplatesProvider TP;
//        public CachedTemplatesProvider(ITemplatesProvider tp)
//        {
//            TP = tp;
//        }
//        public async Task<Templates> GetTemplates(IModelContext trans)
//        {
//            var (item, hit) = await trans.GetOrCreateCachedValueAsync("templates", async () =>
//            {
//                return await TP.GetTemplates(trans);
//            });
//            return item;
//        }
//    }
//}
