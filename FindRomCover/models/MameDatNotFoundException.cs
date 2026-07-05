namespace FindRomCover.Models;

public class MameDatNotFoundException : Exception
{
    public MameDatNotFoundException(string message) : base(message)
    {
    }
}
