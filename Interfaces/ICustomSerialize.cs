namespace Abi2CSharp.Interfaces
{
    public interface ICustomSerialize<T>
    {
        string Serialize();
        T Deserialize(Newtonsoft.Json.JsonReader reader);
    }
}
