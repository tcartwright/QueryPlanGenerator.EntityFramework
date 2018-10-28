using QueryPlanGenerator.EntityFramework;
using QueryPlanGenerator.EntityFramework.Persisters;
using System;
using System.Configuration;
using System.Data.Entity;

namespace $rootnamespace$
{
    public class DbContextConfiguration : DbConfiguration
    {
        public DbContextConfiguration()
        {
            var persister = new DefaultFileSystemPersister($@"executionPlans\{DateTime.Now.ToString("yyyyMMdd_HHmmss.fffffff")}");

			//you can add multiple custom persisters to the interceptor to direct the plans to any destination
            this.AddInterceptor(new QueryPlanInterceptor(persister));
        }
    }
}
