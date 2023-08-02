namespace PTMKApp;

public class DataBaseSettings
{
    public string ConnectionString { get; set; }
    public string DbName { get; set; }
    public override string ToString()
    {
        return $"{ConnectionString}Database={DbName};Pooling=true;";
    }
}