namespace Engine.Core.Stages;

public class StageRunner(IEnumerable<IStage> stages)
{
    private readonly IStage[] _sortedStages = TopologicalSort(stages.ToArray());

    public void Start()
    {
        foreach (var stage in _sortedStages)
        {
            stage.Start();
        }
    }
    public void Update(float dt)
    {
        foreach (var stage in _sortedStages)
        {
            stage.Update(dt);
        }
    }

    private static IStage[] TopologicalSort(IStage[] stages)
    {
        var stageMap = stages.ToDictionary(s => s.GetType(), s => s);
        var result = new List<IStage>();
        var visited = new HashSet<Type>();
        var tempMark = new HashSet<Type>();

        foreach (var stage in stages)
            Visit(stage.GetType());

        return [.. result]; // вернёт в порядке исполнения

        void Visit(Type type)
        {
            if (visited.Contains(type)) return;
            if (tempMark.Contains(type))
                throw new InvalidOperationException($"Цикл в зависимостях стадий: {type.Name}");

            tempMark.Add(type);

            var stage = stageMap[type];

            // Обрабатываем зависимости: те, кто должны идти до нас
            foreach (var dep in stage.After ?? [])
            {
                if (!stageMap.ContainsKey(dep))
                    throw new InvalidOperationException($"{type.Name} зависит от отсутствующей стадии {dep.Name}");
                Visit(dep);
            }

            // Обрабатываем: мы должны быть до кого-то
            foreach (var b in stage.Before ?? [])
            {
                if (!stageMap.ContainsKey(b))
                    throw new InvalidOperationException($"{type.Name} указан в Before -> {b.Name}, но {b.Name} не существует");
                // Вставим ourselves как зависимость для другого
                if (!stageMap.TryGetValue(b, out var target)) continue;
                if (target is null) continue;
                foreach (var back in target.After ?? [])
                    if (back == type) goto skipAdd; // уже учтено
                target.After = target.After.Append(type).ToArray();
                skipAdd:;
            }

            tempMark.Remove(type);
            visited.Add(type);
            result.Add(stage);
        }
    }
}
