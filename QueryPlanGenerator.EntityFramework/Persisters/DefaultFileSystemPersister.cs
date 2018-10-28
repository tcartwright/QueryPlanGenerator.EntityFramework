using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace QueryPlanGenerator.EntityFramework.Persisters
{
    public class DefaultFileSystemPersister : IPlanPersister
    {
        private readonly string path = "plans";

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultFileSystemPersister" /> class.
        /// </summary>
        /// <param name="generatePlan">if set to <c>true</c> [generate plan].</param>
        /// <param name="directory">A directory to outout the plans into. If left null the current path\plans will be assumed. Can be a relative or absolute path.</param>
        public DefaultFileSystemPersister(string directory = null)
        {
            if (!string.IsNullOrWhiteSpace(directory)) { path = directory; }

            if (!Path.IsPathRooted(path))
            {
                var root = Path.GetDirectoryName(Assembly.GetAssembly(typeof(DefaultFileSystemPersister)).Location);
                path = Path.Combine(root, path);
            }
        }
        /// <summary>
        /// Interface for classes that can persist query plans
        /// </summary>
        /// <param name="fqmn">Fully qualified method name where the db call orginated</param>
        /// <param name="plan">Query plan (xml)</param>
        public void Persist(string fqmn, string plan)
        {
            //only create the directory if we persist a plan
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //persist the plan
            File.WriteAllText(Path.Combine(path, $"{fqmn}.sqlplan"), plan);
        }
    }
}
