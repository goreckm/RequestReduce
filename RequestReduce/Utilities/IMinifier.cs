using RequestReduce.Reducer;
namespace RequestReduce.Utilities
{
    public interface IMinifier
    {
        string Minify(string unMinifiedContent, ResourceType resourceType);
    }
}