using QueryPlanGenerator.EntityFramework;
using QueryPlanGenerator.EntityFramework.Persisters;
using System;
using System.Configuration;
using System.Data.Entity;

namespace PlanGenerator
{
    public class DbContextConfiguration : DbConfiguration
    {
        public DbContextConfiguration()
        {
            var persister = new DefaultFileSystemPersister($@"executionPlans\{DateTime.Now.ToString("yyyyMMdd_HHmmss.fffffff")}");

            //do not set this up in the context ctor.... it will keep adding new ones every time the context is recreated.
            this.AddInterceptor(new QueryPlanInterceptor(persister));
        }
    }
}
