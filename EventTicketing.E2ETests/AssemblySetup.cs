using NUnit.Framework;

// All E2E tests share one live API server and one SQLite database.
// Running test classes in parallel would create multiple browser instances
// that race on the same rate-limit buckets and DB state, causing flaky timeouts.
// Forcing sequential execution makes the suite deterministic on CI runners.
[assembly: Parallelizable(ParallelScope.None)]
