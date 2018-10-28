namespace QueryPlanGenerator.EntityFramework.Persisters
{
    public interface IPlanPersister
    {
        /// <summary>
        /// Interface for classes that can persist query plans
        /// </summary>
        /// <param name="fqmn">Fully qualified method name where the db call orginated</param>
        /// <param name="plan">Query plan (xml)</param>
        void Persist(string fqmn, string plan);
    }
}
