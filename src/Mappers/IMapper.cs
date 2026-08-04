using src.Domain;

namespace src.Mappers;

public interface IMapper<T,B>
{
    public Result<B> Map(T  source);
}