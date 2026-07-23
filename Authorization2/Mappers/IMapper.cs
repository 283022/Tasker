namespace Authorization2.Mappers;

public interface IMapper<T,B>
{
    public B Map(T  source);
}