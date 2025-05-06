namespace LoadBalancer.Core.State;

public sealed class Instance
{
    private Instance(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public string Name { get; private set; }
    public string Address { get; private set; }

    private int _isHealthy = -1;
    public bool IsHealthy => Volatile.Read(ref _isHealthy) == 1;
    public bool IsDrained { get; private set; }

    public void MarkHealthy() => Volatile.Write(ref _isHealthy, 1);
    public void MarkUnhealthy() => Volatile.Write(ref _isHealthy, 0);

    public void Drain() => IsDrained = true;
    public void Recover() => IsDrained = false;

    public static Instance CreateFromDefinition(InstanceDefinition iDef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iDef.Name, nameof(iDef.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(iDef.Address, nameof(iDef.Address));

        return new Instance(iDef.Name, iDef.Address);
    }
}