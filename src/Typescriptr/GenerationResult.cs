namespace Typescriptr
{
    public class GenerationResult
    {
        public GenerationResult(string types, string enums)
        {
            Types = types;
            Enums = enums;
        }
        
        public string Types { get; }
        public string Enums { get; }
    }
}