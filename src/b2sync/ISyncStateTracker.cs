namespace b2sync;

public interface ISyncStateTracker
{
    void Initialize();
    void SetComplete(SyncTask task);
    void SetHash(SyncTask task);
    void SeedTasks(IReadOnlyList<SyncTask> tasks);
    IReadOnlyList<SyncTask> GetIncompleteTasks();
}
