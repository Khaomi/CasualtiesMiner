using AngouriMath;

namespace CasualtiesMiner.Dumper.Parsing.Delegates.Operations;

internal abstract class Operation
{
    public Entity? Condition { get; set; }
    public Entity? Timer { get; set; }
}

internal class AssignmentOperation : Operation
{
    public string? Holder { get; set; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public required Entity Value { get; set; }

}

internal class MethodCallOperation : Operation
{
    public string? Holder { get; set; }
    public required string Method { get; set; }
    public required List<Entity> Arguments { get; set; }

}