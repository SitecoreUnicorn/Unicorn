using Rainbow.Storage;

namespace Unicorn.Configuration
{
    class ItemConfigurationDependency : IConfigurationDependency
    {
        public IConfiguration Configuration { get; }
        public TreeRoot Root { get; set; }
        public string ItemPath { get; }
        public string GetLogMessage()
        {
            return $"The <include> '{Root.Name}' needs '{ItemPath}' from the configuration '{Configuration.Name}'";
        }

        public ItemConfigurationDependency(IConfiguration configuration, TreeRoot root, string itemPath)
        {
            Configuration = configuration;
            Root = root;
            ItemPath = itemPath;
        }
    }
}