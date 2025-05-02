public class TrieNode
{
    public Dictionary<string, TrieNode> Children { get; } = new Dictionary<string, TrieNode>();
    public HashSet<string> data { get; } = new HashSet<string>();
    private readonly ReaderWriterLockSlim rwNodeLock = new ReaderWriterLockSlim();

    // Потокобезопасное добавление данных
    public void AddData(string company)
    {
        rwNodeLock.EnterWriteLock();
        try
        {
            data.Add(company);
        }
        finally
        {
            rwNodeLock.ExitWriteLock();
        }
    }

    // Потокобезопасное получение данных
    public ISet<string> GetData()
    {
        rwNodeLock.EnterReadLock();
        try
        {
            return new HashSet<string>(data);
        }
        finally
        {
            rwNodeLock.ExitReadLock();
        }
    }

    // Потокобезопасный доступ к дочернему узлу
    public bool TryGetChild(string key, out TrieNode? node)
    {
        rwNodeLock.EnterReadLock();
        try
        {
            return Children.TryGetValue(key, out node);
        }
        finally
        {
            rwNodeLock.ExitReadLock();
        }
    }

    // Потокобезопасное добавление дочернего узла
    public TrieNode GetOrAddChild(string key)
    {
        rwNodeLock.EnterUpgradeableReadLock();
        try
        {
            if (Children.TryGetValue(key, out var node))
            {
                return node;
            }

            rwNodeLock.EnterWriteLock();
            try
            {
                if (!Children.TryGetValue(key, out node))
                {
                    node = new TrieNode();
                    Children[key] = node;
                }
                return node;
            }
            finally
            {
                rwNodeLock.ExitWriteLock();
            }
        }
        finally
        {
            rwNodeLock.ExitUpgradeableReadLock();
        }
    }

    // Освобождение ресурсов
    public void Dispose()
    {
        rwNodeLock.Dispose();
    }
}

public class AdvertisingStorageTrie : IDisposable
{
    private TrieNode root = new TrieNode();
    private readonly ReaderWriterLockSlim rwTreeLock = new ReaderWriterLockSlim();
    private bool disposed = false;

    // Добавление компании для списка регионов (потокобезопасно)
    static private void Add(TrieNode targetRoot, string company, string regions)
    {
        var regionList = regions.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrEmpty(r) && r.StartsWith("/"));

        foreach (var region in regionList)
        {
            var currentNode = targetRoot;
            var parts = region.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) // Пропускаем пустые пути после TrimStart('/')
                continue;

            foreach (var part in parts)
            {
                currentNode = currentNode.GetOrAddChild(part);
            }

            currentNode.AddData(company);
        }
    }

    // Парсинг строки и добавление компании
    static private void AddFromLine(TrieNode targetRoot, string line)
    {
        var parts = line.Split(':');
        if (parts.Length != 2)
            return; // Некорректная строка

        var company = parts[0].Trim();
        var regions = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(regions))
            return; // Пропускаем пустые компании или регионы
        Add(targetRoot,company, regions);
    }

     public void AddFromContent(string content)
     {
        rwTreeLock.EnterWriteLock();
        try
        {
            var newRoot = new TrieNode();

            using (var reader = new StringReader(content))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    AddFromLine(newRoot,line);
                }
            }

            root = newRoot;
        }
        finally
        {
            rwTreeLock.ExitWriteLock();
        }
     }

    // Получение компаний с учётом иерархии (потокобезопасно)
    public ISet<string> GetCompaniesWithHierarchy(string region)
    {
        if (string.IsNullOrWhiteSpace(region) || !region.StartsWith("/"))
            return new HashSet<string>();
         
        rwTreeLock.EnterReadLock();
        try
        {
            var result = new HashSet<string>();
            var currentNode = root;
            var parts = region.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Собираем компании из всех узлов по пути
            result.UnionWith(currentNode.GetData());
            foreach (var part in parts)
            {
                if (!currentNode.TryGetChild(part, out var nextNode) || nextNode == null)
                {
                    return result;
                }
                currentNode = nextNode;
                result.UnionWith(currentNode.GetData());
            }

            return result;
        }
        finally
        {
            rwTreeLock.ExitReadLock();
        }
    }

    // Освобождение ресурсов
    public void Dispose()
    {
        if (!disposed)
        {
            DisposeNode(root);
            disposed = true;
        }
    }

    private void DisposeNode(TrieNode node)
    {
        foreach (var child in node.Children.Values)
        {
            DisposeNode(child);
        }
        node.Dispose();
    }
}