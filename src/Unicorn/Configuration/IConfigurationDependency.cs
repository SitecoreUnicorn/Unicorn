using Rainbow.Storage;

namespace Unicorn.Configuration
{
    public interface IConfigurationDependency
    {
        /// <summary>
        /// Which configuration is it dependent on.
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// Returns a human readable string describing the dependency 
        /// </summary>
        /// <returns></returns>
        string GetLogMessage();
    }
}