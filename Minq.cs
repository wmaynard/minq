using Maynard.Logging;
using Maynard.Minq.Models;
using Maynard.Minq.Queries;
using Maynard.Minq.Singletons;
using Maynard.Singletons;
using Maynard.Time;

namespace Maynard.Minq;

public abstract class Minq<Model> : Singleton, IGdprHandler where Model : MinqDocument, new()
{
    protected readonly MinqClient<Model> mongo;
    
    protected Minq(string collection) => mongo = MinqClient<Model>.Connect(collection);

    /// <summary>
    /// Pages through all records in the collection, sorted by creation date ascending.
    /// </summary>
    /// <param name="pageSize">The number of records to return with each page.</param>
    /// <param name="pageNumber">The page number to query.  Important: this is zero-indexed!</param>
    /// <param name="remaining">The number of records remaining in the collection.</param>
    /// <returns>An array of models.</returns>
    public Model[] PageAllRecords(int pageSize, int pageNumber, out long remaining) => mongo
        .All()
        .Sort(sort => sort.OrderByDescending(model => model.CreatedOn))
        .Page(size: pageSize, number: pageNumber, out remaining);

    public virtual void Insert(params Model[] models) => mongo.Insert(models);

    public void Update(Model model)
    {
        model.UpdatedOn = Timestamp.Now;
        mongo.Update(model);
    }

    public virtual Model FromId(string id) => mongo
        .Where(query => query.EqualTo(model => model.Id, id))
        .Limit(1)
        .FirstOrDefault();
    
    public virtual Model FromIdUpsert(string id)
    {
        Model output = FromId(id);

        if (output != null)
            return output;
        
        output = new();
        mongo.Insert(output);

        return output;
    }

    public long WipeDatabase()
    {
        long output = 0;

        // if (!PlatformEnvironment.IsLocal || PlatformEnvironment.MongoConnectionString.Contains("-prod"))
            Log.Alert("Code attempted to wipe a database outside of a local environment.  This is not allowed.");
        // else
            output = mongo.All().Delete();

        return output;
    }

    public void Commit(Transaction transaction) => transaction?.Commit();
    public void Abort(Transaction transaction) => transaction?.TryAbort();

    // public void Replace(Model model) => mongo.Replace(model); // Obsolete with Update(Model)
    
    /// <summary>
    /// Overridable method to handle incoming GDPR deletion requests.  GDPR requests may contain an account ID, an
    /// email address, or both - but neither is guaranteed to be present.  When overriding this method, sanitize any
    /// PII (personally identifiable information), whether by deletion or replacing with dummy text, and return the affected
    /// record count.
    /// </summary>
    /// <param name="accountId">The accountId of the user requesting a deletion request.</param>
    /// <param name="dummyText">A dummy text string to replace PII with.</param>
    /// <returns>The affected record count.</returns>
    public virtual long ProcessGdprRequest(string accountId, string dummyText)
    {
        Log.Verbose($"A GDPR request was received but no process has been defined", data: new
        {
            Service = GetType().Name
        });
        return 0;
    }

    // TODO: Make sure Model is searchable, throw exception if not
    public virtual Model[] Search(params string[] terms) => mongo.Search(terms);
}