namespace Ozdilek.PM.TaskService.Domain;

/// <summary>
/// Pure graph-cycle check used before a task's dependency is set. Kept as a standalone static class
/// (no entity/DB dependency) so it is trivial to unit test in isolation.
/// </summary>
public static class TaskDependencyValidator
{
    /// <summary>
    /// Returns true if adding the edge (taskId -> dependsOnTaskId) to the existing edge set would create a cycle,
    /// i.e. dependsOnTaskId (transitively) already depends on taskId.
    /// </summary>
    public static bool WouldCreateCycle(IReadOnlyDictionary<Guid, Guid?> existingDependencies, Guid taskId, Guid dependsOnTaskId)
    {
        if (taskId == dependsOnTaskId)
        {
            return true;
        }

        var visited = new HashSet<Guid>();
        var current = dependsOnTaskId;

        while (true)
        {
            if (current == taskId)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                // Already-corrupt graph (shouldn't happen if every insert was validated) — treat as unsafe.
                return true;
            }

            if (!existingDependencies.TryGetValue(current, out var next) || next is null)
            {
                return false;
            }

            current = next.Value;
        }
    }
}
