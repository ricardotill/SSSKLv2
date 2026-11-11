// Each integration test class runs its own host on its own SQLite file, so classes can run in parallel.
// Tests within a class share that host and run sequentially.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
